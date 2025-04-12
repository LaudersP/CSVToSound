using System.Diagnostics;
using Microsoft.Maui.Storage;

namespace CSVToSound
{
    public partial class MainPage : ContentPage, IMessageService
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

        public Task DisplayAlert(string title, string message, string cancel)
        {
            return Device.InvokeOnMainThreadAsync(() =>
                base.DisplayAlert(title, message, cancel));
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
                        {DevicePlatform.MacCatalyst, new[] {"csv", "public.comma-separated-values-text"} },
                        {DevicePlatform.macOS, new[] {"csv", "public.comma-separated-values-text"} }
                    })
            };

            // Open the file explorer while limiting to the file options set above
            var result = await FilePicker.PickAsync(options);

            // Disable the send button until file is validated
            SendBtn.IsEnabled = false;

            if (result != null)
            {
                // Get the file path
                _selectedFilePath = result.FullPath;

                // Reassign the buttons text
                SelectFileBtn.Text = "Change File";

                // Attempt to create an instance of the simulation
                try
                {
                    _sim = new MindToSoundSimulator(_selectedFilePath, "127.0.0.1", Convert.ToInt32(PortNum.Text), this);
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
                    FileInfoLabel.TextColor = Colors.Red;
                    FileInfoLabel.Text = ex.Message;

                    return;
                }

                // Enable the send button as the file must be valid
                SendBtn.IsEnabled = true;

                // Display the filename
                string filename = Path.GetFileName(_selectedFilePath);
                FileInfoLabel.Text = "Filename: " + filename;
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

                // Enable the send button
                SendBtn.IsEnabled = true;

                // Enable the state buttons
                bool[] buttonEnables = _sim.GetButtonStates();
                BaselineBtn.IsEnabled = buttonEnables[0];
                TransitionToThBtn.IsEnabled = buttonEnables[1];
                ThBtn.IsEnabled = buttonEnables[2];
                TransitionToFlowBtn.IsEnabled = buttonEnables[3];
                FlowBtn.IsEnabled = buttonEnables[4];
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
            // Check that there is a valid simulator object
            if (_sim == null)
                return;

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

        // Method for disabling all buttons
        private void DisableBtn()
        {
            PortNum.IsEnabled = false;
            SelectFileBtn.IsEnabled = false;
            SendBtn.IsEnabled = false;
            BaselineBtn.IsEnabled = false;
            TransitionToThBtn.IsEnabled = false;
            ThBtn.IsEnabled = false;
            TransitionToFlowBtn.IsEnabled = false;
            FlowBtn.IsEnabled = false;
        }

        // Method for enabling all buttons
        private void EnableBtn()
        {
            PortNum.IsEnabled = true;
            SelectFileBtn.IsEnabled = true;
            SendBtn.IsEnabled = true;
            BaselineBtn.IsEnabled = true;
            TransitionToThBtn.IsEnabled = true;
            ThBtn.IsEnabled = true;
            TransitionToFlowBtn.IsEnabled = true;
            FlowBtn.IsEnabled = true;
        }

        public async void OnBaselineBtnClicked(object sender, EventArgs e)
        {
            DisableBtn();
            await _sim.PlaybackState(MindToSoundSimulator.State.Baseline);
            EnableBtn();
        }

        public async void OnTransitionToThBtnClicked(object sender, EventArgs e)
        {
            DisableBtn();
            await _sim.PlaybackState(MindToSoundSimulator.State.TransitionToTh);
            EnableBtn();
        }

        public async void OnThBtnClicked(object sender, EventArgs e)
        {
            DisableBtn();
            await _sim.PlaybackState(MindToSoundSimulator.State.Th);
            EnableBtn();
        }

        public async void OnTransitionToFlowBtnClicked(object sender, EventArgs e)
        {
            DisableBtn();
            await _sim.PlaybackState(MindToSoundSimulator.State.TransitionToFlow);
            EnableBtn();
        }

        public async void OnFlowBtnClicked(object sender, EventArgs e)
        {
            DisableBtn();
            await _sim.PlaybackState(MindToSoundSimulator.State.Flow);
            EnableBtn();
        }
    }
}
