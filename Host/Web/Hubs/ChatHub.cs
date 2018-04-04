﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Auth0.Core.Exceptions;
using DAL.MongoDb.Repository;
using Domain.Crypto;
using Domain.Models;
using Domain.Repository;
using Host.Web.Utils;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Logging;
using Org.BouncyCastle.Bcpg;

namespace Host.Web.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private static readonly string[] ScopesToSave = { "nickname", "picture", "gender", "firstname", "lastname" };
        private static readonly IUserRepository UserRepository = new UserRepository();
        private static readonly IGroupRepository GroupRepository = new GroupRepository();
        private static readonly Dictionary<string, string> ConnectedUsers = new Dictionary<string, string>();
        private readonly ILogger _logger = LoggerFactory.Default.Create(nameof(ChatHub));

        public override Task OnConnected()
        {
            _logger.WriteVerbose($"Connection opened: {Context.ConnectionId}");
            return base.OnConnected();
        }

        public async Task<ConnectionData> Connect()
        {
//            _logger.WriteVerbose($"Connection opened: {Context.ConnectionId}");
            var result = new ConnectionData();

            var accesToken = Context.Headers.Get("Authorization").Replace("Bearer ", String.Empty);

            if (string.IsNullOrEmpty(accesToken))
            {
                Console.WriteLine("No access token provided!");
                throw new ArgumentException("NO ACCESS TOKEN");
            }

            IEnumerable<TypeValueClaim> userClaims;
            try
            {
                userClaims = await Jwt.ReadUserClaims(accesToken);
            }
            catch (ApiException)
            {
                Console.WriteLine();
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

            result.User = user;
            result.Groups = new List<Group>();
            foreach (var chatRoomId in user.PrivateKeys)
            {
                var chatRoom = await GroupRepository.GetByIdIncludeMessages(chatRoomId.GroupId);
                result.Groups.Add(chatRoom);
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
        
        public async Task<OperationResponse<byte[]>> DownloadFile(Guid groupId, Guid messageId)
        {
            var message = await GroupRepository.GetMessageById(groupId, messageId);

            var result = new OperationResponse<byte[]>();
            var filePath = Path.Combine("Files",message.SenderEmail, message.Content);

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

        public async void SendFile(string fileName, byte[] data, Guid groupId)
        {
            var sender = ConnectedUsers[Context.ConnectionId];

            if (!Directory.Exists(Path.Combine("Files", sender)))
            {
                Directory.CreateDirectory(Path.Combine("Files", sender));
            }

            var receipents = await GroupRepository.GetReceipents(groupId);
            
            using (var fileStream = File.Create(Path.Combine("Files", sender, $"{Guid.NewGuid()}.{fileName}")))
            {
                await fileStream.WriteAsync(data, 0, data.Length);

                Console.WriteLine($"SendFile request: {Path.GetFileName(fileStream.Name)}!");

                var message = new Message
                {
                    SenderEmail = sender,
                    Content = Path.GetFileName(fileStream.Name),
                    Sended = DateTime.Now,
                    Type = MessageType.File
                };

                await GroupRepository.InsertMessage(groupId, message);

                NotifyMessageReceivedReceipents(receipents, groupId, message);
            }
        }

        public async void SendMessage(Guid groupId, string content)
        {
            var receipents = await GroupRepository.GetReceipents(groupId);
            var sender = ConnectedUsers[Context.ConnectionId];

            var message = new Message
            {
                SenderEmail = sender,
                Content = content,
                Sended = DateTime.Now,
                Type = MessageType.Plain
            };

            await GroupRepository.InsertMessage(groupId, message);

            Console.WriteLine($"Message from {sender}: {content}");

            NotifyMessageReceivedReceipents(receipents, groupId, message);
        }

        public async void CreateGroup(string owner, string name, GroupType type, IEnumerable<string> receipents)
        {
            Console.WriteLine($"CreateGroup starting!");

            var userGroupPrivateKeys = new List<UserGroupPrivateKeyInfo>();
            var groupUsers = new List<GroupUserPublicKey>();
            
            var group = new Group
            {
                UsersPublicKeys = groupUsers,
                Owner = owner,
                Name = name,
                Type = type
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

            await GroupRepository.Create(group);

            foreach (var chatUser in userGroupPrivateKeys)
            {
                await UserRepository.AddPrivateKey(chatUser.Email, chatUser.PrivateKey, group.Id);

                var userConnection = ConnectedUsers.FirstOrDefault(pair => pair.Value == chatUser.Email).Key;
                if (!string.IsNullOrEmpty(userConnection))
                {
                    Clients.Client(userConnection).RoomInvite(group, chatUser.PrivateKey);
                }
            }

            Console.WriteLine($"Chat room {group.Name} created with {groupUsers.Count} users!");
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
            if (userConnection.Key == null)
            {
                ConnectedUsers.Add(connectionId, userEmail);
            }
            else
            {
//                Clients.Client(userConnection.Key)
//                userConnection.Key.CloseConnection(false);
            }
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
                if (userConnection != null && ConnectedUsers[userConnection] != message.SenderEmail)
//                if (userConnection != null)
                {
                    Clients.Client(userConnection).MessageReceived(chatId, chatId, message);
                }
            }
        }
    }
}