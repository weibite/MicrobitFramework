using System;
using System.IO;
using System.Threading.Tasks;
using NLog;
using NLog.Config;

namespace Microbit.Utils
{
    /// <summary>
    /// 日志封装类
    /// </summary>
    public static class Log
    {
        private static Logger logger = null;

        static Log()
        {
            logger = LogManager.GetLogger("DefaultLog");
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NLog.config");
            if (!File.Exists(configPath))
            {
                configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs\\NLog.config");
                if (!File.Exists(configPath)) return;
            }
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configPath);
            LogManager.Configuration = new XmlLoggingConfiguration(filePath);
        }

        /// <summary>
        /// 写调试日志
        /// </summary>
        /// <param name="message"></param>
        public static void Debug(string message)
        {
            Task.Factory.StartNew(() =>
            {
                if (logger == null) return;
                logger.Debug(message);
            });
        }

        /// <summary>
        /// 写异常日志
        /// </summary>
        /// <param name="message"></param>
        public static void Error(string message)
        {
            Task.Factory.StartNew(() =>
            {
                if (logger == null) return;
                logger.Error(message);
            });
        }
    }
}
