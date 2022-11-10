using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Tni.Helper.Extensions
{
    /// <summary>
    /// Contains various utilities for IO
    /// </summary>
    public static class IOExtensions
    {
        #region DLL Import

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CopyFileEx(string lpExistingFileName, string lpNewFileName,
           CopyProgressRoutine lpProgressRoutine, IntPtr lpData, ref Int32 pbCancel,
           CopyFileFlags dwCopyFlags);

        delegate CopyProgressResult CopyProgressRoutine(
            long totalFileSize,
            long totalBytesTransferred,
            long streamSize,
            long streamBytesTransferred,
            uint dwStreamNumber,
            CopyProgressCallbackReason dwCallbackReason,
            IntPtr hSourceFile,
            IntPtr hDestinationFile,
            IntPtr lpData);

        enum CopyProgressResult : uint
        {
            PROGRESS_CONTINUE = 0,
            PROGRESS_CANCEL = 1,
            PROGRESS_STOP = 2,
            PROGRESS_QUIET = 3
        }

        enum CopyProgressCallbackReason : uint
        {
            CALLBACK_CHUNK_FINISHED = 0x00000000,
            CALLBACK_STREAM_SWITCH = 0x00000001
        }

        [Flags]
        enum CopyFileFlags : uint
        {
            COPY_FILE_FAIL_IF_EXISTS = 0x00000001,
            COPY_FILE_RESTARTABLE = 0x00000002,
            COPY_FILE_OPEN_SOURCE_FOR_WRITE = 0x00000004,
            COPY_FILE_ALLOW_DECRYPTED_DESTINATION = 0x00000008
        }

        #endregion

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

        /// <summary>
        /// Copy a file to target in async mode while providing progress of the copy.
        /// Meant for larger files where the app take its time.
        /// </summary>
        /// <param name="sourceFileName">Source file to be copied</param>
        /// <param name="targetFileName">Target file where to be copied</param>
        /// <param name="token">Cancellation token if interrupt before execution is needed</param>
        /// <param name="progress">Progress interface to provide feedback (optional)</param>
        /// <returns></returns>
        public static Task CopyAsyncTo(this FileInfo sourceFileName, FileInfo targetFileName, CancellationToken token, IProgress<double>? progress = null)
        {
            int pbCancel = 0;
            CopyProgressRoutine copyProgressHandler;
            if (progress != null)
            {
                copyProgressHandler = (total, transferred, streamSize, streamByteTrans, dwStreamNumber, reason, hSourceFile, hDestinationFile, lpData) =>
                {
                    progress.Report((double)transferred / total * 100);
                    return CopyProgressResult.PROGRESS_CONTINUE;
                };
            }
            else
            {
                copyProgressHandler = EmptyCopyProgressHandler;
            }
            token.ThrowIfCancellationRequested();
            var ctr = token.Register(() => pbCancel = 1);
            var copyTask = Task.Run(() =>
            {
                try
                {
                    CopyFileEx
                    (
                        sourceFileName.FullName, targetFileName.FullName,
                        copyProgressHandler, IntPtr.Zero, ref pbCancel,
                        CopyFileFlags.COPY_FILE_RESTARTABLE
                    );
                    token.ThrowIfCancellationRequested();
                }
                finally
                {
                    ctr.Dispose();
                }
            }, token);
            return copyTask;
        }

        /// <summary>
        /// Copy a stream to target in async mode while providing progress of the copy.
        /// Meant for larger files where the app take its time.
        /// </summary>
        /// <param name="source">Source stream to be copied</param>
        /// <param name="destination">Target stream where to be copied</param>
        /// <param name="bufferSize">Cancellation token if interrupt before execution is needed</param>
        /// <param name="progress">Progress interface to provide feedback (optional)</param>
        /// <param name="cancellationToken">Cancellation token if interrupt before execution is needed (optional)</param>
        /// <returns></returns>
        public static async Task CopyToAsync(this Stream source, Stream destination, int bufferSize,
            IProgress<long>? progress = null, CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (!source.CanRead)
                throw new ArgumentException("Has to be readable", nameof(source));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (!destination.CanWrite)
                throw new ArgumentException("Has to be writable", nameof(destination));
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            var buffer = new byte[bufferSize];
            long totalBytesRead = 0;
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                totalBytesRead += bytesRead;

                if (progress != null)
                    progress?.Report(totalBytesRead);
            }
        }

        #region Private methods
        private static CopyProgressResult EmptyCopyProgressHandler(long total, long transferred, long streamSize, long streamByteTrans, uint dwStreamNumber, CopyProgressCallbackReason reason, IntPtr hSourceFile, IntPtr hDestinationFile, IntPtr lpData)
        {
            return CopyProgressResult.PROGRESS_CONTINUE;
        }
        #endregion
    }
}
