using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShairportSharp_Test
{
    class Log : ShairportSharp.ILog
    {
        public event EventHandler<LogEventArgs> NewLog;
        void OnNewLog(LogEventArgs e)
        {
            if (NewLog != null)
                NewLog(this, e);
        }

        public void Info(string format, params object[] args)
        {
            string message;
            if(args != null && args.Length > 0)
                message = string.Format(format, args);
            else
                message = format;
            OnNewLog(new LogEventArgs(message, "Info", DateTime.Now));
        }

        public void Debug(string format, params object[] args)
        {
            string message;
            if (args != null && args.Length > 0)
                message = string.Format(format, args);
            else
                message = format;
            OnNewLog(new LogEventArgs(message, "Debug", DateTime.Now));
        }

        public void Warn(string format, params object[] args)
        {
            string message;
            if (args != null && args.Length > 0)
                message = string.Format(format, args);
            else
                message = format;
            OnNewLog(new LogEventArgs(message, "Warn", DateTime.Now));
        }

        public void Error(string format, params object[] args)
        {
            string message;
            if (args != null && args.Length > 0)
                message = string.Format(format, args);
            else
                message = format;
            OnNewLog(new LogEventArgs(message, "Error", DateTime.Now));
        }

        public void Error(string message, Exception ex)
        {
            message = string.Format("{0} - {1}\r\n{2}", message, ex.Message, ex.StackTrace);
            OnNewLog(new LogEventArgs(message, "Error", DateTime.Now));
        }
    }

    class LogEventArgs : EventArgs
    {
        public LogEventArgs(string message, string level, DateTime time)
        {
            Message = message;
            Level = level;
            Time = time;
        }

        public string Message { get; private set; }
        public string Level { get; private set; }
        public DateTime Time { get; private set; }
    }
}
