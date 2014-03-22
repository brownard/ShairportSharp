using MediaPortal.Profile;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ShairportSharp.Helpers;

namespace AirPlayer
{
    class PluginSettings
    {
        const string SECTION_NAME = "AirPlay";
        const string SERVER_NAME = "server_name";
        const string SERVER_PASS = "server_password";
        const string CUSTOM_ADDRESS = "custom_address";
        const string RTSP_PORT = "rtsp_port";
        const string UDP_PORT = "udp_port";
        const string BUFFER_SIZE = "buffer_size";
        const string ALLOW_VOLUME = "allow_volume";
        const string SEND_COMMANDS = "send_commands";

        const string AIRPLAY_PORT = "airplay_port";
        const string ALLOW_HD_STREAMS = "allow_hd_streams";
        const string VIDEO_BUFFER = "video_buffer";

        const int DEFAULT_RTSP_PORT = 50500;
        const int DEFAULT_UDP_PORT = 50510;
        const int DEFAULT_HTTP_PORT = 60500;

        public string ServerName { get; set; }
        public string Password { get; set; }
        public byte[] CustomAddress { get; set; }
        public int RtspPort { get; set; }
        public int UdpPort { get; set; }
        public decimal AudioBuffer { get; set; }
        public bool AllowVolume { get; set; }
        public bool SendCommands { get; set; }

        public int AirplayPort { get; set; }
        public bool AllowHDStreams { get; set; }
        public int VideoBuffer { get; set; }

        public PluginSettings()
        {
            using (Settings settings = new MPSettings())
            {
                ServerName = settings.GetValueAsString(SECTION_NAME, SERVER_NAME, System.Windows.Forms.SystemInformation.ComputerName);
                Password = settings.GetValueAsString(SECTION_NAME, SERVER_PASS, null);
                CustomAddress = settings.GetValueAsString(SECTION_NAME, CUSTOM_ADDRESS, null).BytesFromHexString();
                RtspPort = settings.GetValueAsInt(SECTION_NAME, RTSP_PORT, DEFAULT_RTSP_PORT);
                UdpPort = settings.GetValueAsInt(SECTION_NAME, UDP_PORT, DEFAULT_UDP_PORT);
                AllowVolume = settings.GetValueAsBool(SECTION_NAME, ALLOW_VOLUME, true);
                SendCommands = settings.GetValueAsBool(SECTION_NAME, SEND_COMMANDS, true);

                decimal bufferSize;
                if (!decimal.TryParse(settings.GetValue(SECTION_NAME, BUFFER_SIZE), NumberStyles.Float, CultureInfo.InvariantCulture, out bufferSize))
                    bufferSize = 2M;
                AudioBuffer = bufferSize;

                AirplayPort = settings.GetValueAsInt(SECTION_NAME, AIRPLAY_PORT, DEFAULT_HTTP_PORT);
                AllowHDStreams = settings.GetValueAsBool(SECTION_NAME, ALLOW_HD_STREAMS, true);
                VideoBuffer = settings.GetValueAsInt(SECTION_NAME, VIDEO_BUFFER, 2);
            }
        }

        public void Save()
        {
            using (Settings settings = new MPSettings())
            {
                settings.SetValue(SECTION_NAME, SERVER_NAME, ServerName);
                settings.SetValue(SECTION_NAME, SERVER_PASS, Password);
                settings.SetValue(SECTION_NAME, CUSTOM_ADDRESS, CustomAddress.HexStringFromBytes());
                settings.SetValue(SECTION_NAME, RTSP_PORT, RtspPort);
                settings.SetValue(SECTION_NAME, UDP_PORT, UdpPort);
                settings.SetValue(SECTION_NAME, BUFFER_SIZE, AudioBuffer.ToString(CultureInfo.InvariantCulture));
                settings.SetValueAsBool(SECTION_NAME, ALLOW_VOLUME, AllowVolume);
                settings.SetValueAsBool(SECTION_NAME, SEND_COMMANDS, SendCommands);

                settings.SetValue(SECTION_NAME, AIRPLAY_PORT, AirplayPort);
                settings.SetValueAsBool(SECTION_NAME, ALLOW_HD_STREAMS, AllowHDStreams);
                settings.SetValue(SECTION_NAME, VIDEO_BUFFER, VideoBuffer);
            }
        }        
    }
}
