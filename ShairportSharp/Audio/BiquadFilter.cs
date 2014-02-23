using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Audio
{
    /// <summary>
    /// A low pass filter to calculate the average drift of the audio renderer based on the change in buffer size. 
    /// </summary>
    class BiquadFilter
    {
        const int START_OFFSET = 128; 
        const int INITIAL_SAMPLES = 1000;
        const double CONTROL_A = (1e-4);
        const double CONTROL_B = (1e-1);
        
        double estDrift = 0.0; //local clock is slower by
        BiquadLPF driftFilter;
        double estError = 0.0;
        BiquadLPF errorFilter;
        double lastError;
        BiquadLPF errorDerivFilter;

        double desiredFill;
        int fillCount;
        int initialFill;
        int samplingRate;
        int frameSize;

        double playbackRate = 1.0;
        public double PlaybackRate
        {
            get
            {
                return playbackRate;
            }
        }

        public BiquadFilter(int samplingRate, int frameSize)
        {
            this.samplingRate = samplingRate;
            this.frameSize = frameSize;

            driftFilter = createLPF(1.0 / 180.0, 0.3);
            errorFilter = createLPF(1.0 / 10.0, 0.25);
            errorDerivFilter = createLPF(1.0 / 2.0, 0.2);

            playbackRate = 1.0;
            estError = 0;
            lastError = 0;
            desiredFill = 0;
            fillCount = 0;
            initialFill = START_OFFSET + INITIAL_SAMPLES;
        }

        public void Update(int fill)
        {
            if (fillCount < START_OFFSET)
            {
                fillCount++;
                return;
            }
            if (fillCount < initialFill)
            {
                desiredFill += fill / (double)INITIAL_SAMPLES;
                fillCount++;
                return;
            }
            double bufDelta = fill - desiredFill;
            estError = filter(errorFilter, bufDelta);
            double errDeriv = filter(errorDerivFilter, estError - lastError);
            double errAdj = estError * CONTROL_A;
            estDrift = filter(driftFilter, CONTROL_B * (errAdj + errDeriv) + estDrift);
            playbackRate = 1.0 + errAdj + estDrift;
            lastError = estError;
            //Logger.Debug("Playback rate: {0}", playbackRate);
        }

        BiquadLPF createLPF(double freq, double Q)
        {
            BiquadLPF ret = new BiquadLPF();

            double w0 = 2 * Math.PI * freq * frameSize / (double)samplingRate;
            double alpha = Math.Sin(w0) / (2.0 * Q);

            double a_0 = 1.0 + alpha;
            ret.b[0] = (1.0 - Math.Cos(w0)) / (2.0 * a_0);
            ret.b[1] = (1.0 - Math.Cos(w0)) / a_0;
            ret.b[2] = ret.b[0];
            ret.a[0] = -2.0 * Math.Cos(w0) / a_0;
            ret.a[1] = (1 - alpha) / a_0;

            return ret;
        }

        double filter(BiquadLPF bq, double input)
        {
            double w = input - bq.a[0] * bq.hist[0] - bq.a[1] * bq.hist[1];
            double output = bq.b[1] * bq.hist[0] + bq.b[2] * bq.hist[1] + bq.b[0] * w;
            bq.hist[1] = bq.hist[0];
            bq.hist[0] = w;
            return output;
        }
    }

    public class BiquadLPF
    {
        public double[] hist = new double[2];
        public double[] a = new double[2];
        public double[] b = new double[3];
    }
}
