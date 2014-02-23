using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ShairportSharp.Audio
{
    delegate void UDPDelegate(UdpClient socket, byte[] packet, IPEndPoint remoteEndPoint);

    class UdpListener
    {
        volatile bool stop = false;
        UdpClient socket;
        public event UDPDelegate OnPacketReceived;

        public UdpListener(UdpClient socket)
        {
            this.socket = socket;
        }

        public void Start()
        {
            stop = false;
            AsyncCallback cb = null;
            try
            {
                socket.BeginReceive(cb = ar =>
                {
                    try
                    {
                        IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
                        byte[] buffer = socket.EndReceive(ar, ref ipEndPoint);
                        if (!stop)
                        {
                            socket.BeginReceive(cb, null);
                            if (buffer.Length > 0 && OnPacketReceived != null)
                                OnPacketReceived(socket, buffer, ipEndPoint);
                        }
                    }
                    catch { }
                }, null);
            }
            catch (Exception ex)
            {
                Logger.Error("UDPListener: Exception starting receive -", ex);
            }
        }

        public void Stop()
        {
            stop = true;
        }
    }
}
