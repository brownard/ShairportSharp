using ShairportSharp.Remote;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Raop
{
    public class VolumeChangedEventArgs : EventArgs
    {
        public VolumeChangedEventArgs(double volume)
        {
            Volume = volume;
        }

        public double Volume { get; private set; }
    }

    public class VolumeRequestedEventArgs : EventArgs
    {
        public double Volume { get; set; }
    }

    public class MetaDataChangedEventArgs : EventArgs
    {
        public MetaDataChangedEventArgs(DmapData metaData)
        {
            MetaData = metaData;
        }

        public DmapData MetaData { get; private set; }
    }

    public class ArtwokChangedEventArgs : EventArgs
    {
        public ArtwokChangedEventArgs(byte[] imageData, string contentType)
        {
            ImageData = imageData;
            ContentType = contentType;
        }

        public byte[] ImageData { get; private set; }
        public string ContentType { get; private set; }
    }

    public class PlaybackProgressChangedEventArgs : EventArgs
    {
        public PlaybackProgressChangedEventArgs(uint start, uint stop, uint current)
        {
            Start = start;
            Stop = stop;
            Current = current;
        }

        public uint Start { get; private set; }
        public uint Stop { get; private set; }
        public uint Current { get; private set; }
    }

    class RemoteInfoFoundEventArgs : EventArgs
    {
        public RemoteInfoFoundEventArgs(RemoteServerInfo remoteServer)
        {
            RemoteServer = remoteServer;
        }

        public RemoteServerInfo RemoteServer { get; private set; }
    }
}
