using ShairportSharp.Http;
using ShairportSharp.Sap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace ShairportSharp.Base
{
    public abstract class SapSession : HttpParser
    {
        protected SapHandler sapHandler;

        public SapSession(Socket socket, string password, string digestRealm)
            : base(socket, password, digestRealm)
        { }

        public event EventHandler Authenticating;
        protected virtual void OnAuthenticating()
        {
            if (Authenticating != null)
                Authenticating(this, EventArgs.Empty);
        }

        protected byte[] GetSapResponse(byte[] challenge)
        {
            int stage;
            if (sapHandler == null)
            {
                OnAuthenticating();
                sapHandler = new SapHandler();
                Logger.Debug("SapSession: Init SAP");
                sapHandler.Init();
                stage = 0;
            }
            else
            {
                stage = 1;
            }
            Logger.Debug("SapSession: Challenge {0}", stage);
            return sapHandler.Challenge(challenge, stage);
        }

        protected byte[] DecryptSapKey(byte[] encryptedKey)
        {
            if (sapHandler == null)
                return null;

            Logger.Debug("SapSession: Decrypting key");
            byte[] decryptedKey = sapHandler.DecryptKey(encryptedKey);
            sapHandler = null;
            return decryptedKey;
        }
    }
}
