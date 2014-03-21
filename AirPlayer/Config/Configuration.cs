using MediaPortal.Profile;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AirPlayer.Config
{
    public partial class Configuration : Form
    {
        PluginSettings pluginSettings;
        public Configuration()
        {
            InitializeComponent();
            pluginSettings = new PluginSettings();
            nameTextBox.Text = pluginSettings.ServerName;
            passwordTextBox.Text = pluginSettings.Password;
            //Audio
            rtspPortUpDown.Value = pluginSettings.RtspPort;
            udpPortUpDown.Value = pluginSettings.UdpPort;
            audioBufferUpDown.Value = pluginSettings.AudioBuffer;
            allowVolumeCheckBox.Checked = pluginSettings.AllowVolume;
            sendCommandCheckBox.Checked = pluginSettings.SendCommands;
            //Photo/Video
            httpPortUpDown.Value = pluginSettings.AirplayPort;
            videoBufferUpDown.Value = pluginSettings.VideoBuffer;
            allowHDCheckBox.Checked = pluginSettings.AllowHDStreams;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            pluginSettings.ServerName = nameTextBox.Text;
            pluginSettings.Password = passwordTextBox.Text;
            pluginSettings.RtspPort = (int)rtspPortUpDown.Value;
            pluginSettings.UdpPort = (int)udpPortUpDown.Value;
            pluginSettings.AudioBuffer = audioBufferUpDown.Value;
            pluginSettings.AllowVolume = allowVolumeCheckBox.Checked;
            pluginSettings.SendCommands = sendCommandCheckBox.Checked;

            pluginSettings.AirplayPort = (int)httpPortUpDown.Value;
            pluginSettings.VideoBuffer = (int)videoBufferUpDown.Value;
            pluginSettings.AllowHDStreams = allowHDCheckBox.Checked;

            pluginSettings.Save();
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
