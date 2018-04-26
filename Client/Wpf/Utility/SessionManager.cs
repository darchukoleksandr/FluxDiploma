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

        public static async Task<ChatUserViewModel> AddToContacts(string contactEmail)
        {
            var operationResponse = await RequestProvider.AddToContacts(contactEmail);

            if (operationResponse.IsErrorOccured())
            {
                throw new Exception(operationResponse.Error);
            }
            else
            {
                UserContacts.Add(operationResponse.Response);
                LoggedUser.Contacts.Add(contactEmail);
            }

            return operationResponse.Response;
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
            var contact = UserContacts.First(user => user.Email == contactEmail);
            RequestProvider.RemoveFromContacts(contactEmail);
            UserContacts.Remove(contact);
        }

        public static Task<IEnumerable<SearchResult>> Search(string groupName)
        {
            return RequestProvider.Search(groupName);
        }
    }
}
