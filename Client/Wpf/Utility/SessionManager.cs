using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Client.Base;
using Domain.Crypto;
using Domain.Models;

namespace Client.Wpf.Utility
{
    public class SessionManager
    {
        public static User LoggedUser { get; set; }

        public static IEnumerable<Group> UserGroups { get; set; }

        public static IEnumerable<ChatUserViewModel> UserContacts { get; set; }
        /// <summary>
        /// Stores profile data for all users
        /// </summary>
        public static IEnumerable<ChatUserViewModel> UsersDataCache { get; set; } = new Collection<ChatUserViewModel>();

        public static void UpdateUserContacts()
        {
            var operationResponse = RequestProvider.GetUsersData(LoggedUser.Contacts.ToArray());

            if (!operationResponse.IsErrorOccured())
            {
                UserContacts = operationResponse.Response;
            }
        }

        public static void UpdateUserGroups()
        {
            var operationResponse = RequestProvider.UpdateGroupsList(
                LoggedUser.PrivateKeys.Select(info => info.GroupId).ToArray());
            
            if (!operationResponse.IsErrorOccured())
            {
                UserGroups = operationResponse.Response;
            }
        }

        public static void AddToContacts(string userEmail)
        {
            var operationResponse = RequestProvider.AddToContacts(LoggedUser.Email, userEmail);
            
            LoggedUser = operationResponse.Response;

            UpdateUserContacts();
//            UsersDataCache = UsersDataCache.Intersect(operationResponse.Response);
        }

        public static ChatUserViewModel GetUsersData(string userEmail)
        {
            var operationResponse = RequestProvider.GetUsersData(new [] { userEmail });

            return operationResponse.Response.First();
//            UsersDataCache = UsersDataCache.Intersect(operationResponse.Response);
        }

        public static async Task LogOut()
        {
            await IsolatedStorageManager.DeleteOauthTokens();
            RequestProvider.CloseAllConnections();

            LoggedUser = null;
            UserGroups = null;
            UserContacts = null;
        }

        public static void LeaveGroup(Guid id)
        {
            RequestProvider.LeaveGroup(id, LoggedUser.Email);
        }

        public static string DecryptMessage(Guid groupId, string chiperText)
        {
            return new Pgp().DecryptString(chiperText,
                LoggedUser.PrivateKeys.First(key => key.GroupId == groupId).PrivateKey);
        }

        public static void AddPrivateKey(Guid groupId, byte[] privateKey)
        {
            LoggedUser.PrivateKeys.Add(new GroupUserPrivateKey
            {
                GroupId = groupId,
                PrivateKey = privateKey
            });
        }
    }
}
