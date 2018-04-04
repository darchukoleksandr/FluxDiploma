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

        public static async Task<string> ReadSavedAccessTokens()
        {
            using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                using (var isolatedStorageFileStream = isolatedStorage.OpenFile(TokensFileName, FileMode.Open, FileAccess.Read))
                {
                    using (var streamReader = new StreamReader(isolatedStorageFileStream))
                    {
                        var accessToken = await streamReader.ReadLineAsync();
                        return accessToken;
                    }
                }
            }
        }

        public static async Task SaveOauthTokens(string accessToken)
        {
            await ReadFile();
            using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                using (var isolatedStorageFileStream = isolatedStorage.OpenFile(TokensFileName, FileMode.Open, FileAccess.Write))
                {
                    using (var streamReader = new StreamWriter(isolatedStorageFileStream))
                    {
                        await streamReader.WriteLineAsync(accessToken);
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
    }
}
