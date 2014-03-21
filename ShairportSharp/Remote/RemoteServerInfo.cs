using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Remote
{
    class RemoteServerInfo
    {
        public RemoteServerInfo(string dacpId, string activeRemote)
        {
            this.dacpId = dacpId;
            this.activeRemote = activeRemote;
        }

        string dacpId;
        public string DacpId 
        { 
            get { return dacpId; } 
        }

        string activeRemote;
        public string ActiveRemote
        {
            get { return activeRemote; }
        }
    }
}
