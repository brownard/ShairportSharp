using MediaPortal.Common;
using MediaPortal.Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AirPlayer.MediaPortal2
{
    class Logger : ShairportSharp.ILog
    {
        const string PREFIX = "[AIRPLAYER] ";
        static Logger instance;
        public static Logger Instance
        {
            get
            {
                if (instance == null)
                    instance = new Logger();
                return instance;
            }
        }

        public void Info(string format, params object[] args)
        {
            ServiceRegistration.Get<ILogger>().Info(getString(format, args));
        }
        
        public void Debug(string format, params object[] args)
        {
            ServiceRegistration.Get<ILogger>().Debug(getString(format, args));
        }

        public void Warn(string format, params object[] args)
        {
            ServiceRegistration.Get<ILogger>().Warn(getString(format, args));
        }

        public void Error(string format, params object[] args)
        {
            ServiceRegistration.Get<ILogger>().Error(getString(format, args));
        }

        public void Error(string message, Exception ex)
        {
            ServiceRegistration.Get<ILogger>().Error(getString(string.Format("{0} {1}\r\n{2}", message, ex.Message, ex.StackTrace)));
        }

        string getString(string format, params object[] args)
        {
            if (args == null || args.Length < 1)
                return PREFIX + format;
            return PREFIX + string.Format(format, args);
        }
    }
}
