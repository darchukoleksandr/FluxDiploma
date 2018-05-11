using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Auth0.OidcClient;
using Client.Wpf.Utility;
using Microsoft.AspNet.SignalR.Client;

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

//        public Preloader(string message)
//        {
//            InitializeComponent();
//            StartSession();
//        }

        private async void StartSession()
        {
            while (true)
            {
                try
                {
                    var accessToken = await IsolatedStorageManager.ReadSavedAccessTokens();

                    if (string.IsNullOrEmpty(accessToken))
                    {
                        await ShowAuth0Login();
                        continue;
                    }

                    await ConnectToServer(accessToken);

                    break;
                }
                catch (Exception e)
                {
//                     ignored 
                }
            }

            App.ChangeMainWindow(new MainWindow());
        }

        private async Task ShowAuth0Login()
        {
            try
            {
                var client = new Auth0Client(new Auth0ClientOptions
                {
                    Domain = "darchukoleksandr.eu.auth0.com",
                    ClientId = "dhoIjZT4QScLhj2soIMnelYLc0A8NaTT",
                    Browser = new SystemWebBrowser(),
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

                await IsolatedStorageManager.SaveOauthTokens(result.AccessToken);
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

        private async Task ConnectToServer(string accesToken)
        {
            InfoTextBlock.Text = "Connecting to server";

            try
            {
                await SessionManager.Connect(accesToken);
            }
            catch (HttpRequestException) // No connection with host
            {
                Dispatcher.Invoke(() => InfoTextBlock.Text = "Server is not responding");
                await Task.Delay(3000);
                throw;
            }
            catch (HttpClientException e)
            {
                if (e.Message.StartsWith("StatusCode: 500"))
                {
                    Dispatcher.Invoke(() => InfoTextBlock.Text = "Can not connect to server!");
                    await Task.Delay(3000);
                }

                if (e.Message.StartsWith("StatusCode: 401")) // Token expired
                {
                    await IsolatedStorageManager.DeleteOauthTokens();
                }

                throw;
            }
            catch (Exception e)
            {
                if (e.Message.StartsWith("Unauthorized")) // Token expired
                {
                    await IsolatedStorageManager.DeleteOauthTokens();
                }

                throw;
            }
        }
    }
}
