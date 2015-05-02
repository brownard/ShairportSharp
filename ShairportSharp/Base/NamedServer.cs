using ShairportSharp.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Base
{
    public abstract class NamedServer<T> : Server<T> where T : HttpParser
    {
        protected BonjourEmitter Emitter;
        
        string name;
        /// <summary>
        // The broadcasted name of the Airplay server. Defaults to the machine name if null or empty.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        string password;
        /// <summary>
        /// The password needed to connect. Set to null or empty to not require a password.
        /// </summary>
        public string Password
        {
            get { return password; }
            set { password = value; }
        }

        byte[] macAddress = null;
        /// <summary>
        /// The MAC address used to identify this server. If null or empty the actual MAC address of this computer will be used.
        /// Set to an alternative value to allow multiple servers on the same computer.
        /// </summary>
        public byte[] MacAddress
        {
            get { return macAddress; }
            set { macAddress = value; }
        }

        string modelName = Constants.DEFAULT_MODEL_NAME;
        /// <summary>
        /// The model of the server, should be in the format '[NAME], [VERSION]'
        /// In most cases can be left as the default 'ShairportSharp, 1'
        /// </summary>
        public string ModelName
        {
            get { return modelName; }
            set { modelName = value; }
        }

        protected override void OnStarted()
        {
            if (Emitter != null)
                Emitter.Publish();
            base.OnStarted();
        }

        protected override void OnStopping()
        {
            if (Emitter != null)
            {
                Emitter.Stop();
                Emitter = null;
            }
            base.OnStopping();
        }
    }
}
