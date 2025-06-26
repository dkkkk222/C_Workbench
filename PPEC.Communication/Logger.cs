using PPEC.Communication.Enum;
using PPEC.Communication.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public class Logger : ILogger
    {
        //读写锁，当资源处于写入模式时，其他线程写入需要等待本次写入结束之后才能继续写入
        private static readonly ReaderWriterLockSlim LogWriteLock = new ReaderWriterLockSlim();
        private const int LevelColumnSize = 15;
        private static readonly string BlankHeader = Environment.NewLine + new string(' ', LevelColumnSize);
        private static readonly object _locker = new object();
        private static Logger _instance;
        public static Logger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_locker)
                    {
                        if (_instance == null) _instance = new Logger();
                    }
                }
                return _instance;
            }
        }
        public Logger()
        {
            MinimumLoggingLevel = LoggingLevel.Debug;
        }

        protected LoggingLevel MinimumLoggingLevel { get; }

        /// <summary>
        /// Returns true if the level should be loggged, false otherwise.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public bool ShouldLog(LoggingLevel level)
        {
            return level >= MinimumLoggingLevel;
        }

        /// <summary>
        /// Log the specified message at the specified level.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="message"></param>
        public void Log(LoggingLevel level, string message)
        {
            if (ShouldLog(level))
            {
                LogCore(level, message);
            }
        }
        /// <summary>
        /// Override this method to implement logging behavior. This function will only be called if ShouldLog(level) is true.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="message"></param>
        protected void LogCore(LoggingLevel level, string message)
        {

            if (!Directory.Exists("Log"))
            {
                Directory.CreateDirectory("Log");
            }
            DateTime now = DateTime.Now;
            string logpath = @"Log\" + now.Year + "" + now.Month + "" + now.Day + ".log";
            message = message?.Replace(Environment.NewLine, BlankHeader);

            try
            {
                //设置读写锁为写入模式独占资源，其他写入请求需要等待本次写入结束之后才能继续写入
                LogWriteLock.EnterWriteLock();
                File.AppendAllText(logpath, $"[{level}]".PadRight(LevelColumnSize) + message + "\r\n");
            }
            finally
            {
                //退出写入模式，释放资源占用
                LogWriteLock.ExitWriteLock();
            }
        }
    }
}
