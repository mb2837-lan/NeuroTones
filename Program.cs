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

    public class BrownNoiseProvider : WaveProvider32
    {
        private Random _rand = new Random();
        private float _lastOut = 0;

        public BrownNoiseProvider()
        {
            SetWaveFormat(44100, 1); // Mono
        }

        public override int Read(float[] buffer, int offset, int sampleCount)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                float white = (float)(_rand.NextDouble() * 2.0 - 1.0);
                _lastOut = (_lastOut + (0.02f * white)) / 1.02f;
                buffer[offset + i] = _lastOut * 3.5f;
            }
            return sampleCount;
        }
    }

    public class AudioEngine
    {
        private WaveOutEvent _waveOut;
        private IWaveProvider _provider;

        public void Start(float baseFreq, float beatFreq)
        {
            Stop();
            
            float left = baseFreq;
            float right = baseFreq + beatFreq;

            _provider = new BinauralWaveProvider(left, right);
            _waveOut = new WaveOutEvent();
            _waveOut.Init(_provider);
            _waveOut.Play();
        }

        public void StartBrownNoise()
        {
            Stop();
            
            _provider = new BrownNoiseProvider();
            _waveOut = new WaveOutEvent();
            _waveOut.Init(_provider);
            _waveOut.Play();
        }

        public void Stop()
        {
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _waveOut = null;
        }
    }
}
