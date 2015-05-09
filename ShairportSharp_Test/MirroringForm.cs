using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ShairportSharp.Airplay;
using DirectShow.Helper;
using System.IO;
using DirectShow;
using System.Runtime.InteropServices;
using AirPlayer.Common.DirectShow;
using ShairportSharp.Mirroring;

namespace ShairportSharp_Test
{
    public partial class MirroringForm : Form
    {
        MirrorPlayer m_Playback = null;
        bool closing;

        public MirroringForm(MirroringStream stream)
        {
            InitializeComponent();
            m_Playback = new MirrorPlayer(stream);
            m_Playback.VideoControl = this.videoControl;
            m_Playback.OnPlaybackStop += m_Playback_OnPlaybackStop;
        }

        public void Start()
        {
            m_Playback.Start();
        }

        void m_Playback_OnPlaybackStop(object sender, EventArgs e)
        {
            BeginInvoke((MethodInvoker)delegate() 
            {
                if (!closing)
                    Close();
            });
        }

        private void VideoForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            closing = true;
            m_Playback.Dispose();
            m_Playback = null;
        }
    }
}
