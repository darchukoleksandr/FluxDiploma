using System;
using System.Collections.Generic;
using System.IdentityModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using Auth0.Core.Exceptions;
using DAL.MongoDb.Repository;
using Domain.Crypto;
using Domain.Models;
using Domain.Repository;
using Host.Web.Utils;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Logging;
using Org.BouncyCastle.Bcpg;
// ReSharper disable UnusedMember.Global

namespace Host.Web.Hubs
{
    [System.Web.Http.Authorize]
    public class ChatHub : Hub
    {
        private static readonly string[] ScopesToSave = { "nickname", "picture", "gender", "firstname", "lastname" };
        private static readonly IUserRepository UserRepository = new UserRepository();
        private static readonly IGroupRepository GroupRepository = new GroupRepository();
        private static readonly Dictionary<string, string> ConnectedUsers = new Dictionary<string, string>();
        private readonly ILogger _logger = LoggerFactory.Default.Create(nameof(ChatHub));

        public ChatHub()
        {
            
        }

        public override Task OnConnected()
        {
            _logger.WriteVerbose($"Connection opened: {Context.ConnectionId}");
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            ConnectedUsers.Remove(Context.ConnectionId);
            Console.WriteLine($"User {Context.ConnectionId} removed from online list!");
            return base.OnDisconnected(stopCalled);
        }

        public async Task<ConnectionData> Connect()
        {
            var accesToken = Context.Headers.Get("Authorization").Replace("Bearer ", string.Empty);

            if (string.IsNullOrEmpty(accesToken))
            {
                throw new BadRequestException("No access token provided!");
            }

            IEnumerable<TypeValueClaim> userClaims;
            try
            {
                userClaims = await Jwt.ReadUserClaims(accesToken);
            }
            catch (ApiException e)
            {
                throw new ApiException(HttpStatusCode.Unauthorized);
            }

            var userEmail = userClaims.First(claim => claim.Type.ToLower() == "email").Value;

            var user = await UserRepository.GetByEmail(userEmail);

            if (user == null)
            {
                user = new User
                {
                    Email = userEmail,
                    Claims = userClaims.Where(claim => ScopesToSave.Contains(claim.Type.ToLower())).ToList()
                };

                await UserRepository.Create(user);
            }

            var result = new ConnectionData
            {
                User = user,
                Groups = new List<Group>()
            };

            foreach (var chatRoomId in user.PrivateKeys)
            {
                try
                {
                    var chatRoom = await GroupRepository.GetByIdIncludeMessages(chatRoomId.GroupId);
                    result.Groups.Add(chatRoom);
                }
                catch (Exception)
                {
                    // ReSharper disable once RedundantJumpStatement
                    continue;
                }
            }
            result.Contacts = new List<ChatUserViewModel>();
            foreach (var contact in user.Contacts)
            {
                var chatUser = await UserRepository.GetByEmailForContacts(contact);
                if (chatUser != null)
                {
                    result.Contacts.Add(chatUser);
                }
            }
            
            AddUserToOnlineList(Context.ConnectionId, userEmail);
            Console.WriteLine($"User connected: {user.Email}");

            return result;
        }

        [AllowAnonymous]
        public int CountOnline()
        {
            return ConnectedUsers.Count;
        }

        public async void LeaveGroup(Guid groupId)
        {
            var userEmail = ConnectedUsers[Context.ConnectionId];

            await GroupRepository.LeaveGroup(groupId, userEmail);
            var receipents = await GroupRepository.GetReceipents(groupId);

            NotifyLeaveGroupReceipents(receipents, groupId, userEmail);

            Console.WriteLine($"LeaveGroup request from: {userEmail}!");
        }
        
        public async Task<List<ChatUserViewModel>> GetUsersData(IEnumerable<string> userEmailList)
        {
            var result = new List<ChatUserViewModel>();

            foreach (var userEmail in userEmailList)
            {
                var chatUser = await UserRepository.GetByEmailForContacts(userEmail);
                if (chatUser != null)
                {
                    result.Add(chatUser);
                }
            }
            
            Console.WriteLine($"GetUsersData request responsed!");
            return result;
        }
        
        public async Task<OperationResponse<ChatUserViewModel>> AddToContacts(string contactEmail)
        {
            var result = new OperationResponse<ChatUserViewModel>();

            var connectedUser = ConnectedUsers[Context.ConnectionId];
            var savedUser = await UserRepository.GetByEmail(connectedUser);
            var contactUser = await UserRepository.GetByEmailForContacts(contactEmail);

            if (connectedUser.Equals(contactEmail))
            {
                result.Error = "Can't add yourself to contacts!";
                Console.WriteLine($"AddToContacts request: {contactEmail} can't add yourself to contacts!");
                return result;
            }
            if (savedUser.Contacts.Contains(contactEmail))
            {
                result.Error = "Contact user already in list!";
                Console.WriteLine($"AddToContacts request: {contactEmail} already in list!");
                return result;
            }
            else if (contactUser == null)
            {
                result.Error = "Contact user was not found!";
                Console.WriteLine($"AddToContacts request: user {contactEmail} not found!");
                return result;
            }
            else
            {
                UserRepository.AddContact(connectedUser, contactEmail);
//                savedUser.Contacts.Add(contactUser.Email);
                result.Response = contactUser;
                Console.WriteLine($"AddToContacts request: {savedUser.Email} added {contactUser.Email}!");
                //CreateGroup(savedUser.Email, String.Empty, GroupType.Personal, new []{ savedUser.Email, contactEmail});
                return result;
            }
        }
        
        public async Task RemoveFromContacts(string contactEmail)
        {
            var result = new OperationResponse<User>();

            var userEmail = ConnectedUsers[Context.ConnectionId];
            var savedUser = await UserRepository.GetByEmail(userEmail);

            if (!savedUser.Contacts.Contains(contactEmail))
            {
                result.Error = "Contact user was not found in list!";
                Console.WriteLine($"RemoveFromContacts request: {contactEmail} not found in your list!");
            }
            else
            {
                await UserRepository.RemoveContact(userEmail, contactEmail);
//                savedUser.Contacts.Remove(contactEmail);
//                result.Response = savedUser;
                Console.WriteLine($"RemoveFromContacts request: {savedUser.Email} removed {contactEmail}!");
            }
        }
        
        public async Task<OperationResponse<byte[]>> DownloadFile(Guid storageFileId)
        {
            var result = new OperationResponse<byte[]>();
            var filePath = Path.Combine("Files", storageFileId.ToString());

            if (!File.Exists(filePath))
            {
                result.Error = "File not exits!";
                return result;
            }

            using (var fileStream = File.OpenRead(filePath))
            {
                using (var memoryStream = new MemoryStream())
                {
                    await fileStream.CopyToAsync(memoryStream);
                    result.Response = memoryStream.ToArray();
                }
            }

            return result;
        }

        //TODO unused?
        public async Task<OperationResponse<IEnumerable<Group>>> GetGroupsData(IEnumerable<Guid> groupsList)
        {
            var groups = new List<Group>();
            var result = new OperationResponse<IEnumerable<Group>>
            {
                Response = groups
            };

            foreach (var groupId in groupsList)
            {
                var chatRoom = await GroupRepository.GetByIdIncludeMessages(groupId);
                groups.Add(chatRoom);
            }

            Console.WriteLine("GetGroupsData request responsed!");
            return result;
        }

        public async void SendFile(string fileName, Guid groupId, byte[] data)
        {
            var sender = ConnectedUsers[Context.ConnectionId];

            var receipents = await GroupRepository.GetReceipents(groupId);
            var storageFileId = Guid.NewGuid().ToString();

            using (var fileStream = File.Create(Path.Combine("Files", storageFileId)))
            {
                await fileStream.WriteAsync(data, 0, data.Length);

                Console.WriteLine($"SendFile request: {Path.GetFileName(fileStream.Name)}!");

                var message = new Message
                {
                    SenderEmail = sender,
                    Content = storageFileId,
                    Sended = DateTime.Now,
                    Type = MessageType.File
                };

                await GroupRepository.InsertMessage(groupId, message);

                NotifyMessageReceivedReceipents(receipents, groupId, message);
            }
        }

        public async void SendMessage(Guid groupId, string content)
        {
            var group = await GroupRepository.GetByIdExcludeMessages(groupId);

            if (group == null)
            {
                throw new NullReferenceException("No group with provided id was found!");
            }

            var sender = ConnectedUsers[Context.ConnectionId];

            if (group.Type == GroupType.Channel)
            {
                if (group.Owner != sender)
                {
                    throw new ArgumentException("Sender is not channel owner!");
                }
            }

            var message = new Message
            {
                SenderEmail = sender,
                Content = content,
                Sended = DateTime.Now,
                Type = MessageType.Plain
            };

            await GroupRepository.InsertMessage(groupId, message);

            var receipents = await GroupRepository.GetReceipents(groupId);

            Console.WriteLine($"Message from {sender}: {content}");

            NotifyMessageReceivedReceipents(receipents, groupId, message);
        }

        public async Task<IEnumerable<SearchResult>> Search(string pattern)
        {
            var groupsResult = await GroupRepository.Search(pattern);
            var usersResult = await UserRepository.Search(pattern);

            var result = new List<SearchResult>();

            result.AddRange(groupsResult.Select(group => new SearchResult
            {
                Id = group.Id,
                Name = group.Name,
                Picture = group.Picture,
                Type = SearchResultType.Group
            }));

            result.AddRange(usersResult.Select(user => new SearchResult
            {
                Id = user.Id,
                Name = user.Email,
                Picture = user.Claims.First(claim => claim.Type == "Picture").Value,
                Type = SearchResultType.User
            }));

            Console.WriteLine($"Search: {pattern} returned ({result.Count} match)!");

            return result;
        }

        //public async void JoinChannel(Guid channelId)
        //{
        //    var userEmail = ConnectedUsers[Context.ConnectionId];
        //    var result = await GroupRepository.GetByIdIncludeMessages(channelId);
        //    await UserRepository.AddPrivateKey(userEmail, null, channelId);
        //    Clients.Caller.JoinChannel(result);
        //}

        //public async void CreateChannel(string owner, string name)
        //{
        //    var result = new Group
        //    {
        //        Owner = owner,
        //        Name = name,
        //        Type = GroupType.Channel
        //    };
            
        //    await GroupRepository.Create(result);

        //    Console.WriteLine($"Channel {result.Name} created!");
        //}

        public async void CreateGroup(string owner, string name, GroupType groupType, IEnumerable<string> receipents)
        {
            Console.WriteLine($"CreateGroup starting!");
            
            var userGroupPrivateKeys = new List<UserGroupPrivateKeyInfo>();
            var groupUsers = new List<GroupUserPublicKey>();

            var result = new Group
            {
                Owner = owner,
                Name = name,
                Type = groupType,
                UsersPublicKeys = groupUsers
            };

            foreach (var chatUserEmail in receipents)
            {
                var pgpKeyPair = new Pgp().GenerateKeyPair(SymmetricKeyAlgorithmTag.Aes256, chatUserEmail);

                groupUsers.Add(new GroupUserPublicKey
                {
                    Email = chatUserEmail,
                    PublicKey = pgpKeyPair.PublicKey
                });

                userGroupPrivateKeys.Add(new UserGroupPrivateKeyInfo
                {
                    Email = chatUserEmail,
                    PrivateKey = pgpKeyPair.PrivateKey
                });
            }

            await GroupRepository.Create(result);

            foreach (var chatUser in userGroupPrivateKeys)
            {
                await UserRepository.AddPrivateKey(chatUser.Email, chatUser.PrivateKey, result.Id);

                var userConnection = ConnectedUsers.FirstOrDefault(pair => pair.Value == chatUser.Email).Key;
                if (!string.IsNullOrEmpty(userConnection))
                {
                    Clients.Client(userConnection).RoomInvite(result, chatUser.PrivateKey);
                }
            }

            Console.WriteLine($"Group {result.Name} created with {groupUsers.Count} users!");
        }

        //TODO unused?
        public async Task<ICollection<Message>> GetGroupMessagesEventHandler(Guid groupId, int skip)
        {
            var messages = await GroupRepository.GetMessages(groupId, skip);

            return messages;
        }

        private void AddUserToOnlineList(string connectionId, string userEmail)
        {
            var userConnection = ConnectedUsers.FirstOrDefault(pair => pair.Value == userEmail);
            if (userConnection.Key != null)
            {
                Clients.Client(userConnection.Key).CloseConnection();
            }
            ConnectedUsers.Add(connectionId, userEmail);
        }

        private void NotifyLeaveGroupReceipents(IEnumerable<string> receipents, Guid chatId, string userEmail)
        {
            foreach (var receipent in receipents)
            {
                var userConnection = ConnectedUsers.FirstOrDefault(pair => pair.Value == receipent).Key;
                if (userConnection != null)
                {
                    Clients.Client(userConnection).UserLeftGroup(chatId, userEmail);
                }
            }
        }

        private void NotifyMessageReceivedReceipents(IEnumerable<string> receipents, Guid chatId, Message message)
        {
            foreach (var receipent in receipents)
            {
                var userConnection = ConnectedUsers.FirstOrDefault(pair => pair.Value == receipent).Key;

                if (message.Type == MessageType.File)
                {
                    if (userConnection != null)
                    {
                        Clients.Client(userConnection).MessageReceived(chatId, message);
                    }
                }
                else
                {
                    if (userConnection != null && ConnectedUsers[userConnection] != message.SenderEmail)
                    {
                        Clients.Client(userConnection).MessageReceived(chatId, message);
                    }
                }
            }
        }
    }
}