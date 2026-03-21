using System.IO;
using System.Threading.Tasks;

namespace xinglin.Services.Data
{
    public class LocalFileStorageService : IFileStorageService
    {
        public void EnsureDirectoryExists(string directoryPath)
        {
            Directory.CreateDirectory(directoryPath);
        }

        public string[] GetFiles(string directoryPath, string searchPattern, bool recursive = false)
        {
            if (recursive)
            {
                return Directory.GetFiles(directoryPath, searchPattern, SearchOption.AllDirectories);
            }
            else
            {
                return Directory.GetFiles(directoryPath, searchPattern, SearchOption.TopDirectoryOnly);
            }
        }

        public string[] GetDirectories(string directoryPath)
        {
            return Directory.GetDirectories(directoryPath);
        }

        public bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        public async Task<string> ReadAllTextAsync(string filePath)
        {
            return await File.ReadAllTextAsync(filePath);
        }

        public string ReadAllText(string filePath)
        {
            return File.ReadAllText(filePath);
        }

        public async Task WriteAllTextAsync(string filePath, string content)
        {
            await File.WriteAllTextAsync(filePath, content);
        }

        public void WriteAllText(string filePath, string content)
        {
            File.WriteAllText(filePath, content);
        }

        public void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public string GetFileName(string filePath)
        {
            return Path.GetFileName(filePath);
        }

        public string GetFileNameWithoutExtension(string filePath)
        {
            return Path.GetFileNameWithoutExtension(filePath);
        }
    }
}
