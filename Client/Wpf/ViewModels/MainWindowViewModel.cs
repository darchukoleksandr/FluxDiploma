using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Domain.Models;

namespace Client.Wpf.ViewModels
{
    class UserViewModel
    {
        public UserViewModel(string email, IEnumerable<TypeValueClaim> claims)
        {
            Email = email;
            Claims = claims;
        }
        public string Email { get; }
        public IEnumerable<TypeValueClaim> Claims { get; }
        public string UserName { 
            get => GetClaim(nameof(UserName));
            set => SetClaim(nameof(UserName), value);
        }

        public string FirstName { 
            get => GetClaim(nameof(FirstName));
            set => SetClaim(nameof(FirstName), value);
        }

        public string LastName { 
            get => GetClaim(nameof(LastName));
            set => SetClaim(nameof(LastName), value);
        }

        public string Picture { 
            get => GetClaim(nameof(Picture));
            set => SetClaim(nameof(Picture), value);
        }

        private string GetClaim(string type)
        {
            return Claims.FirstOrDefault(claim => claim.Type == type)?.Value;
        }
        private void SetClaim(string type, string value)
        {
            var claims = Claims.First(claim => claim.Type == type);
            claims.Value = value;
        }
    }

    internal class MainWindowViewModel : INotifyPropertyChanged
    {
        public MainWindowViewModel()
        {
            SelectedGroupDecryptedMessages = new ObservableCollection<Message>();
            InitializeData();
        }

        private async void InitializeData()
        {
            var accessToken = await Utility.IsolatedStorageManager.ReadSavedAccessTokens();

            AccessToken = accessToken;
        }
        
        public string AccessToken
        {
            get => _accessToken;
            set
            {
                _accessToken = value;
                OnPropertyChanged(nameof(AccessToken));
            }
        }
        private string _accessToken;

        public ICollection<UserViewModel> Contacts
        {
            get => _contacts;
            set
            {
                _contacts = value;
                OnPropertyChanged(nameof(Contacts));
            }
        }
        private ICollection<UserViewModel> _contacts;
//        public ICollection<ChatUserViewModel> Contacts
//        {
//            get => _contacts;
//            set
//            {
//                _contacts = value;
//                OnPropertyChanged(nameof(Contacts));
//            }
//        }
//        private ICollection<ChatUserViewModel> _contacts;

        public IEnumerable<Group> Groups
        {
            get => _groups;
            set
            {
                _groups = value;
                OnPropertyChanged(nameof(Groups));
            }
        }
        private IEnumerable<Group> _groups;

        public User User
        {
            get => _user;
            set
            {
                _user = value;
                OnPropertyChanged(nameof(User));
            }
        }
        private User _user;

//        public ChatUserViewModel SelectedContactUser
//        {
//            get => _selectedContactUser;
//            set
//            {
//                _selectedContactUser = value;
//                OnPropertyChanged(nameof(SelectedContactUser));
//            }
//        }
//        private ChatUserViewModel _selectedContactUser;
        public UserViewModel SelectedContactUser
        {
            get => _selectedContactUser;
            set
            {
                _selectedContactUser = value;
                OnPropertyChanged(nameof(SelectedContactUser));
            }
        }
        private UserViewModel _selectedContactUser;

        public Group SelectedGroup
        {
            get => _group;
            set
            {
                _group = value;
                OnPropertyChanged(nameof(SelectedGroup));
            }
        }
        private Group _group;

        public ICollection<Message> SelectedGroupDecryptedMessages
        {
            get => _selectedGroupDecryptedMessages;
            set
            {
                _selectedGroupDecryptedMessages = value;
                OnPropertyChanged(nameof(SelectedGroupDecryptedMessages));
            }
        }
        private ICollection<Message> _selectedGroupDecryptedMessages;

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
