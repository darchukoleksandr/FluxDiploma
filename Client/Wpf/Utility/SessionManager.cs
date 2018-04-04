using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Client.Wpf.Windows;
using Domain.Crypto;
using Domain.Models;

namespace Client.Wpf.Utility
{
    public class SessionManager
    {
        public static User LoggedUser { get; set; }

        public static IEnumerable<Group> UserGroups { get; set; }

        public static ICollection<ChatUserViewModel> UserContacts { get; set; }
        /// <summary>
        /// Stores profile data for all users
        /// </summary>
        public static IEnumerable<ChatUserViewModel> UsersDataCache { get; set; }

        public static async void UpdateUserContacts()
        {
            var operationResponse = await RequestProvider.GetUsersData(LoggedUser.Contacts.ToArray());

            UserContacts = operationResponse;
        }

//        public static void UpdateUserGroups()
//        {
//            var operationResponse = RequestProvider.UpdateGroupsList(
//                LoggedUser.PrivateKeys.Select(info => info.GroupId).ToArray());
//            
//            if (!operationResponse.IsErrorOccured())
//            {
//                UserGroups = operationResponse.Response;
//            }
//        }

        public static async void AddToContacts(string contactEmail)
        {
            var operationResponse = await RequestProvider.AddToContacts(contactEmail);

            if (operationResponse.IsErrorOccured())
            {
                throw new Exception(operationResponse.Error);
            }

            LoggedUser.Contacts.Add(contactEmail);
            UserContacts.Add(operationResponse.Response);

//            UpdateUserContacts();
//            UsersDataCache = UsersDataCache.Intersect(operationResponse.Response);
        }

        public static async Task<ChatUserViewModel> GetUsersData(string userEmail)
        {
            var operationResponse = await RequestProvider.GetUsersData(new [] { userEmail });

            return operationResponse.First();
//            UsersDataCache = UsersDataCache.Intersect(operationResponse.Response);
        }

        public static async Task LogOut()
        {
            await IsolatedStorageManager.DeleteOauthTokens();
            RequestProvider.CloseAllConnections();

            LoggedUser = null;
            UserGroups = null;
            UserContacts = null;

            App.ChangeMainWindow(new Preloader());
        }

        public static void LeaveGroup(Guid id)
        {
            RequestProvider.LeaveGroup(id);
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

        public static void RemoveFromContacts(string contactEmail)
        {
            RequestProvider.RemoveFromContacts(contactEmail);
        }
    }
}
