using System.IO;
using System.IO.IsolatedStorage;
using System.Threading.Tasks;

namespace Client.Wpf.Utility
{
    class IsolatedStorageManager
    {
        private const string TokensFileName = "Tokens";

        static IsolatedStorageManager()
        {
            using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                if (!isolatedStorage.FileExists(TokensFileName))
                {
                    isolatedStorage.CreateFile(TokensFileName).Close();
                }
            }
        }

        private static async Task ReadFile()
        {
            using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                using (var isolatedStorageFileStream = isolatedStorage.OpenFile(TokensFileName, FileMode.Open, FileAccess.Read))
                {
                    using (var streamReader = new StreamReader(isolatedStorageFileStream))
                    {
                        var text = await streamReader.ReadToEndAsync();
                    }
                }
            }
        }

        public static async Task<(string, string)> ReadSavedOauthTokens()
        {
            using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                using (var isolatedStorageFileStream = isolatedStorage.OpenFile(TokensFileName, FileMode.Open, FileAccess.Read))
                {
                    using (var streamReader = new StreamReader(isolatedStorageFileStream))
                    {
                        var accessToken = await streamReader.ReadLineAsync();
                        var identityToken = await streamReader.ReadLineAsync();
                        return (accessToken, identityToken);
                    }
                }
            }
        }

        public static async Task SaveOauthTokens(string accessToken, string identityToken)
        {
            await ReadFile();
            using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                using (var isolatedStorageFileStream = isolatedStorage.OpenFile(TokensFileName, FileMode.Open, FileAccess.Write))
                {
                    using (var streamReader = new StreamWriter(isolatedStorageFileStream))
                    {
                        await streamReader.WriteLineAsync(accessToken);
                        await streamReader.WriteLineAsync(identityToken);
                    }
                }
            }
        }

        public static async Task DeleteOauthTokens()
        {
            await ReadFile();
            using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                using (var isolatedStorageFileStream = isolatedStorage.OpenFile(TokensFileName, FileMode.Open, FileAccess.Write))
                {
                    using (var streamReader = new StreamWriter(isolatedStorageFileStream))
                    {
                        await streamReader.WriteLineAsync(string.Empty);
                    }
                }
            }
        }

        //    public static async Task SaveProviderToken(ProviderAccessToken token)
        //    {
        //        using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForAssembly())
        //        {
        //            using (var isolatedStorageFileStream = isolatedStorage.OpenFile(
        //                Path.Combine(ProvidersDirectoryName, token.Provider), 
        //                FileMode.OpenOrCreate, FileAccess.Write))
        //            {
        //                using (var streamReader = new StreamWriter(isolatedStorageFileStream))
        //                {
        //                    await streamReader.WriteLineAsync(token.AccessToken);
        //                }
        //            }
        //        }
        //    }

        //    public static async Task<ProviderAccessToken> GetProviderToken(string provider)
        //    {
        //        using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForAssembly())
        //        {
        //            //TODO
        //            if (!isolatedStorage.FileExists(Path.Combine(ProvidersDirectoryName, provider)))
        //            {
        //                return null;
        //            }

        //            using (var isolatedStorageFileStream = isolatedStorage.OpenFile(
        //                Path.Combine(ProvidersDirectoryName, provider),
        //                FileMode.Open, FileAccess.Read))
        //            {
        //                using (var streamReader = new StreamReader(isolatedStorageFileStream))
        //                {
        //                    return new ProviderAccessToken
        //                    {
        //                        Provider = provider,
        //                        AccessToken = await streamReader.ReadLineAsync()
        //                    };
        //                }
        //            }
        //        }
        //    }
    }
}
