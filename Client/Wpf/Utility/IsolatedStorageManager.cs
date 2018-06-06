using System.IO;
using System.IO.IsolatedStorage;
using System.Text;
using System.Threading.Tasks;

namespace Client.Wpf.Utility
{
    public class IsolatedStorageManager
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

        public static async Task<byte[]> ReadFile(string fileName)
        {
            using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                using (var isolatedStorageFileStream = isolatedStorage.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                {
                    var result = new byte[isolatedStorageFileStream.Length];
                    await isolatedStorageFileStream.ReadAsync(result, 0, result.Length);
                    return result;
                }
            }
        }

        public static async Task<string> ReadSavedAccessTokens()
        {
            using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                using (var isolatedStorageFileStream = isolatedStorage.OpenFile(TokensFileName, FileMode.Open, FileAccess.Read))
                {
                    var resultBytes = new byte[isolatedStorageFileStream.Length];
                    await isolatedStorageFileStream.ReadAsync(resultBytes, 0, resultBytes.Length);
                    return Encoding.UTF8.GetString(resultBytes);
                }
            }
        }

        public static async Task SaveFile(string fileName, byte[] data)
        {
            using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                using (var isolatedStorageFileStream = isolatedStorage.CreateFile(fileName))
                {
                    await isolatedStorageFileStream.WriteAsync(data, 0, data.Length);
                }
            }
        }

        public static void DeleteOauthTokens()
        {
            using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                using (isolatedStorage.CreateFile(TokensFileName))
                {
                }
            }
        }
    }
}
