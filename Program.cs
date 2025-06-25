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

        public float Volume
        {
            get => _waveOut?.Volume ?? 1.0f;
            set
            {
                if (_waveOut != null)
                    _waveOut.Volume = value;
            }
        }

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

    public partial class Form1 : Form
    {
        private TabControl _tabControl;
        private TabPage _binauralTab;
        private TabPage _noiseTab;

        private NumericUpDown _baseFreqInput;
        private NumericUpDown _beatFreqInput;
        private Button _presetAlphaButton;
        private Button _presetThetaButton;

        private Button _brownNoiseButton;
        private Button _startButton;
        private Button _stopButton;
        private TrackBar _volumeSlider;

        private AudioEngine _engine;

        public Form1()
        {
            _engine = new AudioEngine();

            Text = "NeuroTones";
            MinimumSize = new System.Drawing.Size(360, 460);

            _tabControl = new TabControl
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Location = new System.Drawing.Point(0, 40),
                Size = new System.Drawing.Size(ClientSize.Width, ClientSize.Height - 130)
            };

            _binauralTab = new TabPage("Binaural Beats");
            _noiseTab = new TabPage("Noise");

            InitializeBinauralTab();
            InitializeNoiseTab();

            Icon = new Icon("neurotones.ico");

            _tabControl.TabPages.Add(_binauralTab);
            _tabControl.TabPages.Add(_noiseTab);

            _startButton = new Button
            {
                Text = "Start",
                Location = new System.Drawing.Point(10, ClientSize.Height - 80),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };

            _stopButton = new Button
            {
                Text = "Stop",
                Location = new System.Drawing.Point(100, ClientSize.Height - 80),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };

            _volumeSlider = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 100,
                TickFrequency = 10,
                LargeChange = 10,
                SmallChange = 1,
                Orientation = Orientation.Horizontal,
                Location = new System.Drawing.Point(10, ClientSize.Height - 40),
                Width = ClientSize.Width - 20,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            _volumeSlider.Scroll += (s, e) => _engine.Volume = _volumeSlider.Value / 100f;

            _startButton.Click += (s, e) =>
            {
                if (_tabControl.SelectedTab == _binauralTab)
                {
                    _engine.Start((float)_baseFreqInput.Value, (float)_beatFreqInput.Value);
                }
                else if (_tabControl.SelectedTab == _noiseTab)
                {
                    _engine.StartBrownNoise();
                }
            };

            _stopButton.Click += (s, e) => _engine.Stop();

            Controls.Add(_tabControl);
            Controls.Add(_startButton);
            Controls.Add(_stopButton);
            Controls.Add(_volumeSlider);

            Resize += (s, e) =>
            {
                _tabControl.Size = new System.Drawing.Size(ClientSize.Width, ClientSize.Height - 130);
                _startButton.Location = new System.Drawing.Point(10, ClientSize.Height - 80);
                _stopButton.Location = new System.Drawing.Point(100, ClientSize.Height - 80);
                _volumeSlider.Location = new System.Drawing.Point(10, ClientSize.Height - 40);
                _volumeSlider.Width = ClientSize.Width - 20;
            };
        }

        private void InitializeBinauralTab()
        {
            _baseFreqInput = new NumericUpDown { Minimum = 100, Maximum = 1000, Value = 200, Location = new System.Drawing.Point(10, 10) };
            _beatFreqInput = new NumericUpDown { Minimum = 1, Maximum = 30, Value = 7, Location = new System.Drawing.Point(10, 40) };

            _presetAlphaButton = new Button { Text = "Alpha (10Hz)", Location = new System.Drawing.Point(10, 80) };
            _presetThetaButton = new Button { Text = "Theta (6Hz)", Location = new System.Drawing.Point(120, 80) };

            _presetAlphaButton.Click += (s, e) =>
            {
                _baseFreqInput.Value = 200;
                _beatFreqInput.Value = 10;
                _engine.Start(200, 10);
            };
            _presetThetaButton.Click += (s, e) =>
            {
                _baseFreqInput.Value = 200;
                _beatFreqInput.Value = 6;
                _engine.Start(200, 6);
            };

            _binauralTab.Controls.Add(_baseFreqInput);
            _binauralTab.Controls.Add(_beatFreqInput);
            _binauralTab.Controls.Add(_presetAlphaButton);
            _binauralTab.Controls.Add(_presetThetaButton);
        }

        private void InitializeNoiseTab()
        {
            _brownNoiseButton = new Button { Text = "Brown Noise", Location = new System.Drawing.Point(10, 10) };
            _brownNoiseButton.Click += (s, e) => _engine.StartBrownNoise();
            _noiseTab.Controls.Add(_brownNoiseButton);
        }
    }
}
