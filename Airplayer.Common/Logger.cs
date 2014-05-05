using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AirPlayer.Common
{
    public static class Logger
    {
        static ShairportSharp.ILog logger = null;
        public static void SetLogger(ShairportSharp.ILog logger)
        {
            Logger.logger = logger;
        }

        public static void Info(string format, params object[] args)
        {
            if (logger != null)
                logger.Info(format, args);
        }
        public static void Debug(string format, params object[] args)
        {
            if (logger != null)
                logger.Debug(format, args);
        }
        public static void Warn(string format, params object[] args)
        {
            if (logger != null)
                logger.Warn(format, args);
        }
        public static void Error(string format, params object[] args)
        {
            if (logger != null)
                logger.Error(format, args);
        }
        public static void Error(string message, Exception ex)
        {
            if (logger != null)
                logger.Error(message, ex);
        }
    }
}
