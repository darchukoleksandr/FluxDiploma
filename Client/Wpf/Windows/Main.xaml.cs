using System;
using System.Collections.Generic;
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

        #region Initialization
        
        public MainWindow()
        {
            _dataContext = new MainWindowViewModel();
            DataContext = _dataContext;

            ConfigureRequestHandlers();

            InitializeComponent();
            InitializeViewModel();

            MessagesListBox.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
        }

        public void InitializeViewModel()
        {
            _dataContext.User = SessionManager.LoggedUser;
            _dataContext.Contacts.AddRange(SessionManager.UserContacts.Select(user => new UserViewModel(user.Email, user.Claims)));
            _dataContext.Groups.AddRange(SessionManager.UserGroups);
//            _dataContext.Groups.AddRange(SessionManager.UserGroups.Select(group =>
//            {
//                if (_dataContext.User.PrivateKeys.Any(key => key.GroupId == group.Id))
//                    return group;
//                return null;
//            }));
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            RequestProvider.CloseAllConnections();
            base.OnClosing(e);
        }

        private void ConfigureRequestHandlers()
        {
            RequestProvider.AppendGlobalConnectionCloseHandler(OnConnectionClosed);
            //RequestProvider.AppendJoinChannelHandler(group =>
            //{
            //    SessionManager.AddPrivateKey(group.Id, null);

            //    _dataContext.Groups.Add(group);
            //});
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

                Dispatcher.Invoke(() =>
                {
                    if (group.Name == NewGroupNameTextBox.Text)
                    {
                        HideNewGroupFormClicked(null, null);
                    }
                });

//                var result = new List<Group>(_dataContext.Groups) {group};
//                _dataContext.Groups = result;
                Dispatcher.Invoke(() => _dataContext.Groups.Add(group));
            });
            RequestProvider.AppendUserJoinedGroupHandler((groupId, publicKey) =>
            {
                var groupJoined = _dataContext.Groups.First(group => group.Id == groupId);
                groupJoined.UsersPublicKeys.Add(publicKey);
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
                            if (message.Type == MessageType.File)
                            {
                                _dataContext.SelectedGroupDecryptedMessages.Add(message);
                            }
                            else
                            {
                                _dataContext.SelectedGroupDecryptedMessages.Add(new Message
                                {
                                    Id = message.Id,
                                    Sended = message.Sended,
                                    SenderEmail = message.SenderEmail,
                                    Content = SessionManager.DecryptMessage(groupId, message.Content)
                                });
                            }
                        });
                    }
                }
            );
        }
        
        private void OnConnectionClosed()
        {
            SessionManager.LogOut();
        }

        #endregion

        private void LogOutButtonClick(object sender, RoutedEventArgs e)
        {
            SessionManager.LogOut();
        }

        private void DisableSendActions()
        {
            Dispatcher.Invoke(() => ChatTextBox.IsEnabled = false);
            Dispatcher.Invoke(() => SendMessageButton.IsEnabled = false);
            Dispatcher.Invoke(() => SendFileButton.IsEnabled = false);
        }

        private async void SearchButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchTextBox.Text))
            {
                return;
            }

             var result = await SessionManager.Search(SearchTextBox.Text);

//             _dataContext.SearchResults = new ObservableCollection<SearchResult>(result);
             _dataContext.SearchResults.AddRange(result);
        }

        private void CreateRoomButtonClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(NewGroupNameTextBox.Text))
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

            var groupType = (GroupType) Enum.Parse(typeof(GroupType), GroupTypeComboBox.SelectionBoxItem.ToString());

            RequestProvider.CreateGroup(_dataContext.User.Email, 
                NewGroupNameTextBox.Text, groupType, receipents);

            HideContactsListClicked(sender, null);
        }

        private void GroupsListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GroupsListBox.SelectedItem == null)
            {
                return;
            }
            
            _dataContext.SelectedGroupDecryptedMessages.Clear();
            _dataContext.SelectedGroup = (Group) GroupsListBox.SelectedItem;

            if (_dataContext.SelectedGroup.Type == GroupType.Channel)
            {
                if (_dataContext.SelectedGroup.Owner != SessionManager.LoggedUser.Email)
                {
                    DisableSendActions();
                }
                return;
            }

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

        private void ShowNewGroupContactsSelectorButtonClick(object sender, RoutedEventArgs e)
        {
            NewGroupForm.Visibility = Visibility.Visible;
        }

        private async void MessageShowSenderInfoClick(object sender, RoutedEventArgs e)
        {
            var sendedMessage = (Message)((FrameworkElement)sender).DataContext;
            
            await ShowUserProfileInfo(sendedMessage.SenderEmail);
        }

        private async Task ShowUserProfileInfo(string userEmail)
        {
            var userData = await SessionManager.GetUsersData(userEmail);
            _dataContext.SelectedContactUser = new UserViewModel(userData.Email, userData.Claims);

            ProfileInfo.DataContext = _dataContext.SelectedContactUser;
//            if (SessionManager.UserContacts.Any(contact => contact.Email == userData.Email))
//            {
//                AddToContacts.Visibility = Visibility.Hidden;
//                RemoveFromContacts.Visibility = Visibility.Visible;
//            }
//            else
//            {
//                RemoveFromContacts.Visibility = Visibility.Hidden;
//                AddToContacts.Visibility = Visibility.Visible;
//            }

            ProfileInfo.Visibility = Visibility.Visible;
        }

        //private void ShowProfileEditorButtonClick(object sender, RoutedEventArgs e)
        //{
        //    _dataContext.SelectedContactUser = new UserViewModel(_dataContext.User.Email, _dataContext.User.Claims);
        //    ProfileEditor.Visibility = Visibility.Visible;
        //}

        private void ContactsButton_OnClick(object sender, RoutedEventArgs e)
        {
            ContactsList.Visibility = Visibility.Visible;
        }

        private void SearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchTextBox.Text))
            {
                _dataContext.SearchResults.Clear();
            }
        }

        private async void SearchResults_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchResults.SelectedItem == null)
            {
                return;
            }

            var selectedItem = (SearchResult) SearchResults.SelectedItem;

            if (selectedItem.Type == SearchResultType.User)
            {
                await ShowUserProfileInfo(selectedItem.Name);
                SearchResults.UnselectAll();
            }

//            _dataContext.SelectedGroupDecryptedMessages.Clear();
            //_dataContext.SelectedGroup = (Group) SearchResults.SelectedItem;
            //foreach (var message in _dataContext.SelectedGroup.Messages)
            //{
            //    if (message.Type != MessageType.Plain)
            //    {
            //        _dataContext.SelectedGroupDecryptedMessages.Add(message);
            //        continue;
            //    }

            //    try
            //    {
            //        _dataContext.SelectedGroupDecryptedMessages.Add(new Message
            //        {
            //            Id = message.Id,
            //            Sended = message.Sended,
            //            SenderEmail = message.SenderEmail,
            //            Content = SessionManager.DecryptMessage(_dataContext.SelectedGroup.Id, message.Content)
            //        });
            //    }
            //    catch (ArgumentException)
            //    {

            //    }
            //}


        }

        #region MessagingEventHandlers
        
        private void SendFileButtonClick(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog
            {
                Multiselect = false
            };
            var result = fileDialog.ShowDialog(this);
            if (!result.Value) 
                return;

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

        private async void DownloadFileImageClick(object sender, MouseButtonEventArgs e)
        {
            var frameworkElement = (FrameworkElement) sender;
            var frameworkElementParent = (Grid) frameworkElement.Parent;
            var uiElement = (FrameworkElement) frameworkElementParent.Children[1];
            var selectedMessage = (Message) uiElement.DataContext;

//            try
//            {
                var operationResponse = await RequestProvider.DownloadFile(selectedMessage.Content);

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
        
        private void MessagesListBox_OnDrop(object sender, DragEventArgs e)
        {

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
                
                RequestProvider.SendMessage(_dataContext.SelectedGroup.Id, encryptedString);

                Dispatcher.Invoke(() =>
                {
                    _dataContext.SelectedGroupDecryptedMessages.Add(new Message
                    {
                        Sended = DateTime.Now,
                        SenderEmail = _dataContext.User.Email,
                        Content = message
                    });

                    _dataContext.SelectedGroup.Messages.Add(new Message
                    {
                        Sended = DateTime.Now,
                        SenderEmail = _dataContext.User.Email,
                        Content = encryptedString
                    });
                });
            });

//            MessagesListBox.GetValue(ScrollViewer.Vert, MessagesScrollViewer.ScrollableHeight);
//            MessagesListBox.SetValue(ScrollViewer.VerticalOffsetProperty, MessagesScrollViewer.ScrollableHeight);
            ChatTextBox.Text = string.Empty;
            ChatTextBox.Focus();
        }
        

        #endregion

        #region ConctactsActionsEventHandlers
        
        private async void AddToContacts(string email)
        {
            try
            {
                var contactUser = await SessionManager.AddToContacts(email);
                _dataContext.Contacts.Add(new UserViewModel(contactUser.Email, contactUser.Claims));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        private void RemoveFromContacts(string email)
        {
            try
            {
                SessionManager.RemoveFromContacts(email);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        private void ChangeContactStatus_OnClick(object sender, RoutedEventArgs e)
        {
            var user = (UserViewModel) ProfileInfo.DataContext;
            if (user.Is)
            {
                RemoveFromContacts(user.Email);
                _dataContext.Contacts.Remove(_dataContext.Contacts.First(contact => contact.Email == user.Email));
            }
            else
            {
                AddToContacts(user.Email);
            }
        }

        private void ContactsListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var user = (UserViewModel)ContactsListBox.SelectedItem;
            
            ContactsListBox.UnselectAll();
            ContactsList.Visibility = Visibility.Hidden;

            ProfileInfo.DataContext = user;
            ProfileInfo.Visibility = Visibility.Visible;
        }

        #endregion
        
        #region HidePopupFormsEventHandlers

        private void HideNewGroupFormClicked(object sender, MouseButtonEventArgs e)
        {
            NewGroupForm.Visibility = Visibility.Hidden;
        }

        private void HideContactsListClicked(object sender, MouseButtonEventArgs e)
        {
            ContactsList.Visibility = Visibility.Hidden;
        }

        private void HideProfileInfoClicked(object sender, MouseButtonEventArgs e)
        {
            _dataContext.SelectedContactUser = null;
            ProfileInfo.Visibility = Visibility.Hidden;
        }

        //private void HideProfileEditorClicked(object sender, MouseButtonEventArgs e)
        //{
        //    _dataContext.SelectedContactUser = null;
        //    ProfileEditor.Visibility = Visibility.Hidden;
        //}

        #endregion

        private async void ShowPersonalInfoClick(object sender, RoutedEventArgs e)
        {
            await ShowUserProfileInfo(_dataContext.User.Email);
        }

        private void MessagesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
