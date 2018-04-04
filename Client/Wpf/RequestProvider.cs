using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows;
using Domain.Models;
using Microsoft.AspNet.SignalR.Client;

namespace Client.Wpf
{
    public class RequestProvider
    {
        
//        public static HubConnection hubConnection = new HubConnection("http://localhost:42512/");
        public static string a = $"{ConfigurationManager.AppSettings["ip"]}:{ConfigurationManager.AppSettings["port"]}";
        public static HubConnection hubConnection = new HubConnection($"{ConfigurationManager.AppSettings["ip"]}:{ConfigurationManager.AppSettings["port"]}");
        public static IHubProxy hubProxy;
        
//        public static void AppendGlobalConnectionCloseHandler(NetworkComms.ConnectionEstablishShutdownDelegate action)
//        {
//            NetworkComms.AppendGlobalConnectionCloseHandler(action);
//        }

//        public static void AppendGlobalConnectionEstablishHandler(NetworkComms.ConnectionEstablishShutdownDelegate action)
//        {
//            NetworkComms.AppendGlobalConnectionCloseHandler(action);
//        }

        public static void AppendRoomInviteHandler(Action<Group, byte[]> action)
        {
            hubProxy.On<Group, byte[]>("RoomInvite", action);
        }

        public static void AppendUserLeftGroupHandler(Action<Guid, string> action)
        {
            hubProxy.On<Guid, string>("UserLeftGroup", action);
        }

        public static void AppendMessageReceivedHandler(Action<Guid, Message> action)
        {
            hubProxy.On<Guid, Message>("MessageReceived", action);
        }

        public static async Task<OperationResponse<ChatUserViewModel>> AddToContacts(string contactEmail)
        {
            return await hubProxy.Invoke<OperationResponse<ChatUserViewModel>>("AddToContacts", contactEmail);
        }
        
        public static async void RemoveFromContacts(string contactEmail)
        {
            await hubProxy.Invoke("RemoveFromContacts", contactEmail);
        }

        public static void CreateGroup(string owner, string name, GroupType type, IEnumerable<string> receipents)
        {
            hubProxy.Invoke("CreateGroup", owner, name, type, receipents);
        }
        
        public static async Task<List<ChatUserViewModel>> GetUsersData(IEnumerable<string> contactsToFind)
        {
            return await hubProxy.Invoke<List<ChatUserViewModel>>("GetUsersData", contactsToFind);
        }
        
        public static void CloseAllConnections()
        {
            hubConnection.Stop();
        }
        
        public static void SendMessage(Guid groupId, string content)
        {
            hubProxy.Invoke("SendMessage", groupId, content);
        }

        public static void SendFile(string fileName, byte[] data, Guid groupId)
        {
            hubProxy.Invoke(fileName, data, groupId);
        }
        
        public static async Task<OperationResponse<byte[]>> DownloadFile(Guid groupId, Guid messageId)
        {
            return await hubProxy.Invoke<OperationResponse<byte[]>>("DownloadFile", groupId, messageId);
        }

        public static async void LeaveGroup(Guid id)
        {
            await hubProxy.Invoke("LeaveGroup", id);
        }

        public static async Task<ConnectionData> Connect(string accessToken)
        {
            if (hubProxy == null)
            {
                hubProxy = hubConnection.CreateHubProxy("ChatHub");
            }

            hubConnection.Headers.Remove("Authorization");
            hubConnection.Headers.Add("Authorization", $"Bearer {accessToken}");

//            hubConnection.Closed += () => MessageBox.Show("Connection closed");
//            hubConnection.Error += (e) => MessageBox.Show($"Error {e.Message}");

            await hubConnection.Start();

            var connectData = await hubProxy.Invoke<ConnectionData>("Connect");

            return connectData;
        }

//        public static OperationResponse<IEnumerable<Group>> UpdateGroupsList(IEnumerable<Guid> groupsToUpdate)
//        {
//            return NetworkComms.SendReceiveObject<IEnumerable<Guid>, OperationResponse<IEnumerable<Group>>>
//                ("GetGroupsData", HostInfo.Ip, HostInfo.Port, "GetGroupsData_response", 5000, groupsToUpdate);
//        }
    }
}