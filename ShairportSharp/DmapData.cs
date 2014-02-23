using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp
{
    public class DmapData
    {
        public DmapData(byte[] data)
        {
            Encoding encoding = Encoding.ASCII;
            int index = 8; //skip top header and length
            while (index < data.Length)
            {
                string tag = encoding.GetString(data, index, 4);
                index += 4;
                int tagLength = data.IntFromBigEndian(index, 4);//Big endian, we're little
                index += 4;
                switch (tag)
                {
                    case "asal":
                        Album = encoding.GetString(data, index, tagLength);
                        break;
                    case "asar":
                        Artist = encoding.GetString(data, index, tagLength);
                        break;
                    case "asgn":
                        Genre = encoding.GetString(data, index, tagLength);
                        break;
                    case "minm":
                        Track = encoding.GetString(data, index, tagLength);
                        break;
                }
                index += tagLength;
            }
        }

        public string Track
        {
            get;
            private set;
        }

        public string Album
        {
            get;
            private set;
        }

        public string Artist
        {
            get;
            private set;
        }

        public string Genre
        {
            get;
            private set;
        }
    }
}
