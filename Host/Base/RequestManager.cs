using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using DAL.MongoDb.Repository;
using Domain;
using Domain.Crypto;
using Domain.Models;
using Microsoft.IdentityModel.Tokens;
using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using NetworkCommsDotNet.DPSBase;
using NetworkCommsDotNet.Tools;
using Org.BouncyCastle.Bcpg;

namespace Host.Base
{
    public class RequestManager
    {
        private readonly string[] _scopesToSave = { "name", "picture", "gender", "phone" };
        private readonly UserRepository _userRepository = new UserRepository();
        private readonly GroupRepository _groupRepository = new GroupRepository();
        private readonly Dictionary<Connection, string> _connectedUsers = new Dictionary<Connection, string>();

        public void StopListening()
        {
            NetworkComms.CloseAllConnections();
            NetworkComms.Shutdown();
        }

        public RequestManager()
        {
            NetworkComms.AppendGlobalConnectionCloseHandler(connection =>
            {
                _connectedUsers.Remove(connection);
                Console.WriteLine($"Connection closed: {connection.ConnectionInfo.NetworkIdentifier.Value}");
            });

            NetworkComms.AppendGlobalIncomingPacketHandler<ValueTuple<string, string>>
                ("Connect", ConnectEventHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<ValueTuple<Guid, string>>
                ("SendMessage", SendMessageEventHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<ValueTuple<string, string, bool, IEnumerable<string>>>
                ("CreateRoom", CreateRoomEventHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<ValueTuple<string, string>>
                ("AddToContacts", AddToContactsEventHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<IEnumerable<Guid>>
                ("GetGroupsData", GetGroupsDataEventHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<IEnumerable<string>>
                ("GetUsersData", GetUsersData);
            NetworkComms.AppendGlobalIncomingPacketHandler<ValueTuple<string, string>>
                ("RemoveFromContacts", RemoveFromContactsEventHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<ValueTuple<string, string, byte[], Guid>>
                ("SendFile", SendFileEventHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<ValueTuple<Guid, int>>
                ("GetGroupMessages", GetGroupMessagesEventHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<ValueTuple<Guid, Guid>>
                ("DownloadFile", DownloadFileEventHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<ValueTuple<Guid, string>>
                ("LeaveGroup", LeaveGroupEventHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<ValueTuple<Guid, string>>
                ("LeaveGroup", LeaveGroupEventHandler);

            var dataSerializer = DPSManager.GetDataSerializer<ProtobufSerializer>();
            var dataProcessors = new List<DataProcessor>();
            var dataProcessorOptions = new Dictionary<string, string>();
            NetworkComms.DefaultSendReceiveOptions = new SendReceiveOptions(dataSerializer, dataProcessors, dataProcessorOptions);

            Connection.StartListening(ConnectionType.TCP, new IPEndPoint(IPAddress.Parse("127.0.0.1"), 42512));
        }

        private async void LeaveGroupEventHandler(PacketHeader packetheader, Connection connection, (Guid, string) request)
        {
            await _groupRepository.LeaveGroup(request.Item1, request.Item2);
            var receipents = await _groupRepository.GetReceipents(request.Item1);

            NotifyLeaveGroupReceipents(receipents, request.Item1, request.Item2);

            Console.WriteLine($"LeaveGroup request from: {request.Item2}!");
        }

        private async void GetGroupMessagesEventHandler(PacketHeader packetheader, Connection connection, (Guid, int) request)
        {
            var messages = await _groupRepository.GetMessages(request.Item1, request.Item2);
            
            connection.SendObject("GetGroupMessages_response", messages);
        }

        private async void DownloadFileEventHandler(PacketHeader packetheader, Connection connection, 
            ValueTuple<Guid, Guid> request)
        {
            var message = await _groupRepository.GetMessageById(request.Item1, request.Item2);

            var result = new OperationResponse<byte[]>();
            var filePath = Path.Combine("Files",message.SenderEmail, message.Content);

            if (!File.Exists(filePath))
            {
                result.Error = "File not exits!";
            }

            using (var fileStream = File.OpenRead(filePath))
            {
                using (var memoryStream = new MemoryStream())
                {
                    await fileStream.CopyToAsync(memoryStream);
                    result.Response = memoryStream.ToArray();
                }
            }

            connection.SendObject("DownloadFile_response", result);
        }

        private async void SendFileEventHandler(PacketHeader packetheader, Connection connection, 
            (string, string, byte[], Guid) request)
        {
            if (!Directory.Exists(Path.Combine("Files", request.Item1)))
            {
                Directory.CreateDirectory(Path.Combine("Files", request.Item1));
            }

            var receipents = await _groupRepository.GetReceipents(request.Item4);
            
            using (var fileStream = File.Create(Path.Combine("Files", request.Item1, $"{ShortGuid.NewGuid()}.{request.Item2}")))
            {
                await fileStream.WriteAsync(request.Item3, 0, request.Item3.Length);

                Console.WriteLine($"SendFile request: {Path.GetFileName(fileStream.Name)}!");

                var message = new Message
                {
                    SenderEmail = request.Item1,
                    Content = Path.GetFileName(fileStream.Name),
                    Sended = DateTime.Now,
                    Type = MessageType.File
                };

                await _groupRepository.InsertMessage(request.Item4, message);

                NotifyMessageReceivedReceipents(receipents, request.Item4, message);
            }
        }

        private async void AddToContactsEventHandler(PacketHeader packetHeader, Connection connection, (string, string) request)
        {
            var result = new OperationResponse<User>();

            var savedUser = await _userRepository.GetByEmail(request.Item1);
            var contactUser = await _userRepository.GetByEmail(request.Item2);

            if (request.Item1.Equals(request.Item2))
            {
                result.Error = "Can't add yourself to contacts!";
                connection.SendObject("AddToContacts_response", result);

                Console.WriteLine($"AddToContacts request: {request.Item2} can't add yourself to contacts!");
            }
            if (savedUser.Contacts.Contains(request.Item2))
            {
                result.Error = "Contact user already in list!";
                connection.SendObject("AddToContacts_response", result);

                Console.WriteLine($"AddToContacts request: {request.Item2} already in list!");
            }
            else if (contactUser == null)
            {
                result.Error = "Contact user was not found!";
                connection.SendObject("AddToContacts_response", result);

                Console.WriteLine($"AddToContacts request: user {request.Item2} not found!");
            }
            else
            { 
                _userRepository.AddContact(request.Item1, request.Item2);
                savedUser.Contacts.Add(contactUser.Email);
                result.Response = savedUser;
                connection.SendObject("AddToContacts_response", result);

                Console.WriteLine($"AddToContacts request: {savedUser.Email} added {contactUser.Email}!");
            }
        }

        private async void CreateRoomEventHandler(PacketHeader packetHeader, Connection connection, (string, string, bool, IEnumerable<string>) request)
        {
            var userGroupPrivateKeys = new List<UserGroupPrivateKeyInfo>();
            var groupUsers = new List<GroupUserPublicKey>();

            GroupType groupType;
            var chatRoom = new Group
            {
                UsersPublicKeys = groupUsers,
                Owner = request.Item1,
                Name = request.Item2
            };

            if (Enum.TryParse(request.Item3.ToString(), out groupType))
            {
                chatRoom.Type = groupType;
            }
            else
            {
                chatRoom.Type = GroupType.Open;
            }

            foreach (var chatUserEmail in request.Item4)
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

            await _groupRepository.Create(chatRoom);

            foreach (var chatUser in userGroupPrivateKeys)
            {
                await _userRepository.AddPrivateKey(chatUser.Email, chatUser.PrivateKey, chatRoom.Id);

                _connectedUsers.FirstOrDefault(pair => pair.Value == chatUser.Email)
                    .Key?.SendObject<(Group, byte[])>("RoomInvite", (chatRoom, chatUser.PrivateKey));
            }

            Console.WriteLine($"Chat room {chatRoom.Name} created with {groupUsers.Count} users!");
        }

        private async void GetGroupsDataEventHandler(PacketHeader packetHeader, Connection connection, IEnumerable<Guid> chatRoomIdsList)
        {
            var groups = new List<Group>();
            var result = new OperationResponse<IEnumerable<Group>>
            {
                Response = groups
            };

            foreach (var chatRoomId in chatRoomIdsList)
            {
                var chatRoom = await _groupRepository.GetByIdIncludeMessages(chatRoomId);
                groups.Add(chatRoom);
            }

            connection.SendObject("GetGroupsData_response", result);

            Console.WriteLine("GetGroupsData request responsed!");
        }

        private async void GetUsersData(PacketHeader packetHeader, Connection connection, IEnumerable<string> userEmailList)
        {
            var userList = new List<ChatUserViewModel>();
            var result = new OperationResponse<IEnumerable<ChatUserViewModel>>
            {
                Response = userList
            };

            foreach (var userEmail in userEmailList)
            {
                var chatUser = await _userRepository.GetByEmailForContacts(userEmail);
                if (chatUser != null)
                {
                    userList.Add(chatUser);
                }
            }

            connection.SendObject("GetUsersData_response", result);

            Console.WriteLine($"GetUsersData request responsed!");
        }

        private async void RemoveFromContactsEventHandler(PacketHeader packetHeader, Connection connection, (string, string) request)
        {
            var result = new OperationResponse<User>();

            var savedUser = await _userRepository.GetByEmail(request.Item1);

            if (!savedUser.Contacts.Contains(request.Item2))
            {
                result.Error = "Contact user was not found in list!";
                connection.SendObject("RemoveFromContacts_response", result);

                Console.WriteLine($"RemoveFromContacts request: {request.Item2} not found in your list!");
            }
            else
            {
                await _userRepository.RemoveContact(request.Item1, request.Item2);
                savedUser.Contacts.Remove(request.Item2);
                result.Response = savedUser;
                connection.SendObject("RemoveFromContacts_response", result);

                Console.WriteLine($"RemoveFromContacts request: {savedUser.Email} removed {request.Item2}!");
            }
        }

        private async void SendMessageEventHandler(PacketHeader packetHeader, Connection connection, (Guid, string) request)
        {
            var receipents = await _groupRepository.GetReceipents(request.Item1);
            var sender = _connectedUsers[connection];

            var message = new Message
            {
                SenderEmail = sender,
                Content = request.Item2,
                Sended = DateTime.Now,
                Type = MessageType.Plain
            };

            await _groupRepository.InsertMessage(request.Item1, message);

            Console.WriteLine($"Message from {sender}: {request.Item2}");

            NotifyMessageReceivedReceipents(receipents, request.Item1, message);
        }

        private void NotifyLeaveGroupReceipents(IEnumerable<string> receipents, Guid chatId, string userEmail)
        {
            foreach (var receipent in receipents)
            {
                var userConnection = _connectedUsers.FirstOrDefault(pair => pair.Value == receipent).Key;
                if (userConnection != null)
                {
                    userConnection.SendObject<ValueTuple<Guid, string>>("LeaveGroup", (chatId, userEmail));
                }
            }
        }

        private void NotifyMessageReceivedReceipents(IEnumerable<string> receipents, Guid chatId, Message message)
        {
            foreach (var receipent in receipents)
            {
                var userConnection = _connectedUsers.FirstOrDefault(pair => pair.Value == receipent).Key;
                if (userConnection != null && _connectedUsers[userConnection] != message.SenderEmail)
//                if (userConnection != null)
                {
                    userConnection.SendObject<ValueTuple<Guid, Message>>("MessageReceived", (chatId, message));
                }
            }
        }

        private async void ConnectEventHandler(PacketHeader packetHeader, Connection connection, (string, string) tokens)
        {
            var result = new OperationResponse<User>();

            if (string.IsNullOrEmpty(tokens.Item1))
            {
                result.Error = "NullRefenceException";
                connection.SendObject("Connect_response", result);
                Console.WriteLine("Null value of access token!");
                connection.CloseConnection(true);
                return;
            }

            try
            {
                await JwtUtils.ValidateAccessToken(tokens.Item1);
            }
            catch (SecurityTokenExpiredException)
            {
                result.Error = "SecurityTokenExpiredException";
                connection.SendObject("Connect_response", result);
                Console.WriteLine("User's token expired!");
                connection.CloseConnection(true);
                return;
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("InvalidOperationException");
                connection.CloseConnection(true);
                return;
            }

            var userClaims = JwtUtils.ReadUserClaims(tokens.Item2);
            var userEmail = userClaims.First(claim => claim.Type == "email").Value;

            var user = await _userRepository.GetByEmail(userEmail);

            if (user == null)
            {
                user = new User
                {
                    Email = userEmail,
                    Claims = userClaims.Where(claim => _scopesToSave.Contains(claim.Type))
                        .Select(claim => new TypeValueClaim
                        {
                            Type = claim.Type,
                            Value = claim.Value
                        }).ToList()
                };

                await _userRepository.Create(user);
            }

            result.Response = user;
            try
            {
                connection.SendObject("Connect_response", result);
            }
            catch (CommunicationException) //connection is closed
            {
                return;
            }

            AddUserToOnlineList(connection, userEmail);

            Console.WriteLine($"User connected: {user.Email}");
        }

        private void AddUserToOnlineList(Connection connection, string userEmail)
        {
            var userConnection = _connectedUsers.FirstOrDefault(pair => pair.Value == userEmail);
            if (userConnection.Key == null)
            {
                _connectedUsers.Add(connection, userEmail);
            }
            else
            {
                userConnection.Key.CloseConnection(true);
            }
        }
    }
}