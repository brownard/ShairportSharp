﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;

namespace ShairportSharp
{
    static class Utils
    {
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

        static readonly Regex textParamatersReg = new Regex(@"([^:]+):\s*(.+)", RegexOptions.Compiled);
        public static Dictionary<string, string> ParseTextParameters(string textParameters)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            foreach (Match m in textParamatersReg.Matches(textParameters))
                parameters[m.Groups[1].Value] = m.Groups[2].Value;
            return parameters;
        }
    }
}