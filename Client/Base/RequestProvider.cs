using System;
using System.Collections.Generic;
using Domain;
using Domain.Models;
using NetworkCommsDotNet;
using NetworkCommsDotNet.DPSBase;

namespace Client.Base
{
    public class RequestProvider
    {
        static RequestProvider()
        {
            var dataSerializer = DPSManager.GetDataSerializer<ProtobufSerializer>();
            var dataProcessors = new List<DataProcessor>();
            var dataProcessorOptions = new Dictionary<string, string>();
            NetworkComms.DefaultSendReceiveOptions = new SendReceiveOptions(dataSerializer, dataProcessors, dataProcessorOptions);
        }

        public static void AppendGlobalConnectionCloseHandler(NetworkComms.ConnectionEstablishShutdownDelegate action)
        {
            NetworkComms.AppendGlobalConnectionCloseHandler(action);
        }

        public static void AppendGlobalConnectionEstablishHandler(NetworkComms.ConnectionEstablishShutdownDelegate action)
        {
            NetworkComms.AppendGlobalConnectionCloseHandler(action);
        }

        public static void AppendRoomInvitePacketHandler(NetworkComms.PacketHandlerCallBackDelegate<(Group, byte[])> action)
        {
            NetworkComms.AppendGlobalIncomingPacketHandler("RoomInvite", action);
        }

        public static void AppendLeaveGroupPacketHandler(NetworkComms.PacketHandlerCallBackDelegate<(Guid, string)> action)
        {
            NetworkComms.AppendGlobalIncomingPacketHandler("LeaveGroup", action);
        }

        public static void AppendMessageReceivedPacketHandler(NetworkComms.PacketHandlerCallBackDelegate<Message> action)
        {
            NetworkComms.AppendGlobalIncomingPacketHandler("MessageReceived", action);
        }

        public static void CreateRoom(ValueTuple<string, string, bool, IEnumerable<string>> requestData)
        {
            NetworkComms.SendObject(
                "CreateRoom", HostInfo.Ip, HostInfo.Port, requestData);
        }

        public static OperationResponse<User> Connect(string accessToken, string identityToken)
        {
            return NetworkComms.SendReceiveObject<ValueTuple<string, string>, OperationResponse<User>>(
                "Connect", HostInfo.Ip, HostInfo.Port, "Connect_response", 10000,
                new ValueTuple<string, string>(accessToken, identityToken));
        }

        public static OperationResponse<IEnumerable<ChatUserViewModel>> GetUsersData(IEnumerable<string> contactsToFind)
        {
            return NetworkComms.SendReceiveObject<IEnumerable<string>, OperationResponse<IEnumerable<ChatUserViewModel>>>
                ("GetUsersData", HostInfo.Ip, HostInfo.Port, "GetUsersData_response", 5000, 
                contactsToFind);
        }

        public static OperationResponse<IEnumerable<Group>> UpdateGroupsList(IEnumerable<Guid> groupsToUpdate)
        {
            return NetworkComms.SendReceiveObject<IEnumerable<Guid>, OperationResponse<IEnumerable<Group>>>
                ("GetGroupsData", HostInfo.Ip, HostInfo.Port, "GetGroupsData_response", 5000, groupsToUpdate);
        }

        public static OperationResponse<User> RemoveFromContacts(string userEmail, string contactEmail)
        {
            return NetworkComms.SendReceiveObject<ValueTuple<string, string>, OperationResponse<User>>
                ("RemoveFromContacts", HostInfo.Ip, HostInfo.Port, "RemoveFromContacts_response", 5000, 
                new ValueTuple<string, string>(userEmail, contactEmail));
        }

        public static OperationResponse<byte[]> DownloadFile(Guid groupId, Guid messageId)
        {
            return NetworkComms.SendReceiveObject<ValueTuple<Guid, Guid>, OperationResponse<byte[]>>(
                "DownloadFile", HostInfo.Ip, HostInfo.Port, "DownloadFile_response", 10000, (groupId, messageId));
        }

        public static void SendFile(ValueTuple<string, string, byte[], Guid> request)
        {
            NetworkComms.SendObject("SendFile", HostInfo.Ip, HostInfo.Port, request);
        }

        public static void SendMessage(Guid chatGroupId, string textToSend)
        {
            NetworkComms.SendObject("SendMessage", HostInfo.Ip, HostInfo.Port, 
                new ValueTuple<Guid, string>(chatGroupId, textToSend));
        }

        public static OperationResponse<User> AddToContacts(string userEmail, string contactEmail)
        {
            return NetworkComms.SendReceiveObject<ValueTuple<string, string>, OperationResponse<User>>
            ("AddToContacts", HostInfo.Ip, HostInfo.Port, "AddToContacts_response", 5000, 
                new ValueTuple<string, string>(userEmail, contactEmail));
        }

        public static void CloseAllConnections()
        {
            NetworkComms.CloseAllConnections();
        }

        public static void LeaveGroup(Guid id, string loggedUserEmail)
        {
            NetworkComms.SendObject("LeaveGroup", HostInfo.Ip, HostInfo.Port, 
                new ValueTuple<Guid, string>(id, loggedUserEmail));
        }
    }
}