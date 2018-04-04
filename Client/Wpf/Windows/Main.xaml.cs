using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Client.Wpf.Utility;
using Client.Wpf.ViewModels;
using Domain.Crypto;
using Domain.Models;
using Microsoft.Win32;

namespace Client.Wpf.Windows
{
    public partial class MainWindow
    {
        private readonly MainWindowViewModel _dataContext;

        public MainWindow()
        {
            _dataContext = new MainWindowViewModel();
            DataContext = _dataContext;

            ConfigureRequestHandlers();

            InitializeComponent();
            InitializeViewModel();

            MessagesListBox.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
        }

        public async void InitializeViewModel()
        {
            GC.Collect();
            var tokens = await IsolatedStorageManager.ReadSavedAccessTokens();
            _dataContext.User = SessionManager.LoggedUser;
            _dataContext.Contacts = new ObservableCollection<UserViewModel>(
                SessionManager.UserContacts.Select(user => new UserViewModel(user.Email, user.Claims)));
//            _dataContext.Groups = SessionManager.UserGroups ?? new ObservableCollection<Group>();
            _dataContext.Groups = new ObservableCollection<Group>(SessionManager.UserGroups.Select(group =>
            {
                if (group.Type == GroupType.Personal)
                {
                    group.Name = group.UsersPublicKeys.First(key => key.Email != SessionManager.LoggedUser.Email).Email;
                }

                return group;
            }));
            _dataContext.AccessToken = tokens;
        }

        private void ConfigureRequestHandlers()
        {
//            RequestProvider.AppendGlobalConnectionCloseHandler(OnConnectionClosed);
//            RequestProvider.AppendGlobalConnectionEstablishHandler(OnConnected);
            RequestProvider.AppendRoomInviteHandler((group, privateKey) =>
            {
                SessionManager.AddPrivateKey(group.Id, privateKey);

                if (group.Owner != _dataContext.User.Email)
                {
                    group.Messages.Add(new Message
                    {
                        Type = MessageType.Info,
                        Content = $"Invited by {group.Owner}"
                    });
                }

                if (group.Type == GroupType.Personal)
                {
                    group.Name = group.UsersPublicKeys.First(key => key.Email != SessionManager.LoggedUser.Email).Email;
                }

                var result = new List<Group>(_dataContext.Groups) {group};
                _dataContext.Groups = result;
            });
            RequestProvider.AppendUserLeftGroupHandler((groupId, userEmail) =>
            {
                var sourceGroup = _dataContext.Groups.First(group => group.Id == groupId);
                var publicKey = sourceGroup.UsersPublicKeys.First(key => key.Email == userEmail);
                sourceGroup.UsersPublicKeys.Remove(publicKey);

                var infoMessage = new Message
                {
                    Type = MessageType.Info,
                    Content = $"User {userEmail} has left."
                };
                sourceGroup.Messages.Add(infoMessage);
                if (_dataContext.SelectedGroup == sourceGroup)
                {
                    Dispatcher.Invoke(() => { _dataContext.SelectedGroupDecryptedMessages.Add(infoMessage); });
                }
            });
            RequestProvider.AppendMessageReceivedHandler((groupId, message) =>
                {
                    var group = _dataContext.Groups.First(groups => groups.Id == groupId);
                    group.Messages.Add(message);

                    if (_dataContext.SelectedGroup == group)
                    {
                        Dispatcher.Invoke(() => {
                            _dataContext.SelectedGroupDecryptedMessages.Add(new Message
                            {
                                Id = message.Id,
                                Sended = message.Sended,
                                SenderEmail = message.SenderEmail,
                                Content = SessionManager.DecryptMessage(groupId, message.Content)
                            });
                        });
                    }
                }
            );
        }

        private async void LogOutButtonClick(object sender, RoutedEventArgs e)
        {
            await SessionManager.LogOut();
        }

        private void SendMessageButtonClick(object sender, RoutedEventArgs e)
        {
            var message = ChatTextBox.Text;
            if (string.IsNullOrEmpty(message))
            {
                return;
            }
            
            Task.Factory.StartNew(() =>
            {
                var encryptedString = new Pgp().EncryptString(message, 
                    _dataContext.SelectedGroup.UsersPublicKeys.Select(user => user.PublicKey));
                
//                try
//                {
                    RequestProvider.SendMessage(_dataContext.SelectedGroup.Id, encryptedString);

                    Dispatcher.Invoke(() =>
                    {
                        var decryptedMessage = new Message
                        {
                            Sended = DateTime.Now,
                            SenderEmail = _dataContext.User.Email,
                            Content = message
                        };

                        _dataContext.SelectedGroupDecryptedMessages.Add(decryptedMessage);
                        _dataContext.SelectedGroup.Messages.Add(new Message
                        {
                            Sended = decryptedMessage.Sended,
                            SenderEmail = decryptedMessage.SenderEmail,
                            Content = encryptedString
                        });
                    });
//                }
//                catch (ConnectionSetupException)
//                {
//                    MessageBox.Show(this, "Server is unreachable!");
//                }
//                catch (ConnectionShutdownException)
//                {
//                    MessageBox.Show(this, "Server stopped connection!");
//                }
            });

//            MessagesListBox.GetValue(ScrollViewer.Vert, MessagesScrollViewer.ScrollableHeight);
//            MessagesListBox.SetValue(ScrollViewer.VerticalOffsetProperty, MessagesScrollViewer.ScrollableHeight);
            ChatTextBox.Text = string.Empty;
            ChatTextBox.Focus();
        }
        
        private void DisableSendActions()
        {
            Dispatcher.Invoke(() => ChatTextBox.IsEnabled = false);
            Dispatcher.Invoke(() => SendMessageButton.IsEnabled = false);
            Dispatcher.Invoke(() => SendFileButton.IsEnabled = false);
        }

//        private void OnConnectionClosed(Connection connection)
//        {
//            DisableSendActions();
//            Dispatcher.Invoke(() => ConnectionIdLabel.Content = string.Empty);
//            Dispatcher.Invoke(() => ConnectionIndicator.IsChecked = false);
//            Dispatcher.Invoke(() => App.ChangeMainWindow(new Preloader()));
//            App.ChangeMainWindow(new Preloader());
//        }

//        private void UpdateGroupsList()
//        {
//            try
//            {
//                SessionManager.UpdateUserGroups();
//                _dataContext.Groups = SessionManager.UserGroups;
//            }
//            catch (ExpectedReturnTimeoutException)
//            {
//                MessageBox.Show(this, "Timeout!");
//            }
//        }

        private void AddToContactsButtonClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(NewContactEmailTextBox.Text))
            {
                MessageBox.Show(this, "Empty!");
                return;
            }

            try
            {
                SessionManager.AddToContacts(NewContactEmailTextBox.Text);

                //if (operationResponse.IsErrorOccured())
                //{
                //    MessageBox.Show(this, operationResponse.Error);
                //}
                //else
                //{
                NewContactEmailTextBox.Text = string.Empty;
                UpdateContactsListFromSession();
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
            //catch (ExpectedReturnTimeoutException)
            //{
            //    MessageBox.Show(this, "Try again");
            //}
        }

        private void CreateRoomButtonClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(NewRoomNameTextBox.Text))
            {
                MessageBox.Show(this, "Enter group name!");
                return;
            }

            if (ContactsSelectorListBox.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "No users selected!");
                return;
            }

            var receipents = new List<string>(ContactsSelectorListBox.SelectedItems.Cast<UserViewModel>().Select(user => user.Email))
            {
                _dataContext.User.Email
            };

//            try
//            {
                RequestProvider.CreateGroup(_dataContext.User.Email, 
                    NewRoomNameTextBox.Text,
                    (GroupType) Enum.Parse(typeof(GroupType), GroupTypeComboBox.SelectionBoxItem.ToString()),
                    receipents);

                HideContactsSelectorGridClicked(null, null);
//            }
//            catch (ExpectedReturnTimeoutException)
//            {
//                MessageBox.Show(this, "Try again");
//            }
        }

        private void GroupsListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GroupsListBox.SelectedItem == null)
            {
                return;
            }

            _dataContext.SelectedGroupDecryptedMessages.Clear();
            _dataContext.SelectedGroup = (Group) GroupsListBox.SelectedItem;
            foreach (var message in _dataContext.SelectedGroup.Messages)
            {
                if (message.Type != MessageType.Plain)
                {
                    _dataContext.SelectedGroupDecryptedMessages.Add(message);
                    continue;
                }

                try
                {
                    _dataContext.SelectedGroupDecryptedMessages.Add(new Message
                    {
                        Id = message.Id,
                        Sended = message.Sended,
                        SenderEmail = message.SenderEmail,
                        Content = SessionManager.DecryptMessage(_dataContext.SelectedGroup.Id, message.Content)
                    });
                }
                catch (ArgumentException)
                {

                }
            }

            if (_dataContext.SelectedGroup.UsersPublicKeys.Any(key => key.Email == _dataContext.User.Email))
            {
                EnableSendActions();
            }
            else
            {
                _dataContext.SelectedGroupDecryptedMessages.Add(new Message
                {
                    Type = MessageType.Info,
                    Content = "You've left this group!"
                });
                DisableSendActions();
            }

        }

        private void EnableSendActions()
        {
            ChatTextBox.IsEnabled = true;
            SendMessageButton.IsEnabled = true;
            SendFileButton.IsEnabled = true;
        }

        private void UpdateContactsListFromSession()
        {
            _dataContext.Contacts = new ObservableCollection<UserViewModel>(SessionManager.UserContacts.Select(user => new UserViewModel(user.Email, user.Claims)));
        }

        private void GroupLeaveMenuItemClick(object sender, RoutedEventArgs e)
        {
            if (GroupsListBox.SelectedItem == null)
                return;

            var selectedChatRoom = (Group) GroupsListBox.SelectedItem;
            
            var groupUserPublicKey = selectedChatRoom.UsersPublicKeys.FirstOrDefault(key => key.Email == _dataContext.User.Email);
            if (groupUserPublicKey == null) // already left this group
                return;

            SessionManager.LeaveGroup(selectedChatRoom.Id);

            selectedChatRoom.UsersPublicKeys.Remove(groupUserPublicKey);

            var leaveInfoMessage = new Message
            {
                Type = MessageType.Info,
                Content = "You have left this group."
            };
            
            _dataContext.Groups.First(group => group == selectedChatRoom).Messages.Add(leaveInfoMessage);

            if (_dataContext.SelectedGroup == selectedChatRoom)
            {
                DisableSendActions();

                _dataContext.SelectedGroupDecryptedMessages.Add(leaveInfoMessage);
            }
        }

        private void ShowUsersMenuItemClick(object sender, RoutedEventArgs e)
        {
            if (!(GroupsListBox.SelectedItem is Group selectedChatRoom))
            {
                return;
            }

            var aggregate = selectedChatRoom.UsersPublicKeys.Select(user => user.Email).Aggregate((ea, kj) => $"{ea}\n{kj}");
            MessageBox.Show(this, aggregate);
        }

        private void SendFileButtonClick(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog
            {
                Multiselect = false
            };
            var result = fileDialog.ShowDialog(this);
            if (result.Value)
            {
                using (var fileStream = fileDialog.OpenFile())
                {
                    using (var streamReader = new MemoryStream())
                    {
                        fileStream.CopyTo(streamReader);
                        
                        RequestProvider.SendFile(fileDialog.SafeFileName, 
                            streamReader.ToArray(), 
                            _dataContext.SelectedGroup.Id);
                    }
                }
            }
        }

        private async void DownloadFileImageClick(object sender, MouseButtonEventArgs e)
        {
            var frameworkElement = (FrameworkElement) sender;
            var frameworkElementParent = (Grid) frameworkElement.Parent;
            var uiElement = (FrameworkElement) frameworkElementParent.Children[1];
            var selectedMessage = (Message) uiElement.DataContext;

//            try
//            {
                var operationResponse = await RequestProvider.DownloadFile(_dataContext.SelectedGroup.Id, selectedMessage.Id);

                if (operationResponse.IsErrorOccured())
                {
                    MessageBox.Show(this, operationResponse.Error);
                }
                else
                {
                    using (var fileStream = File.Open(
                        Path.Combine(Environment.
                            GetFolderPath(Environment.SpecialFolder.DesktopDirectory), Path.GetFileName(selectedMessage.Content)), FileMode.OpenOrCreate))
                    {
                        await fileStream.WriteAsync(operationResponse.Response, 0, operationResponse.Response.Length);
                        Process.Start("explorer.exe", $"/select, \"{Path.GetFileName(selectedMessage.Content)}\"");
                    }
                }
//            }
//            catch (ExpectedReturnTimeoutException ex)
//            {
//                MessageBox.Show(this, $"Try again later ({ex.Message})");
//            }
        }

        private void ShowNewGroupContactsSelectorButtonClick(object sender, RoutedEventArgs e)
        {
            NewGroupContactsSelector.Visibility = Visibility.Visible;
        }

        private void HideContactsSelectorGridClicked(object sender, MouseButtonEventArgs e)
        {
            NewGroupContactsSelector.Visibility = Visibility.Hidden;
            ContactsListBox.UnselectAll();
        }

        private async void MessageShowSenderInfoClick(object sender, RoutedEventArgs e)
        {
            var sendedMessage = (Message)((FrameworkElement)sender).DataContext;
//            var chatUserViewModel = SessionManager.UsersDataCache.FirstOrDefault(user => user.Email == sendedMessage.SenderEmail);
            
//            if (chatUserViewModel == null)
//            {
//                SessionManager.
//            }

            var userData = await SessionManager.GetUsersData(sendedMessage.SenderEmail);
            _dataContext.SelectedContactUser = new UserViewModel(userData.Email, userData.Claims);
//            _dataContext.SelectedContactUser = Utility.SessionManager.GetUsersData(sendedMessage.SenderEmail);
            ProfileInfo.Visibility = Visibility.Visible;
//            RequestProvider.UsersData
//            SelectedContactUser
        }
        
        private void HideProfileInfoGridClicked(object sender, MouseButtonEventArgs e)
        {
            _dataContext.SelectedContactUser = null;
            ProfileInfo.Visibility = Visibility.Hidden;
            ProfileEditor.Visibility = Visibility.Hidden;
            ContactsList.Visibility = Visibility.Hidden;
        }

        private void ShowProfileEditorButtonClick(object sender, RoutedEventArgs e)
        {
//            var chatUserViewModel = new ChatUserViewModel
//            {
//                Id = _dataContext.User.Id,
//                Email = _dataContext.User.Email,
//                Claims = _dataContext.User.Claims
//            };
//            _dataContext.SelectedContactUser = chatUserViewModel;
//            _dataContext.SelectedContactUser = chatUserViewModel;
//            ProfileInfo.Visibility = Visibility.Visible;
            ProfileEditor.Visibility = Visibility.Visible;
        }

        private void MessagesListBox_OnDrop(object sender, DragEventArgs e)
        {

        }

        private void ContactsButton_OnClick(object sender, RoutedEventArgs e)
        {
            ContactsList.Visibility = Visibility.Visible;
        }

        private void RemoveSelectedContactMenuItemClick(object sender, RoutedEventArgs e)
        {
            if (ContactsListBox.SelectedItem == null)
                return;

            var selectedUser = (UserViewModel) ContactsListBox.SelectedItem;

            SessionManager.RemoveFromContacts(selectedUser.Email);
            _dataContext.Contacts.Remove(selectedUser);
            
            
        }
    }
}
