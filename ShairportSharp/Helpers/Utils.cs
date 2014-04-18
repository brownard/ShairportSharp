using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;

namespace ShairportSharp.Helpers
{
    static class Utils
    {
        public const string APPLE_USER_AGENT = "AppleCoreMedia/1.0.0.8F455 (AppleTV; U; CPU OS 4_3 like Mac OS X; en_en)"; 

        public static byte[] GetMacAddress()
        {
            byte[] macAddress = null;
            try
            {
                NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
                if (nics.Length > 0)
                    macAddress = nics[0].GetPhysicalAddress().GetAddressBytes();
                else
                    Logger.Error("No network connection detected");
            }
            catch (Exception ex)
            {
                Logger.Error("Error retrieving MAC address of network adapter -", ex);
            }
            return macAddress;
        }
    }
}
