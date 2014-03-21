using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp
{
    public interface ILog
    {
        void Info(string format, params object[] args);
        void Debug(string format, params object[] args);
        void Warn(string format, params object[] args);
        void Error(string format, params object[] args);
        void Error(string message, Exception ex);
    }
}
