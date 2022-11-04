using System.Collections.Concurrent;
using System.Threading.Channels;
using Tni.Helper.Entities;
using Tni.Helper.Extensions;

namespace Tni.Helper
{
    /// <summary>
    /// Simple logger for messages that will be stored to the harddrive.
    /// </summary>
    public static class Log
    {
        #region Constructors
        static Log()
        {
            Folder = AppDomain.CurrentDomain.BaseDirectory
                .ToDirectoryInfo().Child("Logs").EnsureExists();
        }
        #endregion

        #region Properties

        /// <summary>
        /// Folder where the logs are getting created.
        /// </summary>
        public static DirectoryInfo Folder { get; set; }

        /// <summary>
        /// Indicates if log items of type debug should or should not be stored as well.
        /// </summary>
        public static bool StoreDebugMessages { get; set; }

        /// <summary>
        /// Cancellation toke source
        /// </summary>
        static CancellationTokenSource CancelToken { get; set; } = new CancellationTokenSource();

        /// <summary>
        /// Log queue
        /// </summary>
        static ConcurrentQueue<LogItem> Logs { get; set; } = new ConcurrentQueue<LogItem>();
        static bool IsRunning { get; set; }
        #endregion

        #region Public methods
        public static void Start()
        {
            CancelToken = new CancellationTokenSource();
            Task.Factory.StartNew(Cycler, TaskCreationOptions.LongRunning);
        }
        public static async Task Stop()
        {
            CancelToken.Cancel();

            while (IsRunning)
                await Task.Delay(TimeSpan.FromSeconds(1));

            CancelToken.Dispose();
        }

        /// <summary>
        /// Writes an log item of type error, optionally including the stack trace
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="includeStack"></param>
        public static void Write(Exception exception, bool includeStack = false)
        {
            var error = exception.Message;
            if (includeStack)
                error = $"{error}, stack: {exception.StackTrace}";

            Write(new LogItem(eMessageType.Error, exception.Message));
        }
        
        /// <summary>
        /// Writes an log item of various types
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        public static void Write(eMessageType messageType, string message)
        {
            Write(new LogItem(messageType, message));
        }

        /// <summary>
        /// Writes an log item
        /// </summary>
        /// <param name="logItem"></param>
        public static void Write(LogItem logItem)
        {
            if (CancelToken.IsCancellationRequested) return;
            if (!IsRunning) Start();
            Logs.Enqueue(logItem);
        }
        #endregion

        #region Private methods
        private async static Task Cycler()
        {
            IsRunning = true;
            while (true)
            {
                #region Write standard logs
                while (Logs.Count > 0)
                {
                    if (Logs.TryDequeue(out LogItem logItem))
                        HDDWrite(logItem);
                }
                #endregion

                #region Check if the cancel was requested
                if (CancelToken.IsCancellationRequested)
                {
                    HDDWrite(new LogItem()
                    {
                        MessageType = eMessageType.Information,
                        Message = "Was requested to log to exit",
                        RecordedAt = DateTime.Now
                    });
                    break;
                }
                else
                    await Task.Delay(TimeSpan.FromSeconds(5));
                #endregion
            }
            IsRunning = false;
        }
        private static void HDDWrite(LogItem logItem)
        {
            try
            {
                if (logItem.MessageType == eMessageType.Debug && !StoreDebugMessages)
                    return; //Debug messages are not stored.

                var logFile = Folder.ChildFile($"{DateTime.Now.ToString("yyyyMMdd HH")}.txt");
                var tryout = 3;
                while (true)
                {
                    try
                    {
                        using (var streamWriter = new StreamWriter(logFile.FullName, true))
                        {
                            streamWriter.WriteLine(logItem.ToString());
                            streamWriter.Flush();
                        }

                        //If reached here, the write was succesfull, must exit the while.
                        break;
                    }
                    catch (IOException ioe)
                    {
                        ///If it is occupied try a number of time to write that log
                        if (ioe.Message.Contains("The process cannot access the file") && tryout > 0)
                            tryout--;
                        else
                            throw ioe;
                    }
                }
            }
            catch
            {
                //No matter the error, do nothing, hope next time will be able to log.
            }
        }
        #endregion
    }
}