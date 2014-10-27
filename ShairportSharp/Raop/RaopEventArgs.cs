using ShairportSharp.Remote;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Raop
{
    public class RaopEventArgs : EventArgs
    {
        public RaopEventArgs(string sessionId)
        {
            SessionId = sessionId;
        }

        public string SessionId { get; protected set; }
    }

    public class VolumeChangedEventArgs : RaopEventArgs
    {
        public VolumeChangedEventArgs(double volume, string sessionId)
            : base(sessionId)
        {
            Volume = volume;
        }

        public double Volume { get; private set; }
    }

    public class VolumeRequestedEventArgs : RaopEventArgs
    {
        public VolumeRequestedEventArgs(string sessionId)
            : base(sessionId)
        { }

        public double Volume { get; set; }
    }

    public class MetaDataChangedEventArgs : RaopEventArgs
    {
        public MetaDataChangedEventArgs(DmapData metaData, string sessionId)
            : base(sessionId)
        {
            MetaData = metaData;
        }

        public DmapData MetaData { get; private set; }
    }

    public class ArtwokChangedEventArgs : RaopEventArgs
    {
        public ArtwokChangedEventArgs(byte[] imageData, string contentType, string sessionId)
            : base(sessionId)
        {
            ImageData = imageData;
            ContentType = contentType;
        }

        public byte[] ImageData { get; private set; }
        public string ContentType { get; private set; }
    }

    public class PlaybackProgressChangedEventArgs : RaopEventArgs
    {
        public PlaybackProgressChangedEventArgs(uint start, uint stop, uint current, string sessionId)
            : base(sessionId)
        {
            Start = start;
            Stop = stop;
            Current = current;
        }

        public uint Start { get; private set; }
        public uint Stop { get; private set; }
        public uint Current { get; private set; }
    }

    class RemoteInfoFoundEventArgs : RaopEventArgs
    {
        public RemoteInfoFoundEventArgs(RemoteServerInfo remoteServer, string sessionId)
            : base(sessionId)
        {
            RemoteServer = remoteServer;
        }

        public RemoteServerInfo RemoteServer { get; private set; }
    }
}
