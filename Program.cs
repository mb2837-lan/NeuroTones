using System;
using System.IO;
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

    public class WhiteNoiseProvider : WaveProvider32
    {
        private Random _rand = new Random();

        public WhiteNoiseProvider()
        {
            SetWaveFormat(44100, 1); // Mono
        }

        public override int Read(float[] buffer, int offset, int sampleCount)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                buffer[offset + i] = (float)(_rand.NextDouble() * 2.0 - 1.0);
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

        public void Start(float baseFreq, float beatFreq, bool playWhiteNoise = false)
        {
            Stop();

            float left = baseFreq;
            float right = baseFreq + beatFreq;

            _provider = new BinauralWaveProvider(left, right);
            _waveOut = new WaveOutEvent();
            _waveOut.Init(_provider);
            _waveOut.Play();

            if (playWhiteNoise)
            {
                var whiteNoiseProvider = new WhiteNoiseProvider();
                var whiteNoiseWaveOut = new WaveOutEvent();
                whiteNoiseWaveOut.Init(whiteNoiseProvider);
                whiteNoiseWaveOut.Play();
            }
        }

        public void StartBrownNoise()
        {
            Stop();

            _provider = new BrownNoiseProvider();
            _waveOut = new WaveOutEvent();
            _waveOut.Init(_provider);
            _waveOut.Play();
        }

        public void StartWhiteNoise()
        {
            Stop();
            
            _provider = new WhiteNoiseProvider();
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

        public void PlayMriSound()
        {
            Stop();

            var player = new System.Media.SoundPlayer();
            var mriPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mri.wav");
            if (File.Exists(mriPath))
            {
                player.SoundLocation = mriPath;
                player.Load();
                player.PlayLooping();
            }
            else
            {
                MessageBox.Show("MRI sound file not found: mri.wav", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public partial class Form1 : Form
    {
        private TabControl _tabControl;
        private TabPage _binauralTab;
        private TabPage _noiseTab;
        private TabPage _whiteNoiseTab;
        private NumericUpDown _baseFreqInput;
        private NumericUpDown _beatFreqInput;
        private Button _presetAlphaButton;
        private Button _presetThetaButton;
        private Button _brownNoiseButton;
        private Button _whiteNoiseButton;
        private Button _startButton;
        private Button _stopButton;
        private TrackBar _volumeSlider;
        private ComboBox _durationSelector;
        private Label _timerLabel;
        private System.Windows.Forms.Timer _playbackTimer;
        private TimeSpan _remainingTime;
        private CheckBox _playWhiteNoiseCheckbox;

        private AudioEngine _engine;

        public Form1()
        {
            _engine = new AudioEngine();

            Text = "Neuro Tones";
            Size = new System.Drawing.Size(800, 600);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            _tabControl = new TabControl
            {
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(500, 400)
            };

            _binauralTab = new TabPage("Binaural Beats");
            _noiseTab = new TabPage("Bown Noise");
            _whiteNoiseTab = new TabPage("White Noise");
            var _mriTab = new TabPage("MRI");

            InitializeBinauralTab();
            InitializeNoiseTab();
            InitializeWhiteNoiseTab();
            InitializeMriTab(_mriTab);

            Icon = new Icon("neurotones.ico");

            _tabControl.TabPages.Add(_binauralTab);
            _tabControl.TabPages.Add(_noiseTab);
            _tabControl.TabPages.Add(_whiteNoiseTab);
            _tabControl.TabPages.Add(_mriTab);

            _startButton = new Button
            {
                Text = "Start",
                Location = new System.Drawing.Point(530, 50),
                Size = new System.Drawing.Size(100, 30)
            };

            _stopButton = new Button
            {
                Text = "Stop",
                Location = new System.Drawing.Point(640, 50),
                Size = new System.Drawing.Size(100, 30)
            };

            _volumeSlider = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 100,
                TickFrequency = 10,
                Orientation = Orientation.Horizontal,
                Location = new System.Drawing.Point(530, 100),
                Size = new System.Drawing.Size(210, 45)
            };

            _durationSelector = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new System.Drawing.Point(530, 160),
                Width = 60
            };
            for (int i = 1; i <= 60; i++)
            {
                _durationSelector.Items.Add(i);
            }
            _durationSelector.SelectedIndex = 14;

            _timerLabel = new Label
            {
                Text = "15:00",
                Location = new System.Drawing.Point(600, 163),
                AutoSize = true
            };

            _volumeSlider.Scroll += (s, e) => _engine.Volume = _volumeSlider.Value / 100f;

            _startButton.Click += (s, e) =>
            {
                int durationMinutes = (int)_durationSelector.SelectedItem;
                _remainingTime = TimeSpan.FromMinutes(durationMinutes);
                _timerLabel.Text = _remainingTime.ToString(@"mm\:ss");
                _playbackTimer.Start();

                if (_tabControl.SelectedTab == _binauralTab)
                {
                    _engine.Start((float)_baseFreqInput.Value, (float)_beatFreqInput.Value, _playWhiteNoiseCheckbox.Checked);
                }
                else if (_tabControl.SelectedTab == _noiseTab)
                {
                    _engine.StartBrownNoise();
                }
                else if (_tabControl.SelectedTab == _whiteNoiseTab)
                {
                    _engine.StartWhiteNoise();
                }
                else
                {
                    _engine.PlayMriSound();
                }
            };

            _stopButton.Click += (s, e) =>
            {
                _engine.Stop();
                _playbackTimer.Stop();
            };

            Controls.Add(_tabControl);
            Controls.Add(_startButton);
            Controls.Add(_stopButton);
            Controls.Add(_volumeSlider);
            Controls.Add(_durationSelector);
            Controls.Add(_timerLabel);

            _playbackTimer = new System.Windows.Forms.Timer();
            _playbackTimer.Interval = 1000;
            _playbackTimer.Tick += (s, e) =>
            {
                _remainingTime = _remainingTime.Subtract(TimeSpan.FromSeconds(1));
                _timerLabel.Text = _remainingTime.ToString(@"mm\:ss");
                if (_remainingTime.TotalSeconds <= 0)
                {
                    _engine.Stop();
                    _playbackTimer.Stop();
                }
            };
        }

        private void InitializeBinauralTab()
        {
            _baseFreqInput = new NumericUpDown { Minimum = 100, Maximum = 1000, Value = 220, Location = new System.Drawing.Point(10, 10) };
            _beatFreqInput = new NumericUpDown { Minimum = 1, Maximum = 30, Value = 12, Location = new System.Drawing.Point(10, 40) };

            _presetAlphaButton = new Button { Text = "Alpha (10Hz)", Location = new System.Drawing.Point(10, 80) };
            _presetThetaButton = new Button { Text = "Theta (6Hz)", Location = new System.Drawing.Point(120, 80) };

            _playWhiteNoiseCheckbox = new CheckBox
            {
                Text = "Play White Noise Simultaneously",
                Location = new System.Drawing.Point(10, 120),
                AutoSize = true
            };

            _presetAlphaButton.Click += (s, e) =>
            {
                _baseFreqInput.Value = 200;
                _beatFreqInput.Value = 10;
                _engine.Start(200, 10);
                if (_playWhiteNoiseCheckbox.Checked)
                {
                    _engine.StartWhiteNoise();
                }
            };
            _presetThetaButton.Click += (s, e) =>
            {
                _baseFreqInput.Value = 200;
                _beatFreqInput.Value = 6;
                _engine.Start(200, 6);
                if (_playWhiteNoiseCheckbox.Checked)
                {
                    _engine.StartWhiteNoise();
                }
            };

            _binauralTab.Controls.Add(_baseFreqInput);
            _binauralTab.Controls.Add(_beatFreqInput);
            _binauralTab.Controls.Add(_presetAlphaButton);
            _binauralTab.Controls.Add(_presetThetaButton);
            _binauralTab.Controls.Add(_playWhiteNoiseCheckbox);
        }

        private void InitializeNoiseTab()
        {
            _brownNoiseButton = new Button { Text = "Brown Noise", Location = new System.Drawing.Point(10, 10) };
            _brownNoiseButton.Click += (s, e) => _engine.StartBrownNoise();
            _noiseTab.Controls.Add(_brownNoiseButton);
        }

        private void InitializeWhiteNoiseTab()
        {
            _whiteNoiseButton = new Button { Text = "White Noise", Location = new System.Drawing.Point(10, 10) };
            _whiteNoiseButton.Click += (s, e) => _engine.StartWhiteNoise();
            _whiteNoiseTab.Controls.Add(_whiteNoiseButton);
        }

        private void InitializeMriTab(TabPage mriTab)
        {
            var mriButton = new Button { Text = "Play MRI Sound", Location = new System.Drawing.Point(10, 10) };
            mriButton.Click += (s, e) => _engine.PlayMriSound();
            mriTab.Controls.Add(mriButton);
        }
    }
}
