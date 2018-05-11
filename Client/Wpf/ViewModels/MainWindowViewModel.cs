using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Domain.Models;

namespace Client.Wpf.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<SearchResult> SearchResults { get; set; } = new ObservableCollection<SearchResult>();
            
        public ObservableCollection<UserViewModel> Contacts { get; set; } = new ObservableCollection<UserViewModel>();

        public ObservableCollection<Group> Groups { get; set; } = new ObservableCollection<Group>();

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
                Content = "Select a group from list or create one in options panel"
            }
        };

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
