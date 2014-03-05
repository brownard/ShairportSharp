using MediaPortal.GUI.Library;
using MediaPortal.Player;
using ShairportSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ShairportSharp.Audio;
using AirPlayer.Config;
using ShairportSharp.Remote;
using ShairportSharp.Raop;

namespace AirPlayer
{
    public class AirPlayer : IPlugin, ISetupForm
    {
        #region Consts

        const string PLUGIN_NAME = "AirPlayer";
        const string AIRPLAY_DUMMY_FILE = "AirPlayer_Audio_Stream.wav";
        const int SKIN_PROPERTIES_UPDATE_DELAY = 2000;

        #endregion

        #region Variables

        ShairportServer server = null;
        object streamLock = new object();

        Player currentPlayer;
        DmapData currentMeta = null;
        string currentCover = null;
        uint currentStartStamp;
        uint currentStopStamp;
        
        int coverNumber = 0;
        object coverLock = new object();

        bool allowVolumeControl;
        bool sendCommands;

        volatile bool isPlaying = false;

        #endregion

        #region IPlugin Members

        public void Start()
        {
            ShairportServer.SetLogger(Logger.Instance);
            PluginSettings settings = new PluginSettings();
            allowVolumeControl = settings.AllowVolume;
            sendCommands = settings.SendCommands;

            server = new ShairportServer(settings.ServerName, settings.Password);
            server.Port = settings.RtspPort;
            server.AudioPort = settings.UdpPort;
            server.AudioBufferSize = (int)(settings.BufferSize * 1000);
            server.StreamStopped += server_StreamStopped;
            server.StreamReady += server_StreamReady;
            server.PlaybackProgressChanged += server_PlaybackProgressChanged;
            server.MetaDataChanged += server_MetaDataChanged;
            server.ArtworkChanged += server_ArtworkChanged;
            if (allowVolumeControl)
                server.VolumeChanged += server_VolumeChanged;
            server.Start();

            g_Player.PlayBackStarted += g_Player_PlayBackStarted;
            g_Player.PlayBackChanged += g_Player_PlayBackChanged;
            g_Player.PlayBackStopped += g_Player_PlayBackChanged;
            if (sendCommands)
                GUIWindowManager.OnNewAction += GUIWindowManager_OnNewAction;
        }
        
        public void Stop()
        {
            if (server != null)
                server.Stop();
        }

        #endregion

        #region ISetupForm Members

        public string Author()
        {
            return "Brownard";
        }

        public bool CanEnable()
        {
            return true;
        }

        public bool DefaultEnabled()
        {
            return true;
        }

        public string Description()
        {
            return "An AirPlay server emulator";
        }

        public bool GetHome(out string strButtonText, out string strButtonImage, out string strButtonImageFocus, out string strPictureImage)
        {
            strButtonText = null;
            strButtonImage = null;
            strButtonImageFocus = null;
            strPictureImage = null;
            return true;
        }

        public int GetWindowId()
        {
            return -1;
        }

        public bool HasSetup()
        {
            return true;
        }

        public string PluginName()
        {
            return PLUGIN_NAME;
        }

        public void ShowPlugin()
        {
            new Configuration().ShowDialog();
        }

        #endregion

        #region Shairplay Event Handlers

        void server_StreamStopped(object sender, EventArgs e)
        {
            lock (streamLock)
            {
                if (isPlaying)
                {
                    currentPlayer = null;
                    GUIGraphicsContext.form.BeginInvoke((MethodInvoker)g_Player.Stop);
                }
            }
        }

        void server_StreamReady(object sender, EventArgs e)
        {
            AudioBufferStream input = server.GetStream(StreamType.Wave);
            if (input == null)
                return;

            GUIGraphicsContext.form.BeginInvoke((MethodInvoker)delegate() { startPlayback(input); });
        }

        void startPlayback(AudioBufferStream stream)
        {
            if (g_Player.Playing)
                g_Player.Stop();

            IPlayerFactory savedFactory = g_Player.Factory;
            lock (streamLock)
            {
                currentPlayer = new Player(new PlayerSettings(sendCommands ? server : null, stream));
                g_Player.Factory = new PlayerFactory(currentPlayer);
            }
            g_Player.Play(AIRPLAY_DUMMY_FILE, g_Player.MediaType.Music);
            g_Player.Factory = savedFactory;
            
            ThreadPool.QueueUserWorkItem((o) =>
            {
                Thread.Sleep(SKIN_PROPERTIES_UPDATE_DELAY);
                lock (streamLock)
                {
                    setMetaData(currentMeta);
                    setCover(currentCover);
                    setDuration();
                }
            });
        }

        void server_PlaybackProgressChanged(object sender, PlaybackProgressChangedEventArgs e)
        {
            lock (streamLock)
            {
                currentStartStamp = e.Start;
                currentStopStamp = e.Stop;
                setDuration();
            }
        }

        void setDuration()
        {
            if (isPlaying && currentPlayer != null)
                currentPlayer.UpdateDurationInfo(currentStartStamp, currentStopStamp);
        }

        void server_MetaDataChanged(object sender, MetaDataChangedEventArgs e)
        {
            lock (streamLock)
            {
                currentMeta = e.MetaData;
                setMetaData(currentMeta);
            }
        }

        void setMetaData(DmapData metaData)
        {
            if (isPlaying && metaData != null)
            {
                GUIPropertyManager.SetProperty("#Play.Current.Title", metaData.Track);
                GUIPropertyManager.SetProperty("#Play.Current.Album", metaData.Album);
                GUIPropertyManager.SetProperty("#Play.Current.Artist", metaData.Artist);
                GUIPropertyManager.SetProperty("#Play.Current.Genre", metaData.Genre);
            }
        }

        void server_ArtworkChanged(object sender, ArtwokChangedEventArgs e)
        {
            string newCover = saveImage(e.ImageData, e.ContentType);
            lock (streamLock)
            {
                currentCover = newCover;
                if (currentCover != null)
                    setCover(currentCover);
            }
        }

        void setCover(string cover)
        {
            if (isPlaying && !string.IsNullOrEmpty(cover))
                GUIPropertyManager.SetProperty("#Play.Current.Thumb", cover);
        }

        void server_VolumeChanged(object sender, VolumeChangedEventArgs e)
        {
            lock (streamLock)
            {
                if (isPlaying)
                {
                    VolumeHandler volumeHandler = VolumeHandler.Instance;
                    if (e.Volume < -30)
                    {
                        volumeHandler.IsMuted = true;
                    }
                    else
                    {
                        double percent = (e.Volume + 30) / 30;
                        volumeHandler.Volume = (int)(volumeHandler.Maximum * percent);
                    }
                }
            }
        }

        #endregion

        #region Mediaportal Event Handlers

        void GUIWindowManager_OnNewAction(MediaPortal.GUI.Library.Action action)
        {
            if (isPlaying)
            {
                switch (action.wID)
                {
                    case MediaPortal.GUI.Library.Action.ActionType.ACTION_PREV_CHAPTER:
                    case MediaPortal.GUI.Library.Action.ActionType.ACTION_PREV_ITEM:
                        server.SendCommand(RemoteCommand.PrevItem);
                        break;
                    case MediaPortal.GUI.Library.Action.ActionType.ACTION_NEXT_CHAPTER:
                    case MediaPortal.GUI.Library.Action.ActionType.ACTION_NEXT_ITEM:
                        server.SendCommand(RemoteCommand.NextItem);
                        break;
                }
            }
        }

        void g_Player_PlayBackChanged(g_Player.MediaType type, int stoptime, string filename)
        {
            if (type != g_Player.MediaType.Music || filename != AIRPLAY_DUMMY_FILE)
                return;

            lock (streamLock)
            {
                isPlaying = false;
                currentPlayer = null;
                server.StopCurrentSession();
            }
        }

        void g_Player_PlayBackStarted(g_Player.MediaType type, string filename)
        {
            if (type != g_Player.MediaType.Music || filename != AIRPLAY_DUMMY_FILE)
                return;

            isPlaying = true;
        }

        #endregion

        #region Utils

        string saveImage(byte[] buffer, string contentType)
        {
            lock (coverLock)
            {
                coverNumber = coverNumber % 3;
                string extension = contentType.Replace("image/", "");
                if (extension == "jpeg")
                    extension = "jpg";
                                
                try
                {
                    string path = Path.Combine(Path.GetTempPath(), string.Format("AirPlay_Thumb_{0}.{1}", coverNumber++, extension));
                    Logger.Instance.Debug("Saving cover art to '{0}'", path);
                    using (FileStream fs = File.Create(path))
                        fs.Write(buffer, 0, buffer.Length);
                    return path;
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error("Failed to save received cover art - {0}", ex.Message);
                }
            }
            return null;
        }

        #endregion
    }
}
