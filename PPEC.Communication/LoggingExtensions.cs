using PPEC.Communication.Enum;
using PPEC.Communication.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public static class LoggingExtensions
    {
        #region Standard level-based logging

        public static void Trace(this ILogger logger, string message)
        {
            logger.Log(LoggingLevel.Trace, message);
        }

        public static void Debug(this ILogger logger, string message)
        {
            logger.Log(LoggingLevel.Debug, message);
        }

        public static void Information(this ILogger logger, string message)
        {
            logger.Log(LoggingLevel.Information, message);
        }

        public static void Warning(this ILogger logger, string message)
        {
            logger.Log(LoggingLevel.Warning, message);
        }

        public static void Error(this ILogger logger, string message)
        {
            logger.Log(LoggingLevel.Error, message);
        }

        public static void Critical(this ILogger logger, string message)
        {
            logger.Log(LoggingLevel.Critical, message);
        }

        #endregion

        #region Func Logging
        public static void Log(this ILogger logger, LoggingLevel level, Func<string> messageFactory)
        {
            if (logger.ShouldLog(level))
            {
                string message = messageFactory();

                logger.Log(level, message);
            }
        }

        public static void Trace(this ILogger logger, Func<string> messageFactory)
        {
            logger.Log(LoggingLevel.Trace, messageFactory);
        }

        public static void Debug(this ILogger logger, Func<string> messageFactory)
        {
            logger.Log(LoggingLevel.Debug, messageFactory);
        }

        public static void Information(this ILogger logger, Func<string> messageFactory)
        {
            logger.Log(LoggingLevel.Information, messageFactory);
        }

        public static void Warning(this ILogger logger, Func<string> messageFactory)
        {
            logger.Log(LoggingLevel.Warning, messageFactory);
        }

        public static void Error(this ILogger logger, Func<string> messageFactory)
        {
            logger.Log(LoggingLevel.Error, messageFactory);
        }

        public static void Critical(this ILogger logger, Func<string> messageFactory)
        {
            logger.Log(LoggingLevel.Critical, messageFactory);
        }

        #endregion

        #region Frame logging

        private static void LogFrame(this ILogger logger, string validPrefix, string invalidPrefix, byte[] frame)
        {
            if (logger.ShouldLog(LoggingLevel.Trace))
            {
                if (logger.ShouldLog(LoggingLevel.Trace))
                {
                    string prefix = frame.DoesCrcMatch() ? validPrefix : invalidPrefix;

                    logger.Trace($"{prefix}: {string.Join(" ", frame.Select(b => b.ToString("X2")))}");
                }
            }
        }

        internal static void LogFrameTx(this ILogger logger, byte[] frame)
        {
            logger.LogFrame("TX", "tx", frame);
        }

        internal static void LogFrameRx(this ILogger logger, byte[] frame)
        {
            logger.LogFrame("RX", "rx", frame);
        }

        internal static void LogFrameIgnoreRx(this ILogger logger, byte[] frame)
        {
            logger.LogFrame("IR", "ir", frame);
        }

        #endregion  
    }
}
