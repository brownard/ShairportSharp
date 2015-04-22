using AirPlayer.Common.DirectShow;
using DirectShow;
using DirectShow.Helper;
using MediaPortal.UI.Players.Video.Tools;
using ShairportSharp.Airplay;
using ShairportSharp.Audio;
using ShairportSharp.Mirroring;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ShairportSharp_Test
{
    class WaveStreamPlayer : StreamSourcePlayer
    {
        WaveStream source;
        public WaveStreamPlayer(WaveStream source)
            : base(source)
        {
            this.source = source;
        }

        protected override AMMediaType GetMediaType()
        {
            WaveHeader header = source.Header;
            WaveFormatEx w = new WaveFormatEx();
            w.wBitsPerSample = (ushort)header.BitsPerSample;
            w.cbSize = 0;
            w.nChannels = (ushort)header.Channels;
            w.nSamplesPerSec = header.SampleRate;
            w.wFormatTag = 1;
            int bytesPerSample = header.Channels * (header.BitsPerSample / 8);
            w.nAvgBytesPerSec = w.nSamplesPerSec * bytesPerSample;
            w.nBlockAlign = (ushort)bytesPerSample;

            AMMediaType amt = new AMMediaType();
            amt.majorType = MediaType.Audio;
            amt.subType = MediaSubType.PCM;
            amt.formatType = FormatType.WaveEx;
            amt.SetFormat(w);
            amt.fixedSizeSamples = true;
            amt.sampleSize = 4;
            
            return amt;
        }

        public override double CurrentPosition
        {
            get
            {
                uint currentTimeStamp = source.CurrentTimestamp;
                double position;
                lock (positionLock)
                    position = currentTimeStamp < startStamp ? 0 : (currentTimeStamp - startStamp) / 44100.0;

                return position;
            }
        }
    }

    abstract class StreamSourcePlayer : IPlayer
    {
        // DirectShow objects
        protected IGraphBuilder _graphBuilder;
        protected DsROTEntry _rot;

        Stream source;
        protected uint startStamp;
        protected uint stopStamp;
        protected double duration;
        protected object positionLock = new object();

        public StreamSourcePlayer(Stream source)
        {
            this.source = source;
        }

        ~StreamSourcePlayer()
        {
            Dispose();
        }

        public void Start()
        {
            _graphBuilder = (IFilterGraph2)new FilterGraph();
            _rot = new DsROTEntry(_graphBuilder);

            var sourceFilter = new GenericPushSourceFilter(source, GetMediaType());
            int hr = _graphBuilder.AddFilter(sourceFilter, sourceFilter.Name);
            new HRESULT(hr).Throw();
            AddFilters();
            DSFilter source2 = new DSFilter(sourceFilter);
            hr = source2.OutputPin.Render();
            new HRESULT(hr).Throw();

            IMediaControl mc = (IMediaControl)_graphBuilder;
            hr = mc.Run();
            new HRESULT(hr).Throw();
        }

        protected abstract AMMediaType GetMediaType();
        protected virtual void AddFilters() { }

        public void Stop()
        {
            if (_graphBuilder != null)
            {
                try
                {
                    FilterState state;
                    IMediaControl mc = (IMediaControl)_graphBuilder;
                    mc.GetState(10, out state);
                    if (state != FilterState.Stopped)
                    {
                        mc.Stop();
                    }
                }
                catch { }
            }
        }

        public void UpdateDurationInfo(uint startStamp, uint stopStamp)
        {
            lock (positionLock)
            {
                this.startStamp = startStamp;
                this.stopStamp = stopStamp;
                duration = (stopStamp - startStamp) / 44100.0;
            }
        }

        public double Duration
        {
            get
            {
                lock (positionLock)
                    return duration;
            }
        }

        public virtual double CurrentPosition
        {
            get { return 0; }
        }

        public void Dispose()
        {
            Stop();
            if (_rot != null)
            {
                try
                {
                    _rot.Dispose();
                }
                catch { }
                _rot = null;
            }
            if (_graphBuilder != null)
            {
                Marshal.ReleaseComObject(_graphBuilder);
                _graphBuilder = null;
            }
        }


        public void SetVolume(double volume)
        {
            IBasicAudio audio = _graphBuilder as IBasicAudio;
            if (audio != null)
            {
                int iVolume;
                if (volume <= -100)
                    iVolume = -10000;
                else
                    iVolume = (int)(volume * 100);
                audio.put_Volume(iVolume);
            }
        }
    }
}