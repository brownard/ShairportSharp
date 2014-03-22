using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ShairportSharp.Helpers
{
    enum CaseType
    {
        Upper,
        Lower,
        Digit
    }

    public static class NetworkAddressExtensionMethods
    {
        public static string HexStringFromBytes(this byte[] addressBytes, string seperator = null)
        {
            string addressString = "";
            if (addressBytes == null || addressBytes.Length == 0)
                return addressString;

            bool addSeperator = !string.IsNullOrEmpty(seperator);
            for (int x = 0; x < addressBytes.Length - 1; x++)
            {
                addressString += addressBytes[x].ToString("X2");
                if (addSeperator)
                    addressString += seperator;
            }
            addressString += addressBytes[addressBytes.Length - 1].ToString("X2");
            return addressString;
        }

        public static byte[] BytesFromHexString(this string value, string seperator = null)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            if (!string.IsNullOrEmpty(seperator))
            {
                value = value.Replace(seperator, "");
                if(value.Length == 0)
                    return null;
            }

            if(value.Length % 2 != 0)
                return null;

            byte[] bytes;
            try
            {
                int length = value.Length / 2;
                bytes = new byte[length];
                for (int x = 0; x < length; x++)
                    bytes[x] = byte.Parse(value.Substring(x * 2, 2), NumberStyles.HexNumber);
            }
            catch
            {
                return null;
            }
            return bytes;
        }
    }

    static class ExtensionMethods
    {
        public static string ComputerNameIfNullOrEmpty(this string name)
        {
            if (string.IsNullOrEmpty(name))
                return SystemInformation.ComputerName;
            return name;
        }

        public static uint UIntFromBigEndian(this byte[] buffer, int offset, int count)
        {
            if (count > 4)
                count = 4;
            uint result = 0;
            for (int x = 0; x < count; x++)
            {
                int shift = 8 * (count - 1 - x);
                result = result | (uint)buffer[x + offset] << shift;
            }
            return result;
        }

        public static int IntFromBigEndian(this byte[] buffer, int offset, int count)
        {
            if (count > 4)
                count = 4;
            int result = 0;
            for (int x = 0; x < count; x++)
            {
                int shift = 8 * (count - 1 - x);
                result = result | (int)buffer[x + offset] << shift;
            }
            return result;
        }

        public static int CheckValidPortNumber(this int port, int defaultValue, int range = 1)
        {
            if (range < 1)
                range = 1;
            else if (range > ushort.MaxValue)
                range = ushort.MaxValue;

            if (port < 1 || port > ushort.MaxValue + 1 - range)
            {
                Logger.Warn("Invalid port number {0}, setting to default {1}", port, defaultValue);
                port = defaultValue;
            }
            return port;
        }


        public static int IndexOf(this byte[] searchBytes, byte[] patternBytes)
        {
            int maxIndex = searchBytes.Length - patternBytes.Length;
            if (maxIndex < 0)
                return -1;

            for (int x = 0; x <= maxIndex; x++)
            {
                for (int y = 0; y < patternBytes.Length; y++)
                {
                    if (searchBytes[x + y] != patternBytes[y])
                        break;
                    if (y == patternBytes.Length - 1)
                        return x;
                }
            }
            return -1;
        }

        public static CaseType GetCasing(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return CaseType.Digit;

            for (int x = 0; x < value.Length; x++)
            {
                char c = value[x];
                if (!char.IsDigit(c))
                {
                    return char.IsLower(c) ? CaseType.Lower : CaseType.Upper;
                }
            }
            return CaseType.Digit;
        }

        static readonly Regex textParamatersReg = new Regex(@"([^:]+):\s*(.+)", RegexOptions.Compiled);
        public static Dictionary<string, string> AsTextParameters(this string value)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(value))
            {
                foreach (Match m in textParamatersReg.Matches(value))
                    parameters[m.Groups[1].Value] = m.Groups[2].Value;
            }
            return parameters;
        }

        public static Dictionary<string, string> GetQueryStringParameters(this string value)
        {
            Dictionary<string, string> query = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(value))
                return query;

            int index = value.IndexOf("?");
            if (index > -1)
                value = value.Substring(index + 1);

            string[] keyVals = value.Split('&');
            foreach (string keyVal in keyVals)
            {
                string[] keyValSplit = keyVal.Split('=');
                if (keyValSplit.Length == 2)
                    query[keyValSplit[0]] = keyValSplit[1];
            }

            return query;
        }
    }
}
