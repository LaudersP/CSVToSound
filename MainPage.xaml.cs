﻿using System.Diagnostics;
using Microsoft.Maui.Storage;

namespace CSVToSound
{
    public partial class MainPage : ContentPage
    {
        private static string? _selectedFilePath;
        private static int? _lengthOfCSV;
        private static bool _transmitting;
        private static MindToSoundSimulator? _sim;
        private CancellationTokenSource? _cancellationTokenSource;

        public MainPage()
        {
            InitializeComponent();

            _selectedFilePath = null;
            _lengthOfCSV = null;
            _transmitting = false;
            _sim = null;
        }

        private async void OnSelectFileClicked(object sender, EventArgs e)
        {   
            // Create the supported file options
            var options = new PickOptions
            {
                PickerTitle = "Select CSV File",
                FileTypes = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        {DevicePlatform.WinUI, new[] {".csv"} },
                        {DevicePlatform.macOS, new[] {"csv"} }
                    })
            };

            // Open the file explorer while limiting to the file options set above
            var result = await FilePicker.PickAsync(options);

            if (result != null)
            {
                // Get the file path
                _selectedFilePath = result.FullPath;

                // Reassign the buttons text
                SelectFileBtn.Text = "Change File";

                // Attempt to create an instance of the simulaton
                try
                {
                    _sim = new MindToSoundSimulator(_selectedFilePath, "127.0.0.1", Convert.ToInt32(PortNum.Text));
                }
                catch (IOException)    // File opened by another process
                {
                    FileInfoLabel.Text = "Error: File in use by another application,\nplease close to continue.";
                    FileInfoLabel.TextColor = Colors.Red;

                    return;
                }
                catch (Exception ex)    // Misc.
                {
                    // Check if the exception is from the constructor
                    // ... due to the file not being long enough
                    if (ex.Message == "Error: Please select a file with 3+ rows.")
                    {
                        FileInfoLabel.TextColor = Colors.Red;
                        FileInfoLabel.Text = ex.Message;
                    }

                    return;
                }

                // Display the filename
                string[] pathParts = _selectedFilePath.Split("\\");
                FileInfoLabel.Text = "Filename: " + pathParts[pathParts.Length - 1];
                FileInfoLabel.TextColor = Colors.White;

                // Display the length of the file
                _lengthOfCSV = _sim.GetRowCount();
                FileInfoLabel.TextColor = Colors.White;
                FileInfoLabel.Text += "\nTransmissions: " + (_lengthOfCSV - 1).ToString();

                // Calculate the time in seconds
                double lengthSeconds = Convert.ToDouble(_sim.GetTransmissionDelay() * (_lengthOfCSV - 1));

                // Calculate the min
                byte lengthMinutes = 0;
                while (lengthSeconds > 60)
                {
                    lengthSeconds -= 60;
                    lengthMinutes++;
                }

                // Calculate the hours
                byte lengthHours = 0;
                while (lengthMinutes > 60)
                {
                    lengthMinutes -= 60;
                    lengthHours++;
                }

                // Display the estimated length of the file in HRS:MINS:SECS
                FileInfoLabel.Text += $"\nEstimated Time: {lengthHours}:{lengthMinutes}:{Convert.ToByte(Double.Round(lengthSeconds, 2))}";

                // Enable the send buttong
                SendBtn.IsEnabled = true;
            }
        }

        private async void OnSendClicked(object sender, EventArgs e)
        {
            // Check that there is a valid simulator object
            if (_sim == null)
                return;

            if(!_transmitting)  // Send data
            {
                // Disable the user inputs
                SelectFileBtn.IsEnabled = false;
                PortNum.IsEnabled = false;

                // Change the send button into a stop button
                SendBtn.Text = "Stop";
                SendBtn.BackgroundColor = Colors.Red;

                // Pre-update transmitting status
                // ... will be blocked until stopped/done if after simulating
                _transmitting = true;

                // Run the simulation on a background thread
                _cancellationTokenSource = new CancellationTokenSource();
                await Task.Run(_sim.StartMindToSoundSimulation, _cancellationTokenSource.Token);

                // === BLOCKED UNTIL SIMULATION REACHES THE END OF THE CSV FILE ===
                
                if(_sim.GetCSVRowIndex() == _lengthOfCSV)
                    RestoreSendBtn();
            }
            else    // Stop data
            {
                // Cancel the running task
                _cancellationTokenSource?.Cancel();
                Thread.Sleep(100);

                // Stop the simulation
                RestoreSendBtn();
            }
        }

        private void RestoreSendBtn()
        {
            // Switch button to status text
            SendBtn.Text = "Stopping Data...";

            // Zero out the OSC channels
            _sim.StopMindToSoundSimulation();

            // Switch the button to send functionality
            SendBtn.Text = "Send Data";
            SendBtn.BackgroundColor = Colors.Green;
            _transmitting = false;

            // Reactivate the user inputs
            PortNum.IsEnabled = true;
            SelectFileBtn.IsEnabled = true;
        }
    }
}
