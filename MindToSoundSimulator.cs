using CoreOSC;
using System.Diagnostics;

namespace CSVToSound
{
    internal class MindToSoundSimulator
    {
        private readonly string _csvFilePath;
        private int _index;
        private readonly int _rowCount;
        private readonly double _transmissionDelay;
        private OSC _oscTransmitter;
        private readonly string[] _bands = { "theta", "alpha", "betaL", "betaH", "gamma" };
        private readonly string[] _sensors = { "AF3", "F7", "F3", "FC5", "T7", "P7", "O1", "O2", "P8", "T8", "FC6", "F4", "F8", "AF4" };
        private bool _transmission;

        // Constructor
        public MindToSoundSimulator(string csvFilePath, string ipAddress, int portNum)
        {
            _csvFilePath = csvFilePath;
            _index = 0;
            _rowCount = GetFileLength();

            // Validate the length (3 rows due to calculating the transmission delay)
            // ... Row 1 <Headers>
            // ... Row 2 <Timestamp, data, ..., data
            if(_rowCount < 3)
            {
                throw new Exception("Error: Please select a file with 3+ rows.");
            }

            _transmissionDelay = CalculateTransmissionDelay();
            _oscTransmitter = new OSC(ipAddress, portNum);
        }

        // Getter for the CSV file row count
        public int GetRowCount()
        {
            return _rowCount;
        }

        // Getter for the transmission delay
        public double GetTransmissionDelay()
        {
            return _transmissionDelay;
        }

        // Getter for the CSV row index
        public int GetCSVRowIndex()
        {
            return _index;
        }

        // Method for getting the length of the CSV file
        private int GetFileLength()
        {
            int count = 0;

            foreach (string line in File.ReadLines(_csvFilePath))
            {
                count++;
            }

            return count;
        }

        // Method for getting the next single line of data
        private string GetNextCSVLine ()
        {
            // Check if we are at the end of the file
            if (_index == _rowCount)
            {
                return "EOF";
            }

            return File.ReadLines(_csvFilePath).ElementAt(_index++);
        }

        // Method for getting specific a single line of data
        private string GetCSVLine(int index)
        {
            // Check for valid index
            if (index < 0 || index >= _rowCount)
                throw new Exception($"Error: Index out of range (0 - {_rowCount}");

            return File.ReadLines( _csvFilePath).ElementAt(index);
        }

        // Method for breaking down a data row into individual data items
        private static string[] BreakupDataRow(string line)
        {
            string tempLine = line.Replace(", ", ",");
            return tempLine.Split(",");
        }

        // Method for getting the delay between each transmission
        private double CalculateTransmissionDelay()
        {
            // Read the first data row timestamp
            string firstDataRow = GetCSVLine(1);
            string[] firstDataRowEntries = BreakupDataRow(firstDataRow);
            double firstTimestamp = Convert.ToDouble(firstDataRowEntries[0]);

            // Read the last data row timestamp
            string lastDataRow = GetCSVLine(_rowCount - 1);
            string[] lastDataRowEntries = BreakupDataRow(lastDataRow);
            double lastTimestamp = Convert.ToDouble(lastDataRowEntries[0]);

            // Calculate the average transmission delay
            return ((lastTimestamp - firstTimestamp) / (_rowCount - 1)) / (532f / 480f);
        }

        // Method for getting the OSC channel string
        private string GetOSCAddress(int index)
        {
            // Determine the band
            string band = _bands[(index - 1) % _bands.Length];

            // Assign the sensor
            string sensor = _sensors[(index - 1) / _bands.Length];

            // Construct the OSC address string
            return $"/{band}/{sensor}";
        }

        // Method used to simulate the MindToSound middleware
        public void StartMindToSoundSimulation()
        {
            _transmission = true;
            string line = "";

            // Skip the header row
            _index = 1;

            do
            {
                // Get a line of data
                line = GetNextCSVLine();
                if (line == "EOF")
                    break;

                // Get the data entries
                string[] dataEntries = BreakupDataRow(line);

                // Iterate through the data
                for(int i = 1; i <= (dataEntries.Length - 1); i++)
                {
                    if(i >= dataEntries.Length)
                        break;

                    // Construct the arguments
                    float floatValue = Convert.ToSingle(dataEntries[i]);
                    object[] args = { floatValue };

                    // Send the OSC message
                    _oscTransmitter.SendMessage(GetOSCAddress(i), args);
                }

                Thread.Sleep(Convert.ToInt32(_transmissionDelay * 1000));
            }
            while (line != "EOF" && _transmission);
        }

        public void StopMindToSoundSimulation()
        {

            _transmission = false;
            Thread.Sleep(100);

            // Zero each channel
            foreach(string sensor in _sensors)
            {
                foreach(string band in _bands)
                {
                    object[] args = { 0f };
                    _oscTransmitter.SendMessage($"/{band}/{sensor}", args);
                }

                Thread.Sleep(Convert.ToInt32(_transmissionDelay * 1000));
            }
        }
    }
}
