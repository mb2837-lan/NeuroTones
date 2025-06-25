namespace BinauralBeats
{
    public partial class Form1 : Form
    {
        private NumericUpDown _baseFreqInput;
        private NumericUpDown _beatFreqInput;
        private Button _startButton;
        private Button _stopButton;
        private AudioEngine _engine;

        public Form1()
        {
            _engine = new AudioEngine();

            Text = "Binaural Beats";
            Width = 300;
            Height = 200;

            _baseFreqInput = new NumericUpDown { Minimum = 100, Maximum = 1000, Value = 200, Location = new System.Drawing.Point(10, 10) };
            _beatFreqInput = new NumericUpDown { Minimum = 1, Maximum = 30, Value = 7, Location = new System.Drawing.Point(10, 40) };

            _startButton = new Button { Text = "Start", Location = new System.Drawing.Point(10, 80) };
            _stopButton = new Button { Text = "Stop", Location = new System.Drawing.Point(100, 80) };

            _startButton.Click += (s, e) => _engine.Start((float)_baseFreqInput.Value, (float)_beatFreqInput.Value);
            _stopButton.Click += (s, e) => _engine.Stop();

            Controls.Add(_baseFreqInput);
            Controls.Add(_beatFreqInput);
            Controls.Add(_startButton);
            Controls.Add(_stopButton);
        }
    }
}
