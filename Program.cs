/* 
ScreamGuard

How to run:
> dotnet build
> dotnet run

How to publish:
> dotnet build
> dotnet publish -r win-x64 -p:PublishSingleFile=true --self-contained false
*/

using System;
using System.Drawing;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Diagnostics;

namespace ScreamGuard
{
    public class Program : Form
    {
        private Label lblWarningLevel;
        private TextBox txtWarningLevel;
        private Label lblAlarmLevel;
        private TextBox txtAlarmLevel;
        private Label lblMovingAverage; // Label for moving average
        private TextBox txtMovingAverage; // TextBox for moving average
        private Label lblSamplingRate; // Label for sampling rate
        private TextBox txtSamplingRate; // TextBox for sampling rate
        private Button btnClose;
        private Button btnStartMonitoring;
        private Button btnStopMonitoring;
        private ComboBox cboMicrophones; // Dropdown for microphones
        private TextBox txtOutput;
        private float warningLevel = 50f;
        private float alarmLevel = 70f;
        private int movingAveragePeriod = 20; // Default moving average period
        private int samplingRate = 100; // Default sampling rate
        private MMDeviceEnumerator enumerator;
        private MMDevice selectedMicrophone;
        private bool monitoring = false;
        private OverlayForm overlayForm; // Overlay form to show warning
        private OverlayForm alarmOverlayForm; // Overlay form to show alarm
        private const string SettingsFilePath = "screamguard_settings.json"; // Path to settings file

        public Program()
        {
            // Initialize UI components
            this.Text = "ScreamGuard"; // Set the title to ScreamGuard
            this.Size = new System.Drawing.Size(400, 380);
            this.FormBorderStyle = FormBorderStyle.FixedSingle; // Prevent window resizing
            this.Icon = new Icon("icon.ico"); // Set custom icon

            // Warning Level Label
            lblWarningLevel = new Label();
            lblWarningLevel.Text = "Warning Level (%):";
            lblWarningLevel.Location = new System.Drawing.Point(10, 20);
            lblWarningLevel.AutoSize = true;
            this.Controls.Add(lblWarningLevel);

            // Warning Level TextBox
            txtWarningLevel = new TextBox();
            txtWarningLevel.Text = "20"; // Default value
            txtWarningLevel.Location = new System.Drawing.Point(170, 20);
            this.Controls.Add(txtWarningLevel);

            // Alarm Level Label
            lblAlarmLevel = new Label();
            lblAlarmLevel.Text = "Alarm Level (%):";
            lblAlarmLevel.Location = new System.Drawing.Point(10, 60);
            lblAlarmLevel.AutoSize = true;
            this.Controls.Add(lblAlarmLevel);

            // Alarm Level TextBox
            txtAlarmLevel = new TextBox();
            txtAlarmLevel.Text = "30"; // Default value
            txtAlarmLevel.Location = new System.Drawing.Point(170, 60);
            this.Controls.Add(txtAlarmLevel);

            // Moving Average Label
            lblMovingAverage = new Label();
            lblMovingAverage.Text = "Moving Median (Samples):";
            lblMovingAverage.Location = new System.Drawing.Point(10, 100);
            lblMovingAverage.AutoSize = true;
            this.Controls.Add(lblMovingAverage);

            // Moving Average TextBox
            txtMovingAverage = new TextBox();
            txtMovingAverage.Text = "20"; // Default value for moving average
            txtMovingAverage.Location = new System.Drawing.Point(170, 100);
            this.Controls.Add(txtMovingAverage);

            // Sampling Rate Label
            lblSamplingRate = new Label();
            lblSamplingRate.Text = "Sampling every (ms):";
            lblSamplingRate.Location = new System.Drawing.Point(10, 140);
            lblSamplingRate.AutoSize = true;
            this.Controls.Add(lblSamplingRate);

            // Sampling Rate TextBox
            txtSamplingRate = new TextBox();
            txtSamplingRate.Text = "100"; // Default value for sampling rate
            txtSamplingRate.Location = new System.Drawing.Point(170, 140);
            this.Controls.Add(txtSamplingRate);

            // Microphone Dropdown (ComboBox)
            cboMicrophones = new ComboBox();
            cboMicrophones.DropDownStyle = ComboBoxStyle.DropDownList;
            cboMicrophones.Location = new System.Drawing.Point(10, 180);
            cboMicrophones.Size = new System.Drawing.Size(360, 20);
            this.Controls.Add(cboMicrophones);

            // Start Monitoring Button
            btnStartMonitoring = new Button();
            btnStartMonitoring.Text = "Start";
            btnStartMonitoring.Location = new System.Drawing.Point(10, 210);
            btnStartMonitoring.Size = new System.Drawing.Size(90, 30);
            btnStartMonitoring.Click += new EventHandler(StartMonitoring);
            this.Controls.Add(btnStartMonitoring);

            // Stop Monitoring Button
            btnStopMonitoring = new Button();
            btnStopMonitoring.Text = "Stop";
            btnStopMonitoring.Location = new System.Drawing.Point(150, 210);
            btnStopMonitoring.Size = new System.Drawing.Size(90, 30);
            btnStopMonitoring.Enabled = false; // Initially disabled
            btnStopMonitoring.Click += new EventHandler(StopMonitoring);
            this.Controls.Add(btnStopMonitoring);

            // Close Button
            btnClose = new Button();
            btnClose.Text = "Close";
            btnClose.Location = new System.Drawing.Point(280, 210);
            btnClose.Size = new System.Drawing.Size(90, 30);
            btnClose.Click += new EventHandler(CloseApp);
            this.Controls.Add(btnClose);

            // Output TextBox
            txtOutput = new TextBox();
            txtOutput.Multiline = true;
            txtOutput.Location = new System.Drawing.Point(10, 250);
            txtOutput.Size = new System.Drawing.Size(360, 50);
            txtOutput.ReadOnly = true;
            this.Controls.Add(txtOutput);

            // GitHub Link Label
            LinkLabel linkLabelGitHub = new LinkLabel();
            linkLabelGitHub.Text = "Find more information on GitHub";
            linkLabelGitHub.Location = new System.Drawing.Point(10, 310);
            linkLabelGitHub.AutoSize = true;
            linkLabelGitHub.LinkClicked += (sender, args) => 
            {
                Process.Start(new ProcessStartInfo 
                { 
                    FileName = "https://github.com/johnny-de/screamguard", 
                    UseShellExecute = true 
                });
            };
            this.Controls.Add(linkLabelGitHub);

            enumerator = new MMDeviceEnumerator();
            LoadSettings(); // Load Settings from file
            RefreshMicrophones(); // Populate microphones list initially
            StartMicrophoneRefresh(); // Start refreshing the list every 5 seconds
        }

        private void LoadSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);

                warningLevel = settings.WarningLevel;
                alarmLevel = settings.AlarmLevel;
                movingAveragePeriod = settings.MovingAveragePeriod;
                samplingRate = settings.SamplingRate;

                // Populate UI fields with loaded values
                txtWarningLevel.Text = warningLevel.ToString();
                txtAlarmLevel.Text = alarmLevel.ToString();
                txtMovingAverage.Text = movingAveragePeriod.ToString();
                txtSamplingRate.Text = samplingRate.ToString();

                // Refresh microphone list to include any recent changes
                RefreshMicrophones();

                // Select the saved microphone by matching ID if it exists in current list
                foreach (var device in cboMicrophones.Items.Cast<MMDevice>())
                {
                    if (device.ID == settings.MicrophoneId)
                    {
                        cboMicrophones.SelectedItem = device;
                        selectedMicrophone = device;
                        break;
                    }
                }
            }
            else
            {
                // Use default values if no settings file exists
                txtWarningLevel.Text = "20";
                txtAlarmLevel.Text = "30";
                txtMovingAverage.Text = "20";
                txtSamplingRate.Text = "100";
            }
        }


        private void SaveSettings()
        {
            var settings = new AppSettings
            {
                WarningLevel = float.TryParse(txtWarningLevel.Text, out float wl) ? wl : warningLevel,
                AlarmLevel = float.TryParse(txtAlarmLevel.Text, out float al) ? al : alarmLevel,
                MovingAveragePeriod = int.TryParse(txtMovingAverage.Text, out int ma) ? ma : movingAveragePeriod,
                SamplingRate = int.TryParse(txtSamplingRate.Text, out int sr) ? sr : samplingRate,
                MicrophoneId = cboMicrophones.SelectedItem is MMDevice device ? device.ID : null
            };

            string json = JsonSerializer.Serialize(settings);
            File.WriteAllText(SettingsFilePath, json);
        }

        private void RefreshMicrophones()
        {
            // Save the currently selected microphone, if any
            string previouslySelectedDeviceId = cboMicrophones.SelectedItem is MMDevice selectedDevice ? selectedDevice.ID : null;

            var captureDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

            // Update the list only if there are changes
            bool listUpdated = false;
            foreach (var device in captureDevices)
            {
                if (!cboMicrophones.Items.Cast<MMDevice>().Any(d => d.ID == device.ID))
                {
                    // Add new devices to the list if not already present
                    cboMicrophones.Items.Add(device);
                    listUpdated = true;
                }
            }

            // Remove devices that are no longer available
            for (int i = cboMicrophones.Items.Count - 1; i >= 0; i--)
            {
                var device = (MMDevice)cboMicrophones.Items[i];
                if (!captureDevices.Any(d => d.ID == device.ID))
                {
                    cboMicrophones.Items.RemoveAt(i);
                    listUpdated = true;
                }
            }

            // Re-select the previously selected microphone if still available
            if (!string.IsNullOrEmpty(previouslySelectedDeviceId))
            {
                var previouslySelectedDevice = captureDevices.FirstOrDefault(d => d.ID == previouslySelectedDeviceId);
                if (previouslySelectedDevice != null)
                {
                    cboMicrophones.SelectedItem = previouslySelectedDevice;
                }
                else if (listUpdated && cboMicrophones.Items.Count > 0)
                {
                    cboMicrophones.SelectedIndex = 0; // Fallback to first device if previous is unavailable
                }
            }
            else if (listUpdated && cboMicrophones.Items.Count > 0)
            {
                cboMicrophones.SelectedIndex = 0; // Default to the first item if no previous selection
            }
        }


        private async void StartMicrophoneRefresh()
        {
            while (true)
            {
                RefreshMicrophones();
                await System.Threading.Tasks.Task.Delay(5000); // Refresh every 5 seconds
            }
        }

        private void StartMonitoring(object sender, EventArgs e)
        {
            SaveSettings(); // Save settings when monitoring starts

            if (cboMicrophones.SelectedItem == null)
            {
                MessageBox.Show("Please select a microphone.");
                return;
            }

            selectedMicrophone = (MMDevice)cboMicrophones.SelectedItem; // Get the selected microphone

            // Try to read Warning and Alarm levels from the user inputs
            if (float.TryParse(txtWarningLevel.Text, out float parsedWarningLevel))
            {
                warningLevel = parsedWarningLevel;
            }

            if (float.TryParse(txtAlarmLevel.Text, out float parsedAlarmLevel))
            {
                alarmLevel = parsedAlarmLevel;
            }

            // Try to read Moving Average period from user input
            if (int.TryParse(txtMovingAverage.Text, out int parsedMovingAveragePeriod))
            {
                movingAveragePeriod = parsedMovingAveragePeriod; // Update moving average period
            }

            // Try to read Sampling Rate period from user input
            if (int.TryParse(txtSamplingRate.Text, out int parsedSamplingRate))
            {
                samplingRate = parsedSamplingRate; // Update moving sampling rate
            }

            monitoring = true;
            btnStartMonitoring.Enabled = false; // Disable Start button
            btnStopMonitoring.Enabled = true;    // Enable Stop button
            txtWarningLevel.Enabled = false;      // Disable input fields
            txtAlarmLevel.Enabled = false;        // Disable input fields
            txtMovingAverage.Enabled = false;     // Disable moving average input field
            txtSamplingRate.Enabled = false;     // Disable sampling rate input field
            cboMicrophones.Enabled = false;       // Disable microphone dropdown
            txtOutput.Clear(); // Clear previous output
            MonitorMicrophone();
        }

        private void StopMonitoring(object sender, EventArgs e)
        {
            monitoring = false; // Stop monitoring
            btnStartMonitoring.Enabled = true;  // Enable Start button
            btnStopMonitoring.Enabled = false;   // Disable Stop button
            txtWarningLevel.Enabled = true;      // Enable input fields
            txtAlarmLevel.Enabled = true;        // Enable input fields
            txtMovingAverage.Enabled = true;     // Enable moving average input field
            txtSamplingRate.Enabled = true;     // Enable sampling input field
            cboMicrophones.Enabled = true;       // Enable microphone dropdown
            txtOutput.Clear();                   // Clear output when stopping monitoring

            // Hide overlays if they're visible
            overlayForm?.Hide();
            alarmOverlayForm?.Hide();
            overlayForm = null; // Allow creating a new instance next time
            alarmOverlayForm = null; // Allow creating a new instance next time
        }

        private async void MonitorMicrophone()
        {
            if (selectedMicrophone == null)
            {
                MessageBox.Show("No microphone selected.");
                return;
            }

            // List to hold the last volume levels
            var volumeLevels = new List<float>();

            while (monitoring)
            {
                float volumeLevel = selectedMicrophone.AudioMeterInformation.MasterPeakValue * 100;

                // Add the current volume level to the list
                volumeLevels.Add(volumeLevel);

                // Keep only the last x values
                if (volumeLevels.Count > movingAveragePeriod) // Use user-defined moving average period
                {
                    volumeLevels.RemoveAt(0);
                }

                // Calculate the median
                float medianVolume = CalculateMedian(volumeLevels);

                // Check for warning and alarm levels
                if (medianVolume >= alarmLevel)
                {
                    txtOutput.Text = $"ALARM! {selectedMicrophone.FriendlyName} is too loud at {medianVolume:F2}%"; // Show alarm message
                    ShowAlarmOverlay();
                }
                else if (medianVolume >= warningLevel)
                {
                    txtOutput.Text = $"WARNING: {selectedMicrophone.FriendlyName} is getting loud at {medianVolume:F2}%"; // Show warning message
                    ShowOverlay();
                    HideAlarmOverlay();
                }
                else
                {
                    txtOutput.Text = $"{selectedMicrophone.FriendlyName} is at {medianVolume:F2}%"; // Show median volume message
                    HideOverlays();
                }

                // Wait for a second before checking again
                await Task.Delay(samplingRate);
            }
        }

        // Method to calculate the median of a list of floats
        private float CalculateMedian(List<float> values)
        {
            if (values.Count == 0)
                return 0;

            var sortedValues = values.OrderBy(x => x).ToList(); // Sort the list

            int midIndex = sortedValues.Count / 2;

            if (sortedValues.Count % 2 == 0) // If even, return average of two middle numbers
            {
                return (sortedValues[midIndex - 1] + sortedValues[midIndex]) / 2;
            }
            else // If odd, return the middle number
            {
                return sortedValues[midIndex];
            }
        }

        private void ShowOverlay()
        {
            if (overlayForm == null || overlayForm.IsDisposed)
            {
                overlayForm = new OverlayForm(Color.Orange); // Show warning overlay
                overlayForm.Show();
            }
        }

        private void ShowAlarmOverlay()
        {
            if (alarmOverlayForm == null || alarmOverlayForm.IsDisposed)
            {
                alarmOverlayForm = new OverlayForm(Color.Red); // Show alarm overlay
                alarmOverlayForm.Show();
            }
        }

        private void HideOverlays()
        {
            overlayForm?.Hide();
            overlayForm = null; // Reset to allow new instance creation next time
            alarmOverlayForm?.Hide();
            alarmOverlayForm = null; // Reset to allow new instance creation next time
        }

        private void HideAlarmOverlay()
        {
            alarmOverlayForm?.Hide();
            alarmOverlayForm = null; // Reset to allow new instance creation next time
        }

        private void CloseApp(object sender, EventArgs e)
        {
            monitoring = false; // Stop monitoring before closing
            overlayForm?.Hide(); // Hide overlay before closing
            alarmOverlayForm?.Hide(); // Hide alarm overlay before closing
            SaveSettings(); // Save settings on close
            this.Close();
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.Run(new Program());
        }
    }

    // Overlay form class
    public class OverlayForm : Form
    {
        private Color borderColor;

        public OverlayForm(Color color)
        {
            this.FormBorderStyle = FormBorderStyle.None; // No border
            this.BackColor = Color.Lime; // Use Lime for transparency
            this.TransparencyKey = Color.Lime; // Set the transparency key
            this.Opacity = 1.0; // Fully opaque for border drawing
            this.TopMost = true; // Stay on top of other windows
            this.WindowState = FormWindowState.Maximized; // Maximize to cover the screen
            this.borderColor = color; // Set the border color based on alarm level
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Set the thickness of the border
            int borderThickness = 20; // Thickness of the border

            // Draw the outer border (rectangle)
            e.Graphics.DrawRectangle(new Pen(borderColor, borderThickness),
                new Rectangle(0, 0, this.Width, this.Height));
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            this.Activate(); // Activate the overlay
        }
    }

    public class AppSettings
    {
        public float WarningLevel { get; set; }
        public float AlarmLevel { get; set; }
        public int MovingAveragePeriod { get; set; }
        public int SamplingRate { get; set; }
        public string MicrophoneId { get; set; }
    }
}
