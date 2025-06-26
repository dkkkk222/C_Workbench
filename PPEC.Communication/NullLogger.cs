using PPEC.Communication.Enum;
using PPEC.Communication.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public class NullLogger : ILogger
    {
        /// <summary>
        /// Singleton.
        /// </summary>
        public static NullLogger Instance = new NullLogger();

        private NullLogger()
        {
        }

        /// <summary>
        /// This won't do anything.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="message"></param>
        public void Log(LoggingLevel level, string message)
        {
        }

        /// <summary>
        /// Always returnsa false
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public bool ShouldLog(LoggingLevel level)
        {
            return false;
        }
    }
}
