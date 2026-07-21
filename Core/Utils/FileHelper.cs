using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EVESyncTool.Core.Utils
{
    /// <summary>
    /// 文件操作辅助类
    /// </summary>
    public static class FileHelper
    {
        #region 文件信息

        /// <summary>
        /// 获取文件大小（人类可读格式）
        /// </summary>
        public static string GetFileSizeString(string filePath)
        {
            if (!File.Exists(filePath))
                return "0 B";

            var info = new FileInfo(filePath);
            return GetSizeString(info.Length);
        }

        /// <summary>
        /// 获取文件大小（人类可读格式）
        /// </summary>
        public static string GetSizeString(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// 获取文件的 MD5 哈希值
        /// </summary>
        public static string GetFileMD5(string filePath)
        {
            if (!File.Exists(filePath))
                return string.Empty;

            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            byte[] hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// 获取文件的 SHA256 哈希值
        /// </summary>
        public static string GetFileSHA256(string filePath)
        {
            if (!File.Exists(filePath))
                return string.Empty;

            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            byte[] hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        #endregion

        #region 目录操作

        /// <summary>
        /// 获取目录大小（递归）
        /// </summary>
        public static long GetDirectorySize(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                return 0;

            long size = 0;
            try
            {
                var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    try
                    {
                        size += new FileInfo(file).Length;
                    }
                    catch
                    {
                        // 忽略无法访问的文件
                    }
                }
            }
            catch
            {
                // 忽略权限问题
            }
            return size;
        }

        /// <summary>
        /// 获取目录大小（人类可读格式）
        /// </summary>
        public static string GetDirectorySizeString(string directoryPath)
        {
            return GetSizeString(GetDirectorySize(directoryPath));
        }

        /// <summary>
        /// 获取目录下的文件数量（递归）
        /// </summary>
        public static int GetFileCount(string directoryPath, string searchPattern = "*")
        {
            if (!Directory.Exists(directoryPath))
                return 0;

            try
            {
                return Directory.GetFiles(directoryPath, searchPattern, SearchOption.AllDirectories).Length;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 确保目录存在
        /// </summary>
        public static void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        /// <summary>
        /// 清空目录（删除所有子目录和文件）
        /// </summary>
        public static void ClearDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                return;

            try
            {
                foreach (string file in Directory.GetFiles(directoryPath))
                {
                    try { File.Delete(file); } catch { }
                }
                foreach (string dir in Directory.GetDirectories(directoryPath))
                {
                    try { Directory.Delete(dir, true); } catch { }
                }
            }
            catch
            {
                // 忽略清理失败
            }
        }

        #endregion

        #region 文件比较

        /// <summary>
        /// 比较两个文件是否相同（通过 MD5）
        /// </summary>
        public static bool AreFilesEqual(string filePath1, string filePath2)
        {
            if (!File.Exists(filePath1) || !File.Exists(filePath2))
                return false;

            string hash1 = GetFileMD5(filePath1);
            string hash2 = GetFileMD5(filePath2);
            return hash1 == hash2;
        }

        /// <summary>
        /// 比较两个文件是否相同（通过字节比较，适用于大文件）
        /// </summary>
        public static bool AreFilesEqualByteByByte(string filePath1, string filePath2)
        {
            if (!File.Exists(filePath1) || !File.Exists(filePath2))
                return false;

            var info1 = new FileInfo(filePath1);
            var info2 = new FileInfo(filePath2);

            if (info1.Length != info2.Length)
                return false;

            const int bufferSize = 8192;
            using var fs1 = new FileStream(filePath1, FileMode.Open, FileAccess.Read);
            using var fs2 = new FileStream(filePath2, FileMode.Open, FileAccess.Read);

            byte[] buffer1 = new byte[bufferSize];
            byte[] buffer2 = new byte[bufferSize];

            while (true)
            {
                int bytesRead1 = fs1.Read(buffer1, 0, bufferSize);
                int bytesRead2 = fs2.Read(buffer2, 0, bufferSize);

                if (bytesRead1 != bytesRead2)
                    return false;

                if (bytesRead1 == 0)
                    return true;

                if (!buffer1.Take(bytesRead1).SequenceEqual(buffer2.Take(bytesRead2)))
                    return false;
            }
        }

        #endregion

        #region 路径操作

        /// <summary>
        /// 获取安全的文件名（移除非法字符）
        /// </summary>
        public static string GetSafeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return fileName;

            char[] invalidChars = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(fileName);
            foreach (char c in invalidChars)
            {
                sb.Replace(c.ToString(), string.Empty);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 获取安全的路径（移除非法字符）
        /// </summary>
        public static string GetSafePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            char[] invalidChars = Path.GetInvalidPathChars();
            var sb = new StringBuilder(path);
            foreach (char c in invalidChars)
            {
                sb.Replace(c.ToString(), string.Empty);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 确保路径以目录分隔符结尾
        /// </summary>
        public static string EnsureTrailingSlash(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                return path + Path.DirectorySeparatorChar;
            }
            return path;
        }

        /// <summary>
        /// 获取相对路径
        /// </summary>
        public static string GetRelativePath(string fullPath, string basePath)
        {
            if (string.IsNullOrEmpty(fullPath) || string.IsNullOrEmpty(basePath))
                return fullPath;

            if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                return fullPath;

            string relative = fullPath.Substring(basePath.Length);
            return relative.TrimStart('\\', '/');
        }

        /// <summary>
        /// 合并路径（安全的 Path.Combine）
        /// </summary>
        public static string CombinePaths(params string[] paths)
        {
            if (paths == null || paths.Length == 0)
                return string.Empty;

            string result = paths[0];
            for (int i = 1; i < paths.Length; i++)
            {
                result = Path.Combine(result, paths[i]);
            }
            return result;
        }

        #endregion

        #region 异步操作

        /// <summary>
        /// 异步复制文件
        /// </summary>
        public static async Task CopyFileAsync(string sourcePath, string destPath, bool overwrite = true)
        {
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException($"源文件不存在: {sourcePath}");

            EnsureDirectoryExists(Path.GetDirectoryName(destPath));

            using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, true);
            using var destStream = new FileStream(destPath, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.None, 8192, true);
            await sourceStream.CopyToAsync(destStream);
        }

        /// <summary>
        /// 异步读取文件内容
        /// </summary>
        public static async Task<string> ReadAllTextAsync(string filePath, Encoding encoding = null)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"文件不存在: {filePath}");

            encoding ??= Encoding.UTF8;
            using var reader = new StreamReader(filePath, encoding);
            return await reader.ReadToEndAsync();
        }

        /// <summary>
        /// 异步写入文件内容
        /// </summary>
        public static async Task WriteAllTextAsync(string filePath, string content, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            EnsureDirectoryExists(Path.GetDirectoryName(filePath));
            using var writer = new StreamWriter(filePath, false, encoding);
            await writer.WriteAsync(content);
        }

        /// <summary>
        /// 异步读取文件字节
        /// </summary>
        public static async Task<byte[]> ReadAllBytesAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"文件不存在: {filePath}");

            using var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, true);
            byte[] buffer = new byte[sourceStream.Length];
            await sourceStream.ReadAsync(buffer, 0, buffer.Length);
            return buffer;
        }

        /// <summary>
        /// 异步写入文件字节
        /// </summary>
        public static async Task WriteAllBytesAsync(string filePath, byte[] data)
        {
            EnsureDirectoryExists(Path.GetDirectoryName(filePath));
            using var destStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
            await destStream.WriteAsync(data, 0, data.Length);
        }

        #endregion

        #region 临时文件

        /// <summary>
        /// 创建临时文件并写入内容
        /// </summary>
        public static string CreateTempFile(string content, string extension = ".tmp")
        {
            string tempPath = Path.GetTempFileName();
            if (!string.IsNullOrEmpty(extension))
            {
                string newPath = Path.ChangeExtension(tempPath, extension);
                if (File.Exists(newPath))
                    File.Delete(newPath);
                File.Move(tempPath, newPath);
                tempPath = newPath;
            }
            File.WriteAllText(tempPath, content);
            return tempPath;
        }

        /// <summary>
        /// 创建临时目录
        /// </summary>
        public static string CreateTempDirectory()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);
            return tempPath;
        }

        /// <summary>
        /// 安全删除文件（忽略异常）
        /// </summary>
        public static void SafeDeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch
            {
                // 忽略删除失败
            }
        }

        /// <summary>
        /// 安全删除目录（忽略异常）
        /// </summary>
        public static void SafeDeleteDirectory(string directoryPath)
        {
            try
            {
                if (Directory.Exists(directoryPath))
                    Directory.Delete(directoryPath, true);
            }
            catch
            {
                // 忽略删除失败
            }
        }

        #endregion
    }
}