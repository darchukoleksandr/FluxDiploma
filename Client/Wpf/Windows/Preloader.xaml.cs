using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Auth0.OidcClient;
using Client.Base;
using NetworkCommsDotNet;

namespace Client.Wpf.Windows
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Preloader
    {
        public Preloader()
        {
            InitializeComponent();

            StartSession();
        }

        private async void StartSession()
        {
//            await IsolatedStorageManager.DeleteOauthTokens();

            while (true)
            {
                try
                {
                    var tokensTuple = await Utility.IsolatedStorageManager.ReadSavedOauthTokens();

                    if (string.IsNullOrEmpty(tokensTuple.Item1) || string.IsNullOrEmpty(tokensTuple.Item2))
                    {
                        await ShowAuth0Login();
                    }

                    await ConnectToServer();

                    break;
                }
                catch (Exception)
                {
                    // ignored 
                }
            }

            if (Application.Current.MainWindow == this)
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is MainWindow mainWindow)
                    {
                        Application.Current.MainWindow = mainWindow;
                        Application.Current.MainWindow.Show();
                        Application.Current.MainWindow.Show();
                        mainWindow.InitializeViewModel();
                    }
                }
            }
            else
            {
                DialogResult = true;
            }

            Close();
        }

        private async Task ShowAuth0Login()
        {
            try
            {
                var client = new Auth0Client(new Auth0ClientOptions
                {
                    Domain = "darchukoleksandr.eu.auth0.com",
                    ClientId = "dhoIjZT4QScLhj2soIMnelYLc0A8NaTT",
                    Browser = new Utility.SystemWebBrowser(),
                    RedirectUri = "http://127.0.0.1:7895/"
                });
                
                InfoTextBlock.Text = "Waiting user to log in";

                var result = await client.LoginAsync(new Dictionary<string, string>
                {
                    {"audience", "https://darchukoleksandr.eu.auth0.com/api/v2/"},
                    {"scope", "openid profile email"}
                });

                if (result.IsError)
                {
                    MessageBox.Show(this, result.Error);
                    return;
                }

                await Utility.IsolatedStorageManager.SaveOauthTokens(result.AccessToken, result.IdentityToken);
            }
            catch (InvalidOperationException)
            {
                InfoTextBlock.Text = "No internet connection";
                await Task.Delay(3000);
                throw;
            }
            //catch (HttpListenerException)
            //{

            //}
        }

        private async Task ConnectToServer()
        {
            var tokensTuple = await Utility.IsolatedStorageManager.ReadSavedOauthTokens();
            InfoTextBlock.Text = "Connecting to server";

            await Task.Run(async () =>
            {
                try
                {
                    //TODO dislocate to session
                    var connectionResponse =
                        RequestProvider.Connect(tokensTuple.Item1, tokensTuple.Item2);

                    if (connectionResponse.IsErrorOccured())
                    {
                        if (connectionResponse.Error == "NullRefenceException" ||
                            connectionResponse.Error == "SecurityTokenExpiredException")
                        {
                            await Utility.IsolatedStorageManager.DeleteOauthTokens();
                            throw new ConnectionShutdownException();
                        }

                        if (connectionResponse.Error == "InvalidOperationException")
                            throw new ConnectionSetupException();
                        throw new Exception();
                    }

                    Utility.SessionManager.LoggedUser = connectionResponse.Response;

                    if (connectionResponse.Response.PrivateKeys.Any())
                    {
                        Utility.SessionManager.UpdateUserGroups();
                    }

                    if (connectionResponse.Response.Contacts.Any())
                    {
                        Utility.SessionManager.UpdateUserContacts();
                    }
                }
                catch (ConnectionSetupException)
                {
                    Dispatcher.Invoke(() => InfoTextBlock.Text = "Server is not responding");
                    await Task.Delay(3000);
                    throw;
                }
            });

        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);


//            Application.Current.Shutdown();
        }
    }
}
