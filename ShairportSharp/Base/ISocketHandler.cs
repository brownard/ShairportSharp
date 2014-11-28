using ShairportSharp.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Base
{
    public interface ISocketHandler
    {
        void Start();
        void Close();
        event EventHandler<ClosedEventArgs> Closed;
    }
}
