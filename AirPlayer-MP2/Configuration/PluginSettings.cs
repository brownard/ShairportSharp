using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ShairportSharp.Helpers;
using MediaPortal.Common.Settings;
using MediaPortal.Common;

namespace AirPlayer.MediaPortal2.Configuration
{
    public class PluginSettings
    {
        const int DEFAULT_RTSP_PORT = 50500;
        const int DEFAULT_UDP_PORT = 50510;
        const int DEFAULT_HTTP_PORT = 60500;

        const double DEFAULT_AUDIO_BUFFER = 2;
        const double DEFAULT_VIDEO_BUFFER = 2;

        [Setting(SettingScope.User, System.Windows.Forms.SystemInformation.ComputerName)]
        public string ServerName { get; set; }

        [Setting(SettingScope.User, null)]
        public string Password { get; set; }

        [Setting(SettingScope.User, null)]
        public byte[] CustomAddress { get; set; }

        [Setting(SettingScope.User, DEFAULT_RTSP_PORT)]
        public int RtspPort { get; set; }

        [Setting(SettingScope.User, DEFAULT_UDP_PORT)]
        public int UdpPort { get; set; }

        [Setting(SettingScope.User, DEFAULT_AUDIO_BUFFER)]
        public double AudioBuffer { get; set; }

        [Setting(SettingScope.User, true)]
        public bool AllowVolume { get; set; }

        [Setting(SettingScope.User, true)]
        public bool SendAudioCommands { get; set; }

        [Setting(SettingScope.User, DEFAULT_HTTP_PORT)]
        public int AirplayPort { get; set; }

        [Setting(SettingScope.User, true)]
        public bool AllowHDStreams { get; set; }

        [Setting(SettingScope.User, DEFAULT_VIDEO_BUFFER)]
        public int VideoBuffer { get; set; }

        public static PluginSettings Load()
        {
            return ServiceRegistration.Get<ISettingsManager>().Load<PluginSettings>();
        }

        public void Save()
        {
            ServiceRegistration.Get<ISettingsManager>().Save(this);
        }        
    }
}
