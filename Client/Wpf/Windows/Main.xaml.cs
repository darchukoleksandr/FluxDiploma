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
using Client.Base;
using Domain.Crypto;
using Domain.Models;
using Microsoft.Win32;
using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;

namespace Client.Wpf.Windows
{
    public partial class MainWindow
    {
        private readonly ViewModels.MainWindowViewModel _dataContext;

        public MainWindow()
        {
            var preloaderWindow = new Preloader();
            if (!preloaderWindow.ShowDialog().Value)
            {
                Close();
            }
            
            _dataContext = new ViewModels.MainWindowViewModel();
            DataContext = _dataContext;
            Closing += OnWindowClosing;

            ConfigureRequestHandlers();

            InitializeComponent();
            InitializeViewModel();

            MessagesListBox.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
//            new ContactsSelector().ShowDialog();
        }

        public async void InitializeViewModel()
        {
            GC.Collect();
            var tokens = await Utility.IsolatedStorageManager.ReadSavedOauthTokens();
            _dataContext.User = Utility.SessionManager.LoggedUser;
            _dataContext.Contacts = Utility.SessionManager.UserContacts ?? new List<ChatUserViewModel>();
            _dataContext.Groups = Utility.SessionManager.UserGroups ?? new List<Group>();
            _dataContext.AccessToken = tokens.Item1;
            _dataContext.IdentityToken = tokens.Item2;
        }

        private void ConfigureRequestHandlers()
        {
            RequestProvider.AppendGlobalConnectionCloseHandler(OnConnectionClosed);
            RequestProvider.AppendGlobalConnectionEstablishHandler(OnConnected);
            RequestProvider.AppendRoomInvitePacketHandler((header, connection, request) =>
            {
                Utility.SessionManager.AddPrivateKey(request.Item1.Id, request.Item2);

                if (request.Item1.Owner != _dataContext.User.Email)
                {
                    request.Item1.Messages.Add(new Message
                    {
                        Type = MessageType.Info,
                        Content = $"Invited by {request.Item1.Owner}"
                    });
                }

                var result = new List<Group>(_dataContext.Groups) {request.Item1};
                _dataContext.Groups = result;
            });
            RequestProvider.AppendLeaveGroupPacketHandler((header, connection, request) =>
            {
                var sourceGroup = _dataContext.Groups.First(group => group.Id == request.Item1);
                var publicKey = sourceGroup.UsersPublicKeys.First(key => key.Email == request.Item2);
                sourceGroup.UsersPublicKeys.Remove(publicKey);

                var infoMessage = new Message
                {
                    Type = MessageType.Info,
                    Content = $"User {request.Item2} has left."
                };
                sourceGroup.Messages.Add(infoMessage);
                if (_dataContext.SelectedGroup == sourceGroup)
                {
                    Dispatcher.Invoke(() => { _dataContext.SelectedGroupDecryptedMessages.Add(infoMessage); });
                }
            });
            NetworkComms.AppendGlobalIncomingPacketHandler<ValueTuple<Guid, Message>>("MessageReceived",
                (header, connection, request) =>
                {
                    var group = _dataContext.Groups.First(groups => groups.Id == request.Item1);
                    group.Messages.Add(request.Item2);

                    if (_dataContext.SelectedGroup == group)
                    {
                        Dispatcher.Invoke(() => {
                            _dataContext.SelectedGroupDecryptedMessages.Add(new Message
                            {
                                Id = request.Item2.Id,
                                Sended = request.Item2.Sended,
                                SenderEmail = request.Item2.SenderEmail,
                                Content = Utility.SessionManager.DecryptMessage(request.Item1, request.Item2.Content)
                            });
                        });
                    }
                }
            );
        }

        private async void LogOutButtonClick(object sender, RoutedEventArgs e)
        {
            await Utility.SessionManager.LogOut();

            _dataContext.User = null;
            _dataContext.Contacts = null;
            _dataContext.Groups = null;
            _dataContext.SelectedGroup = null;
            _dataContext.AccessToken = string.Empty;
            _dataContext.IdentityToken = string.Empty;
            _dataContext.SelectedGroupDecryptedMessages.Clear();
            _dataContext.SelectedContactUser = null;
            
            App.Current.MainWindow = new Wpf.Windows.Preloader();
            this.Hide();
            App.Current.MainWindow.Show();
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
                
                try
                {
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
                }
                catch (ConnectionSetupException)
                {
                    MessageBox.Show(this, "Server is unreachable!");
                }
                catch (ConnectionShutdownException)
                {
                    MessageBox.Show(this, "Server stopped connection!");
                }
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

        private void OnConnectionClosed(Connection connection)
        {
            DisableSendActions();
            Dispatcher.Invoke(() => ConnectionIdLabel.Content = string.Empty);
            Dispatcher.Invoke(() => ConnectionIndicator.IsChecked = false);
        }

        private void OnConnected(Connection connection)
        {
            Dispatcher.InvokeAsync(() => ConnectionIndicator.IsChecked = true);
            Dispatcher.InvokeAsync(() => ConnectionIdLabel.Content = connection.ConnectionInfo.NetworkIdentifier.Value);
        }

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
            if (_dataContext.User == null)
            {
                MessageBox.Show(this, "Connect first!");
                return;
            }

            //try
            //{
                Utility.SessionManager.AddToContacts(NewContactEmailTextBox.Text);

                //if (operationResponse.IsErrorOccured())
                //{
                //    MessageBox.Show(this, operationResponse.Error);
                //}
                //else
                //{
                    NewContactEmailTextBox.Text = string.Empty;
                    UpdateContactsList();
                //}
            //}
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

            if (ContactsListBox.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "No users selected!");
                return;
            }

            var receipents = new List<string>(ContactsListBox.SelectedItems.Cast<ChatUserViewModel>().Select(user => user.Email))
            {
                _dataContext.User.Email
            };

            var requestData = new ValueTuple<string, string, bool, IEnumerable<string>>(
                _dataContext.User.Email, 
                NewRoomNameTextBox.Text,
                GroupTypeToggleButton.IsChecked ?? default(bool),
                receipents);

            try
            {
                RequestProvider.CreateRoom(requestData);

                HideContactsSelectorGridClicked(null, null);
            }
            catch (ExpectedReturnTimeoutException)
            {
                MessageBox.Show(this, "Try again");
            }
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
                
                _dataContext.SelectedGroupDecryptedMessages.Add(new Message
                {
                    Id = message.Id,
                    Sended = message.Sended,
                    SenderEmail = message.SenderEmail,
                    Content = Utility.SessionManager.DecryptMessage(_dataContext.SelectedGroup.Id, message.Content)
                });
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

        private void UpdateContactsList()
        {
            //try
            //{
            //SessionManager.UpdateUserContacts();
            _dataContext.Contacts = Utility.SessionManager.UserContacts;
//                var operationResponse = RequestProvider.UpdateContactsList(_dataContext.User.Contacts.ToArray());
//
//                if (!operationResponse.IsErrorOccured())
//                {
//                    _dataContext.Contacts = operationResponse.Response;
//                }
            //}
            //catch (ExpectedReturnTimeoutException)
            //{

            //}
        }

        private void DeleteFromContactsMenuItemClick(object sender, RoutedEventArgs e)
        {
            var contactsSelectedItem = ContactsListBox.SelectedItem;
            if (contactsSelectedItem == null)
            {
                return;
            }
            var selectedUserEmail = ((ChatUserViewModel)ContactsListBox.SelectedItem).Email;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    var operationResponse = RequestProvider.RemoveFromContacts(_dataContext.User.Email, selectedUserEmail);

                    if (!operationResponse.IsErrorOccured())
                    {
                        _dataContext.User = operationResponse.Response;
                        UpdateContactsList();
                    }
                }
                catch (ConnectionSetupException)
                {
                    MessageBox.Show(this, "Server is unreachable!");
                }
            });
        }

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            RequestProvider.CloseAllConnections();
        }

        private void GroupLeaveMenuItemClick(object sender, RoutedEventArgs e)
        {
            if (GroupsListBox.SelectedItem == null)
                return;

            var selectedChatRoom = (Group) GroupsListBox.SelectedItem;
            
            var groupUserPublicKey = selectedChatRoom.UsersPublicKeys.FirstOrDefault(key => key.Email == _dataContext.User.Email);
            if (groupUserPublicKey == null) // already left this group
                return;

            Utility.SessionManager.LeaveGroup(selectedChatRoom.Id);

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
                ConnectionIdLabel.Content = fileDialog.FileName;

                using (var fileStream = fileDialog.OpenFile())
                {
                    using (var streamReader = new MemoryStream())
                    {
                        fileStream.CopyTo(streamReader);
                        
                        var requestData = new ValueTuple<string, string, byte[], Guid>(
                            _dataContext.User.Email, 
                            fileDialog.SafeFileName, 
                            streamReader.ToArray(), 
                            _dataContext.SelectedGroup.Id);

                        RequestProvider.SendFile(requestData);
                    }
                }
            }
        }

        private async void DownloadFileImageClick(object sender, MouseButtonEventArgs e)
        {
            var selectedMessage = (Message) MessagesListBox.SelectedItem;

            try
            {
                var operationResponse = RequestProvider.DownloadFile(_dataContext.SelectedGroup.Id, selectedMessage.Id);

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
            }
            catch (ExpectedReturnTimeoutException ex)
            {
                MessageBox.Show(this, $"Try again later ({ex.Message})");
            }
        }

        private void ContactsButtonClick(object sender, RoutedEventArgs e)
        {
            ContactsSelector.Visibility = Visibility.Visible;
        }

        private void HideContactsSelectorGridClicked(object sender, MouseButtonEventArgs e)
        {
            ContactsSelector.Visibility = Visibility.Hidden;
            ContactsListBox.UnselectAll();
        }

        private void MessageShowSenderInfoClick(object sender, RoutedEventArgs e)
        {
            var sendedMessage = (Message)((FrameworkElement)sender).DataContext;
//            var chatUserViewModel = SessionManager.UsersDataCache.FirstOrDefault(user => user.Email == sendedMessage.SenderEmail);
            
//            if (chatUserViewModel == null)
//            {
//                SessionManager.
//            }

            _dataContext.SelectedContactUser = Utility.SessionManager.GetUsersData(sendedMessage.SenderEmail);
            ProfileInfo.Visibility = Visibility.Visible;
//            RequestProvider.UsersData
//            SelectedContactUser
        }

        private void ShowCreateRoomViewButtonClick(object sender, RoutedEventArgs e)
        {
            
        }

        private void HideProfileInfoGridClicked(object sender, MouseButtonEventArgs e)
        {
            _dataContext.SelectedContactUser = null;
            ProfileInfo.Visibility = Visibility.Hidden;
        }

        private void ShowProfileButtonClick(object sender, RoutedEventArgs e)
        {
            var chatUserViewModel = new ChatUserViewModel
            {
                Id = _dataContext.User.Id,
                Email = _dataContext.User.Email,
                Claims = _dataContext.User.Claims
            };
            _dataContext.SelectedContactUser = chatUserViewModel;
            ProfileInfo.Visibility = Visibility.Visible;
        }
    }
}
