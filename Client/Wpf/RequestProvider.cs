﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using Domain.Models;
using Microsoft.AspNet.SignalR.Client;

namespace Client.Wpf
{
    public class RequestProvider
    {
        
        public static HubConnection HubConnection = new HubConnection($"{ConfigurationManager.AppSettings["ip"]}:{ConfigurationManager.AppSettings["port"]}");
        public static IHubProxy HubProxy;
        
        public static void AppendGlobalConnectionCloseHandler(Action action)
        {
            HubProxy.On("CloseConnection", action);
        }

        public static void AppendRoomInviteHandler(Action<Group, byte[]> action)
        {
            HubProxy.On<Group, byte[]>("RoomInvite", action);
        }

        public static void AppendUserLeftGroupHandler(Action<Guid, string> action)
        {
            HubProxy.On<Guid, string>("UserLeftGroup", action);
        }

        public static void AppendMessageReceivedHandler(Action<Guid, Message> action)
        {
            HubProxy.On<Guid, Message>("MessageReceived", action);
        }

        public static async Task<OperationResponse<ChatUserViewModel>> AddToContacts(string contactEmail)
        {
            return await HubProxy.Invoke<OperationResponse<ChatUserViewModel>>("AddToContacts", contactEmail);
        }
        
        public static async void RemoveFromContacts(string contactEmail)
        {
            await HubProxy.Invoke("RemoveFromContacts", contactEmail);
        }

        public static void CreateGroup(string owner, string name, GroupType type, IEnumerable<string> receipents)
        {
            HubProxy.Invoke("CreateGroup", owner, name, type, receipents);
        }
        
        public static async Task<List<ChatUserViewModel>> GetUsersData(IEnumerable<string> contactsToFind)
        {
            return await HubProxy.Invoke<List<ChatUserViewModel>>("GetUsersData", contactsToFind);
        }
        
        public static void CloseAllConnections()
        {
            HubConnection.Stop();
        }
        
        public static void SendMessage(Guid groupId, string content)
        {
            HubProxy.Invoke("SendMessage", groupId, content);
        }

        public static void SendFile(string fileName, byte[] data, Guid groupId)
        {
            HubProxy.Invoke(fileName, data, groupId);
        }
        
        public static async Task<OperationResponse<byte[]>> DownloadFile(Guid groupId, Guid messageId)
        {
            return await HubProxy.Invoke<OperationResponse<byte[]>>("DownloadFile", groupId, messageId);
        }

        public static async void LeaveGroup(Guid id)
        {
            await HubProxy.Invoke("LeaveGroup", id);
        }

        public static async Task<ConnectionData> Connect(string accessToken)
        {
            if (HubProxy == null)
            {
                HubProxy = HubConnection.CreateHubProxy("ChatHub");
            }

            HubConnection.Headers.Remove("Authorization");
            HubConnection.Headers.Add("Authorization", $"Bearer {accessToken}");

//            hubConnection.Closed += () => MessageBox.Show("Connection closed");
//            hubConnection.Error += (e) => MessageBox.Show($"Error {e.Message}");

            await HubConnection.Start();

            var connectData = await HubProxy.Invoke<ConnectionData>("Connect");

            return connectData;
        }

        public static async Task<IEnumerable<SearchResult>> Search(string groupName)
        {
            return await HubProxy.Invoke<IEnumerable<SearchResult>>("Search", groupName);
        }
    }
}