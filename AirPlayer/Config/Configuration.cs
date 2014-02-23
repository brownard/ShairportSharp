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
            rtspPortUpDown.Value = pluginSettings.RtspPort;
            udpPortUpDown.Value = pluginSettings.UdpPort;
            bufferSizeUpDown.Value = pluginSettings.BufferSize;
            allowVolumeCheckBox.Checked = pluginSettings.AllowVolume;
            sendCommandCheckBox.Checked = pluginSettings.SendCommands;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            pluginSettings.ServerName = nameTextBox.Text;
            pluginSettings.Password = passwordTextBox.Text;
            pluginSettings.RtspPort = (int)rtspPortUpDown.Value;
            pluginSettings.UdpPort = (int)udpPortUpDown.Value;
            pluginSettings.BufferSize = bufferSizeUpDown.Value;
            pluginSettings.AllowVolume = allowVolumeCheckBox.Checked;
            pluginSettings.SendCommands = sendCommandCheckBox.Checked;
            pluginSettings.Save();
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
