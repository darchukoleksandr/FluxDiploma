using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Client.Wpf.Utility;
using Domain.Models;

namespace Client.Wpf.ViewModels
{
    public class UserViewModel
    {
        public UserViewModel(string email, IEnumerable<TypeValueClaim> claims)
        {
            Email = email;
            Claims = claims;
        }
        public string Email { get; }
        public IEnumerable<TypeValueClaim> Claims { get; }

        public bool Is
        {
            get => SessionManager.UserContacts.Any(contact => contact.Email == Email);
        }

        public string FullName => $"{GetClaim(nameof(FirstName))} {GetClaim(nameof(LastName))}";

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

    public class MainWindowViewModel : INotifyPropertyChanged
    {

        public ObservableCollection<SearchResult> SearchResults { get; set; } = new ObservableCollection<SearchResult>();
            
        public ObservableCollection<UserViewModel> Contacts { get; set; } = new ObservableCollection<UserViewModel>();

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

        public ObservableCollection<Message> SelectedGroupDecryptedMessages { get; set; } = new ObservableCollection<Message>{ 
            new Message {
                Type = MessageType.Info,
                Content = "Select a group from list or create on in options panel"
            }
        };

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
