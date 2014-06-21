using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ShairportSharp.Helpers;
using MediaPortal.Common.Settings;
using MediaPortal.Common;
using MediaPortal.Common.Configuration.ConfigurationClasses;

namespace AirPlayer.MediaPortal2.Configuration
{
    public class PluginSettings
    {
        const int DEFAULT_RTSP_PORT = 50500;
        const int DEFAULT_UDP_PORT = 50510;
        const int DEFAULT_HTTP_PORT = 60500;

        const double DEFAULT_AUDIO_BUFFER = 2;
        const int DEFAULT_VIDEO_BUFFER = 2;

        string serverName;
        [Setting(SettingScope.User, null)]
        public string ServerName 
        {
            get { return serverName; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    serverName = System.Windows.Forms.SystemInformation.ComputerName;
                else
                    serverName = value;
            }
        }

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
    }

    public class ServerName : Entry
    {
        public override void Load()
        {
            _value = SettingsManager.Load<PluginSettings>().ServerName;
        }

        public override void Save()
        {
            PluginSettings settings = SettingsManager.Load<PluginSettings>();
            settings.ServerName = _value;
            SettingsManager.Save(settings);
        }

        public override int DisplayLength
        {
            get { return 20; }
        }
    }

    public class Password : Entry
    {
        public override void Load()
        {
            _value = SettingsManager.Load<PluginSettings>().Password;
        }

        public override void Save()
        {
            PluginSettings settings = SettingsManager.Load<PluginSettings>();
            settings.Password = _value;
            SettingsManager.Save(settings);
        }

        public override int DisplayLength
        {
            get { return 20; }
        }
    }

    public class DummyIdentifier : YesNo
    {
        public override void Load()
        {
            byte[] customAddress = SettingsManager.Load<PluginSettings>().CustomAddress;
            _yes = customAddress != null && customAddress.Length == 6;
        }

        public override void Save()
        {
            PluginSettings settings = SettingsManager.Load<PluginSettings>();
            if (_yes)
            {
                if (settings.CustomAddress == null || settings.CustomAddress.Length != 6)
                {
                    settings.CustomAddress = new byte[6];
                    new Random().NextBytes(settings.CustomAddress);
                }
            }
            else
            {
                settings.CustomAddress = null;
            }
            SettingsManager.Save(settings);
        }
    }

    #region AirTunes Config

    public class RtspPort : LimitedNumberSelect
    {
        public override void Load()
        {
            _type = NumberType.Integer;
            _step = 1;
            _lowerLimit = 1;
            _upperLimit = ushort.MaxValue;
            _value = SettingsManager.Load<PluginSettings>().RtspPort;
        }

        public override void Save()
        {
            PluginSettings settings = SettingsManager.Load<PluginSettings>();
            settings.RtspPort = (int)_value;
            SettingsManager.Save(settings);
        }
    }

    public class UdpPort : LimitedNumberSelect
    {
        public override void Load()
        {
            _type = NumberType.Integer;
            _step = 1;
            _lowerLimit = 1;
            _upperLimit = ushort.MaxValue;
            _value = SettingsManager.Load<PluginSettings>().UdpPort;
        }

        public override void Save()
        {
            PluginSettings settings = SettingsManager.Load<PluginSettings>();
            settings.UdpPort = (int)_value;
            SettingsManager.Save(settings);
        }
    }

    public class AudioBuffer : LimitedNumberSelect
    {
        public override void Load()
        {
            _type = NumberType.FloatingPoint;
            _step = 0.1;
            _lowerLimit = 0.1;
            _upperLimit = 10;
            _value = SettingsManager.Load<PluginSettings>().AudioBuffer;
        }

        public override void Save()
        {
            PluginSettings settings = SettingsManager.Load<PluginSettings>();
            settings.AudioBuffer = _value;
            SettingsManager.Save(settings);
        }
    }

    public class AllowVolume : YesNo
    {
        public override void Load()
        {
            _yes = SettingsManager.Load<PluginSettings>().AllowVolume;
        }

        public override void Save()
        {
            PluginSettings settings = SettingsManager.Load<PluginSettings>();
            settings.AllowVolume = _yes;
            SettingsManager.Save(settings);
        }
    }

    public class SendAudioCommands : YesNo
    {
        public override void Load()
        {
            _yes = SettingsManager.Load<PluginSettings>().SendAudioCommands;
        }

        public override void Save()
        {
            PluginSettings settings = SettingsManager.Load<PluginSettings>();
            settings.SendAudioCommands = _yes;
            SettingsManager.Save(settings);
        }
    }

    #endregion

    #region Airplay Config

    public class AirplayPort : LimitedNumberSelect
    {
        public override void Load()
        {
            _type = NumberType.Integer;
            _step = 1;
            _lowerLimit = 1;
            _upperLimit = ushort.MaxValue;
            _value = SettingsManager.Load<PluginSettings>().AirplayPort;
        }

        public override void Save()
        {
            PluginSettings settings = SettingsManager.Load<PluginSettings>();
            settings.AirplayPort = (int)_value;
            SettingsManager.Save(settings);
        }
    }

    public class VideoBuffer : LimitedNumberSelect
    {
        public override void Load()
        {
            _type = NumberType.FloatingPoint;
            _step = 0.1;
            _lowerLimit = 0.1;
            _upperLimit = 10;
            _value = SettingsManager.Load<PluginSettings>().VideoBuffer / 1000.0;
        }

        public override void Save()
        {
            PluginSettings settings = SettingsManager.Load<PluginSettings>();
            settings.VideoBuffer = (int)(_value * 1000);
            SettingsManager.Save(settings);
        }
    }

    public class AllowHDStreams : YesNo
    {
        public override void Load()
        {
            _yes = SettingsManager.Load<PluginSettings>().AllowHDStreams;
        }

        public override void Save()
        {
            PluginSettings settings = SettingsManager.Load<PluginSettings>();
            settings.AllowHDStreams = _yes;
            SettingsManager.Save(settings);
        }
    }

    #endregion
}
