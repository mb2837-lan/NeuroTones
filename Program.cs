using System;
using System.Windows.Forms;
using NAudio.Wave;

namespace BinauralBeats
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }

    public class BinauralWaveProvider : WaveProvider32
    {
        private float _frequencyLeft;
        private float _frequencyRight;
        private float _sample;
        private float _sampleRate;

        public BinauralWaveProvider(float frequencyLeft, float frequencyRight)
        {
            _frequencyLeft = frequencyLeft;
            _frequencyRight = frequencyRight;
            _sampleRate = 44100;
            SetWaveFormat((int)_sampleRate, 2);
        }

        public override int Read(float[] buffer, int offset, int sampleCount)
        {
            for (int n = 0; n < sampleCount; n += 2)
            {
                float left = (float)Math.Sin((2 * Math.PI * _frequencyLeft * _sample) / _sampleRate);
                float right = (float)Math.Sin((2 * Math.PI * _frequencyRight * _sample) / _sampleRate);
                buffer[offset + n] = left;
                buffer[offset + n + 1] = right;
                _sample++;
            }
            return sampleCount;
        }

        public void SetFrequencies(float left, float right)
        {
            _frequencyLeft = left;
            _frequencyRight = right;
        }
    }

    public class AudioEngine
    {
        private WaveOutEvent _waveOut;
        private BinauralWaveProvider _provider;

        public void Start(float baseFreq, float beatFreq)
        {
            Stop(); // Ensure previous playback is stopped before starting new one

            float left = baseFreq;
            float right = baseFreq + beatFreq;

            _provider = new BinauralWaveProvider(left, right);
            _waveOut = new WaveOutEvent();
            _waveOut.Init(_provider);
            _waveOut.Play();
        }

        public void Stop()
        {
            if (_waveOut != null)
            {
                try
                {
                    _waveOut.Stop();
                }
                catch
                {
                    // Log or ignore depending on your logging strategy
                }
                finally
                {
                    _waveOut.Dispose();
                    _waveOut = null;
                }
            }

            _provider = null;
        }
    }
}
