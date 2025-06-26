using PPEC.Communication.Enum;

namespace PPEC.Communication.Interface
{
    public interface ILogger
    {
        /// <summary>
        /// Conditionally log a message
        /// </summary>
        /// <param name="level"></param>
        /// <param name="message"></param>
        void Log(LoggingLevel level, string message);

        /// <summary>
        /// True if this level should be logged, false otherwise.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        bool ShouldLog(LoggingLevel level);
    }
}
