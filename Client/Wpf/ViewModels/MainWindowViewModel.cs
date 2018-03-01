using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Domain.Models;

namespace Client.Wpf.ViewModels
{
    internal class MainWindowViewModel : INotifyPropertyChanged
    {
        public MainWindowViewModel()
        {
//            Contacts = SessionManager.UserContacts ?? new List<ChatUserViewModel>();
//            Groups = SessionManager.UserGroups ?? new List<Group>();
//            User = SessionManager.LoggedUser;
            SelectedGroupDecryptedMessages = new ObservableCollection<Message>();
//            SelectedGroupDecryptedMessages = new ObservableCollection<Message>{new Message
//            {
//                Content = "Testttt", Sended = DateTime.Now, SenderEmail = "darchukoleksandr@gmail.com", Type = Type.Plain
//            }, new Message
//                {
//                    Content = "TesttttTesttttTesttttTesttttTesttttTesttttTesttttTesttttTesttttTesttttTesttttTesttttTesttttTestttt", Sended = DateTime.Now, SenderEmail = "darchukoleksandr@gmail.com", Type = Type.Plain
//                }};
            InitializeData();
        }

        private async void InitializeData()
        {
            var valueTuple = await Utility.IsolatedStorageManager.ReadSavedOauthTokens();

            AccessToken = valueTuple.Item1;
            IdentityToken = valueTuple.Item2;
        }
        
        public string IdentityToken
        {
            get => _identityToken;
            set
            {
                _identityToken = value;
                OnPropertyChanged(nameof(IdentityToken));
            }
        }
        private string _identityToken;

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

        public IEnumerable<ChatUserViewModel> Contacts
        {
            get => _contacts;
            set
            {
                _contacts = value;
                OnPropertyChanged(nameof(Contacts));
            }
        }
        private IEnumerable<ChatUserViewModel> _contacts;

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

        public ChatUserViewModel SelectedContactUser
        {
            get => _selectedContactUser;
            set
            {
                _selectedContactUser = value;
                OnPropertyChanged(nameof(SelectedContactUser));
            }
        }
        private ChatUserViewModel _selectedContactUser;

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
