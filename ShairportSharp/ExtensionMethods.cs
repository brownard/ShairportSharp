using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ShairportSharp
{
    static class ExtensionMethods
    {
        public static string ComputerNameIfNullOrEmpty(this string name)
        {
            if (string.IsNullOrEmpty(name))
                return SystemInformation.ComputerName;
            return name;
        }

        public static string StringFromAddressBytes(this byte[] addressBytes, string seperator = null)
        {
            string addressString = "";
            if (addressBytes == null || addressBytes.Length < 1)
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

        public static int GetValidPortNumber(this int port, int defaultValue, int range = 1)
        {
            return port > 0 && port <= ushort.MaxValue + 1 - range ? port : defaultValue;
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
    }
}
