using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using NAudio.Wave;

namespace Clickboard
{
    public class AudioPlayerForm : Form
    {
        private IWavePlayer waveOut;
        private WaveStream audioStream;
        private TrackBar seekBar;
        private TrackBar volumeBar;
        private Button playPauseButton;
        private Button skipButton;
        private Button closeButton;
        private Label timeLabel;
        private Timer updateTimer;
        private bool isPlaying = false;
        private bool isDragging = false;
        private byte[] audioData;
        private string audioFormat;
        private Theme theme;
        private Point lastPoint;

        public AudioPlayerForm(byte[] audioData, string audioFormat, Theme theme)
        {
            this.audioData = audioData;
            this.audioFormat = audioFormat;
            this.theme = theme;
            this.StartPosition = FormStartPosition.Manual;
            LoadLastLocationOrCenter();
            InitializeUI();
            LoadAudio();
        }

        private void LoadLastLocationOrCenter()
        {
            string settingsPath = Path.Combine(Application.StartupPath, "audioplayer.loc");
            if (File.Exists(settingsPath))
            {
                var parts = File.ReadAllText(settingsPath).Split(',');
                if (parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
                {
                    this.Location = new Point(x, y);
                    return;
                }
            }
            var mainWin = Application.OpenForms[0];
            if (mainWin != null)
            {
                int centerX = mainWin.Location.X + (mainWin.Width - this.Width) / 2;
                int centerY = mainWin.Location.Y + (mainWin.Height - this.Height) / 2;
                this.Location = new Point(centerX, centerY);
            }
            else
            {
                var screen = Screen.PrimaryScreen.WorkingArea;
                int centerX = screen.Left + (screen.Width - this.Width) / 2;
                int centerY = screen.Top + (screen.Height - this.Height) / 2;
                this.Location = new Point(centerX, centerY);
            }
        }

        private void InitializeUI()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Width = 400;
            this.Height = 140;
            this.BackColor = theme?.HeaderBarColor ?? Color.Black;
            this.ForeColor = theme?.ButtonTextColor ?? Color.White;
            this.MouseDown += AudioPlayerForm_MouseDown;
            this.MouseMove += AudioPlayerForm_MouseMove;
            this.MouseUp += AudioPlayerForm_MouseUp;

            playPauseButton = new Button
            {
                Text = ">", 
                Width = 40,
                Height = 40,
                Location = new Point(20, 40),
                BackColor = theme?.ButtonColor ?? Color.Gray,
                ForeColor = theme?.ButtonTextColor ?? Color.White,
                FlatStyle = FlatStyle.Flat
            };
            playPauseButton.FlatAppearance.BorderSize = 0;
            playPauseButton.Click += PlayPauseButton_Click;
            this.Controls.Add(playPauseButton);

            skipButton = new Button
            {
                Text = "+10s",
                Width = 50,
                Height = 40,
                Location = new Point(70, 40),
                BackColor = theme?.ButtonColor ?? Color.Gray,
                ForeColor = theme?.ButtonTextColor ?? Color.White,
                FlatStyle = FlatStyle.Flat
            };
            skipButton.FlatAppearance.BorderSize = 0;
            skipButton.Click += SkipButton_Click;
            this.Controls.Add(skipButton);

            seekBar = new TrackBar
            {
                Width = 140, 
                Height = 20, 
                Location = new Point(130, 50),
                Minimum = 0,
                Maximum = 1000,
                TickStyle = TickStyle.None
            };
            seekBar.MouseDown += (s, e) => isDragging = true;
            seekBar.MouseUp += (s, e) => { isDragging = false; SeekAudio(); };
            seekBar.Scroll += (s, e) => { if (isDragging) SeekAudio(); };
            this.Controls.Add(seekBar);

            volumeBar = new TrackBar
            {
                Width = 80,
                Height = 30,
                Location = new Point(130, 90),
                Minimum = 0,
                Maximum = 100,
                Value = 30, 
                TickStyle = TickStyle.None
            };
            volumeBar.Scroll += (s, e) => SetVolume();
            this.Controls.Add(volumeBar);

            timeLabel = new Label
            {
                Text = "00:00 / 00:00",
                Width = 120,
                Height = 20,
                Location = new Point(320, 50),
                ForeColor = theme?.ButtonTextColor ?? Color.White
            };
            this.Controls.Add(timeLabel);

            updateTimer = new Timer { Interval = 200 };
            updateTimer.Tick += UpdateTimer_Tick;

            this.BringToFront();
        }

        private void LoadAudio()
        {
            if (audioStream != null)
            {
                audioStream.Dispose();
                audioStream = null;
            }
            if (waveOut != null)
            {
                waveOut.Dispose();
                waveOut = null;
            }
            var ms = new MemoryStream(audioData);
            if (audioFormat == ".mp3")
                audioStream = new Mp3FileReader(ms);
            else if (audioFormat == ".wav")
                audioStream = new WaveFileReader(ms);
            else
                throw new NotSupportedException("Unsupported audio format");
            waveOut = new WaveOutEvent();
            waveOut.Init(audioStream);
            SetVolume();
            updateTimer.Start();
        }

        private void PlayPauseButton_Click(object sender, EventArgs e)
        {
            if (isPlaying)
            {
                waveOut.Pause();
                playPauseButton.Text = ">";
                isPlaying = false;
            }
            else
            {
                waveOut.Play();
                playPauseButton.Text = "||";
                isPlaying = true;
            }
        }

        private void SkipButton_Click(object sender, EventArgs e)
        {
            if (audioStream != null)
            {
                var newPos = audioStream.CurrentTime.Add(TimeSpan.FromSeconds(10));
                if (newPos > audioStream.TotalTime)
                    newPos = audioStream.TotalTime;
                audioStream.CurrentTime = newPos;
            }
        }

        private void SeekAudio()
        {
            if (audioStream != null)
            {
                double percent = seekBar.Value / (double)seekBar.Maximum;
                var newPos = TimeSpan.FromSeconds(audioStream.TotalTime.TotalSeconds * percent);
                audioStream.CurrentTime = newPos;
            }
        }

        private void SetVolume()
        {
            if (waveOut != null)
                waveOut.Volume = volumeBar.Value / 100f;
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (audioStream == null) return;
            if (!isDragging)
            {
                double percent = audioStream.CurrentTime.TotalSeconds / audioStream.TotalTime.TotalSeconds;
                seekBar.Value = Math.Min(seekBar.Maximum, (int)(percent * seekBar.Maximum));
            }
            timeLabel.Text = $"{audioStream.CurrentTime:mm\\:ss} / {audioStream.TotalTime:mm\\:ss}";
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            this.BringToFront();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (waveOut != null) waveOut.Dispose();
            if (audioStream != null) audioStream.Dispose();
            updateTimer.Stop();
            string settingsPath = Path.Combine(Application.StartupPath, "audioplayer.loc");
            File.WriteAllText(settingsPath, $"{this.Location.X},{this.Location.Y}");
        }

        private void InitializeCloseButton()
        {
            closeButton = new Button
            {
                Text = "X",
                Width = 32,
                Height = 24,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Red,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                TabStop = false,
                Top = 8,
                Left = this.ClientSize.Width - 40,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += (s, e) => this.Close();
            this.Controls.Add(closeButton);
            closeButton.BringToFront();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            InitializeCloseButton();
        }

  
        private void AudioPlayerForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                lastPoint = new Point(e.X, e.Y);
        }
        private void AudioPlayerForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Left += e.X - lastPoint.X;
                this.Top += e.Y - lastPoint.Y;
            }
        }
        private void AudioPlayerForm_MouseUp(object sender, MouseEventArgs e)
        {
          
        }

        public static void SaveMainWindowLocation(Point location)
        {
            string settingsPath = Path.Combine(Application.StartupPath, "mainwindow.loc");
            File.WriteAllText(settingsPath, $"{location.X},{location.Y}");
        }

        public static Point? LoadMainWindowLocation(Size windowSize)
        {
            string settingsPath = Path.Combine(Application.StartupPath, "mainwindow.loc");
            if (File.Exists(settingsPath))
            {
                var parts = File.ReadAllText(settingsPath).Split(',');
                if (parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
                {
                    return new Point(x, y);
                }
            }
            var screen = Screen.PrimaryScreen.WorkingArea;
            int centerX = screen.Left + (screen.Width - windowSize.Width) / 2;
            int centerY = screen.Top + (screen.Height - windowSize.Height) / 2;
            return new Point(centerX, centerY);
        }
    }
}
