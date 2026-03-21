using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace xinglin.Services.Data
{
    public interface IFileStorageService
    {
        /// <summary>
        /// 确保目录存在
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        void EnsureDirectoryExists(string directoryPath);

        /// <summary>
        /// 获取目录下的所有文件
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        /// <param name="searchPattern">搜索模式</param>
        /// <param name="recursive">是否递归查找子目录</param>
        /// <returns>文件路径列表</returns>
        string[] GetFiles(string directoryPath, string searchPattern, bool recursive = false);

        /// <summary>
        /// 获取目录下的所有子目录
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        /// <returns>子目录路径列表</returns>
        string[] GetDirectories(string directoryPath);

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件是否存在</returns>
        bool FileExists(string filePath);

        /// <summary>
        /// 读取文件内容（异步）
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件内容</returns>
        Task<string> ReadAllTextAsync(string filePath);

        /// <summary>
        /// 读取文件内容（同步）
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件内容</returns>
        string ReadAllText(string filePath);

        /// <summary>
        /// 写入文件内容（异步）
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="content">文件内容</param>
        Task WriteAllTextAsync(string filePath, string content);

        /// <summary>
        /// 写入文件内容（同步）
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="content">文件内容</param>
        void WriteAllText(string filePath, string content);

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        void DeleteFile(string filePath);

        /// <summary>
        /// 获取文件名
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件名</returns>
        string GetFileName(string filePath);

        /// <summary>
        /// 获取文件名（不含扩展名）
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件名（不含扩展名）</returns>
        string GetFileNameWithoutExtension(string filePath);
    }
}
