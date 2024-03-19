using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AZ.FolderSystemWatcher.Next
{
    public static class Utility
    {
        /// <summary>
        /// 判断给定地址是否为文件夹
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static bool IsDir(string filepath)
        {
            var fi = new FileInfo(filepath);
            return (fi.Attributes & FileAttributes.Directory) != 0;
        }

        /// <summary>
        /// 判断给定地址是否为文件夹
        /// </summary>
        /// <param name="fileinfo"></param>
        /// <returns></returns>
        public static bool IsDir(FileInfo fileinfo)
        {
            return (fileinfo.Attributes & FileAttributes.Directory) != 0;
        }

        /// <summary>
        /// 获取文件的MD5值
        /// </summary>
        /// <param name="bytes">文件的字节数组</param>
        /// <returns>无连字符的MD5值</returns>
        public static string ComputeMd5(byte[] bytes)
        {
            return BitConverter.ToString(new MD5CryptoServiceProvider().ComputeHash(bytes)).Replace("-", "");
        }

        /// <summary>
        /// 计算文件的Hash代码
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string ComputeFileHash(string filePath)
        {
            return ComputeMd5(File.ReadAllBytes(filePath));
        }

        /// <summary>
        /// 获取字符串的MD5值
        /// </summary>
        /// <param name="s">字符串</param>
        /// <returns>无连字符的MD5值</returns>
        public static string ComputeMd5(string s)
        {
            return BitConverter.ToString(new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(s))).Replace("-", "");
        }

        /// <summary>
        /// 是否是文件夹
        /// </summary>
        /// <param name="filepath">路径</param>
        /// <returns>如果路径无法访问，返回空值</returns>
        public static bool? IsDirectory(string filepath)
        {
            // 如果是文件夹
            if (Directory.Exists(filepath))
            {
                return true;
            }
            // 如果是文件
            if (File.Exists(filepath))
            {
                return false;
            }
            // 否则 返回空值
            return null;
        }
        /// <summary>
        /// 获取文件夹大小
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static long GetDirectorySize(DirectoryInfo dir)
        {
            return dir.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);
        }
        /// <summary>
        /// 获取文件夹大小
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public static long GetDirectorySize(string folderPath)
        {
            return GetDirectorySize(new DirectoryInfo(folderPath));
        }
    }
}
