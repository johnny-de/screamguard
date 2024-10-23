using System;
using System.Drawing;
using System.Windows.Forms;
using NAudio.CoreAudioApi;

namespace ScreamGuard
{
    public class Program : Form
    {
        private Label lblWarningLevel;
        private TextBox txtWarningLevel;
        private Label lblAlarmLevel;
        private TextBox txtAlarmLevel;
        private Button btnClose;
        private Button btnStartMonitoring;
        private Button btnStopMonitoring;
        private TextBox txtOutput;
        private float warningLevel = 50f;
        private float alarmLevel = 70f;
        private MMDeviceEnumerator enumerator;
        private bool monitoring = false;
        private OverlayForm overlayForm; // Overlay form to show warning
        private OverlayForm alarmOverlayForm; // Overlay form to show alarm

        public Program()
        {
            // Initialize UI components
            this.Text = "ScreamGuard"; // Set the title to ScreamGuard
            this.Size = new System.Drawing.Size(400, 250);
            this.FormBorderStyle = FormBorderStyle.FixedSingle; // Prevent window resizing

            // Warning Level Label
            lblWarningLevel = new Label();
            lblWarningLevel.Text = "Warning Level (%):";
            lblWarningLevel.Location = new System.Drawing.Point(10, 20);
            lblWarningLevel.AutoSize = true;
            this.Controls.Add(lblWarningLevel);

            // Warning Level TextBox
            txtWarningLevel = new TextBox();
            txtWarningLevel.Text = "50"; // Default value
            txtWarningLevel.Location = new System.Drawing.Point(150, 20);
            this.Controls.Add(txtWarningLevel);

            // Alarm Level Label
            lblAlarmLevel = new Label();
            lblAlarmLevel.Text = "Alarm Level (%):";
            lblAlarmLevel.Location = new System.Drawing.Point(10, 60);
            lblAlarmLevel.AutoSize = true;
            this.Controls.Add(lblAlarmLevel);

            // Alarm Level TextBox
            txtAlarmLevel = new TextBox();
            txtAlarmLevel.Text = "70"; // Default value
            txtAlarmLevel.Location = new System.Drawing.Point(150, 60);
            this.Controls.Add(txtAlarmLevel);

            // Start Monitoring Button
            btnStartMonitoring = new Button();
            btnStartMonitoring.Text = "Start";
            btnStartMonitoring.Location = new System.Drawing.Point(10, 100);
            btnStartMonitoring.Click += new EventHandler(StartMonitoring);
            this.Controls.Add(btnStartMonitoring);

            // Stop Monitoring Button
            btnStopMonitoring = new Button();
            btnStopMonitoring.Text = "Stop";
            btnStopMonitoring.Location = new System.Drawing.Point(150, 100);
            btnStopMonitoring.Enabled = false; // Initially disabled
            btnStopMonitoring.Click += new EventHandler(StopMonitoring);
            this.Controls.Add(btnStopMonitoring);

            // Close Button
            btnClose = new Button();
            btnClose.Text = "Close";
            btnClose.Location = new System.Drawing.Point(290, 100);
            btnClose.Click += new EventHandler(CloseApp);
            this.Controls.Add(btnClose);

            // Output TextBox
            txtOutput = new TextBox();
            txtOutput.Multiline = true;
            txtOutput.Location = new System.Drawing.Point(10, 140);
            txtOutput.Size = new System.Drawing.Size(360, 50);
            txtOutput.ReadOnly = true;
            this.Controls.Add(txtOutput);

            enumerator = new MMDeviceEnumerator();
        }

        private void StartMonitoring(object sender, EventArgs e)
        {
            // Try to read Warning and Alarm levels from the user inputs
            if (float.TryParse(txtWarningLevel.Text, out float parsedWarningLevel))
            {
                warningLevel = parsedWarningLevel;
            }

            if (float.TryParse(txtAlarmLevel.Text, out float parsedAlarmLevel))
            {
                alarmLevel = parsedAlarmLevel;
            }

            monitoring = true;
            btnStartMonitoring.Enabled = false; // Disable Start button
            btnStopMonitoring.Enabled = true;    // Enable Stop button
            txtWarningLevel.Enabled = false;      // Disable input fields
            txtAlarmLevel.Enabled = false;        // Disable input fields
            txtOutput.Clear(); // Clear previous output
            MonitorMicrophones();
        }

        private void StopMonitoring(object sender, EventArgs e)
        {
            monitoring = false; // Stop monitoring
            btnStartMonitoring.Enabled = true;  // Enable Start button
            btnStopMonitoring.Enabled = false;   // Disable Stop button
            txtWarningLevel.Enabled = true;      // Enable input fields
            txtAlarmLevel.Enabled = true;        // Enable input fields
            txtOutput.Clear();                   // Clear output when stopping monitoring

            // Hide overlays if they're visible
            overlayForm?.Hide();
            alarmOverlayForm?.Hide();
            overlayForm = null; // Allow creating a new instance next time
            alarmOverlayForm = null; // Allow creating a new instance next time
        }

        private async void MonitorMicrophones()
        {
            var captureDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

            if (captureDevices.Count == 0)
            {
                MessageBox.Show("No active microphones found.");
                return;
            }

            while (monitoring)
            {
                float maxVolume = 0;
                string maxVolumeDeviceName = "";

                foreach (var device in captureDevices)
                {
                    float volumeLevel = device.AudioMeterInformation.MasterPeakValue * 100;

                    if (volumeLevel > maxVolume)
                    {
                        maxVolume = volumeLevel;
                        maxVolumeDeviceName = device.FriendlyName;
                    }

                    // Check for warning and alarm levels
                    if (volumeLevel >= alarmLevel)
                    {
                        txtOutput.Text = $"ALARM! {device.FriendlyName} is too loud at {volumeLevel:F2}%"; // Show alarm message
                        // Hide warning overlay and show alarm overlay
                        if (overlayForm != null)
                        {
                            overlayForm.Hide();
                            overlayForm = null; // Reset to allow new instance creation next time
                        }
                        ShowAlarmOverlay();
                    }
                    else if (volumeLevel >= warningLevel)
                    {
                        txtOutput.Text = $"WARNING: {device.FriendlyName} is getting loud at {volumeLevel:F2}%"; // Show warning message
                        // Show overlay for warning
                        ShowOverlay();
                    }
                    else
                    {
                        txtOutput.Text = $"{device.FriendlyName} is at {volumeLevel:F2}%"; // Show volume message
                        // Hide overlays if volume is below warning level
                        if (overlayForm != null)
                        {
                            overlayForm.Hide();
                            overlayForm = null; // Reset to allow new instance creation next time
                        }

                        if (alarmOverlayForm != null)
                        {
                            alarmOverlayForm.Hide();
                            alarmOverlayForm = null; // Reset to allow new instance creation next time
                        }
                    }
                }

                // Wait for a second before checking again
                await System.Threading.Tasks.Task.Delay(500);
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

        private void CloseApp(object sender, EventArgs e)
        {
            monitoring = false; // Stop monitoring before closing
            overlayForm?.Hide(); // Hide overlay before closing
            alarmOverlayForm?.Hide(); // Hide alarm overlay before closing
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
}
