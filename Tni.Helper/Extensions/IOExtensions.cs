using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tni.Helper.Extensions
{
    /// <summary>
    /// Contains various utilities for IO
    /// </summary>
    public static class IOExtensions
    {
        /// <summary>
        /// Returns a string to a DirectoryInfo object
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static DirectoryInfo ToDirectoryInfo(this string source)
        {
            return new DirectoryInfo(source);
        }

        /// <summary>
        /// Returns a string to a FileInfo object
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static FileInfo ToFileInfo(this string source)
        {
            return new FileInfo(source);
        }

        /// <summary>
        /// Ensure that an specified directory exists.
        /// </summary>
        /// <param name="source">Specified folder</param>
        /// <returns></returns>
        public static DirectoryInfo EnsureExists(this DirectoryInfo source)
        {
            source.Refresh();
            if (!source.Exists)
                source.Create();
            return source;
        }

        /// <summary>
        /// Maps a child directory to specified parent and optionally it creates it if not existing.
        /// </summary>
        /// <param name="source">Parent folder</param>
        /// <param name="child">Child folder</param>
        /// <param name="assureFolderExists">Optional if child should be created.</param>
        /// <returns></returns>
        public static DirectoryInfo Child(this DirectoryInfo source, string child, bool assureFolderExists = true)
        {
            var childFolder = new DirectoryInfo(Path.Combine(source.FullName, child));
            if (assureFolderExists && !childFolder.Exists)
                childFolder.Create();
            return childFolder;
        }

        /// <summary>
        /// Maps a file to an specified folder
        /// </summary>
        /// <param name="source"></param>
        /// <param name="child"></param>
        /// <returns></returns>
        public static FileInfo ChildFile(this DirectoryInfo source, string child)
        {
            return new FileInfo(Path.Combine(source.FullName, child));
        }

        /// <summary>
        /// Returns in a friendly manner the size of an specified FileInfo
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string DisplaySize(this FileInfo source)
        {
            return source.Length.DisplaySize();
        }

        /// <summary>
        /// Returns in a friendy manner the size of an specified long value.
        /// </summary>
        /// <param name="byteCount"></param>
        /// <returns></returns>
        public static string DisplaySize(this long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };

            if (byteCount == 0)
                return "0" + suf[0];

            var bytes = Math.Abs(byteCount);
            var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            var num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }
    }
}
