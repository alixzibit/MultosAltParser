using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MultosAltParser
{
    /// <summary>
    /// Interaction logic for DataPreparationControl.xaml
    /// </summary>
    public partial class DataPreparationControl : UserControl
    {
        private List<PersonalizationRecord> _personalizationRecords;
        private AltParser _altParser;
        private AluDataRecord _aluData;
        private CardholderData _cdfData;
        private CdfParser _cdfParser = new CdfParser();
        private KeyData _keyData;
        private KeyDataParser _keyParser = new KeyDataParser();


        public DataPreparationControl()
        {
            InitializeComponent();
        }

        public void LoadAltData(AltParser parser, List<PdaRecord> pdaRecords, AluDataRecord aluData)
        {
            _altParser = parser;
            _aluData = aluData;

            Debug.WriteLine("Converting PDA records to Personalization records...");

            _personalizationRecords = pdaRecords
        .Where(p => p.DataUsage == "Input to ALU")  // Only get records that need input
        .Select(p =>
        {
            var record = new PersonalizationRecord
            {
                Tag = p.Tag,
                TagDescription = p.TagDescription,
                DataSource = p.DataSource,
                Address = p.Address,
                DataFormat = p.DataFormat,
                Length = p.Length,
                CurrentData = ExtractCurrentData(p)
            };

            // Category 1: CDF Data (Green)
            if (p.DataSource == "CDF")
            {
                record.Status = "Pending CDF";
                // PersonalizationData will be set when CDF is loaded
            }
            // Category 2: Keys Data (DarkSlateBlue)
            else if (IsKeyData(p.Tag))
            {
                record.Status = "Pending Keys";
                // PersonalizationData will be set when Keys are loaded
            }
            // Category 3: Default Data (Pass through current data)
            else
            {
                record.PersonalizationData = record.CurrentData;
                record.Status = "Ready (Default)";
            }

            return record;
        }).ToList();

            Debug.WriteLine("\nPersonalization Records Created:");
            foreach (var record in _personalizationRecords)
            {
                Debug.WriteLine($"Tag: {record.Tag}, Format: {record.DataFormat}, Address: {record.Address}");
            }

            dgPersonalization.ItemsSource = _personalizationRecords;
            
        }

        private bool IsKeyData(string tag)
        {
            var normalizedTag = tag.TrimStart('0');
            var keyTags = new HashSet<string>
    {
        "DF42", "DF43", "DF44", "DF5E",
        "DF60", "DF61", "9F46", "0090"
    };

            // Check if either the normalized tag or the original tag is in the set
            return keyTags.Contains(normalizedTag) || keyTags.Contains(tag);
        }


        private string ExtractCurrentData(PdaRecord pda)
        {
            try
            {
                Debug.WriteLine($"\nExtracting data for tag {pda.Tag}:");
                int offset = Convert.ToInt32(pda.Address, 16);
                int length = Convert.ToInt32(pda.Length, 16);
                byte[] sourceData = pda.DataCategory == "FCI data" ?
                                 _aluData.FciRecord : _aluData.DataRecord;

                if (sourceData == null)
                    return "No data available";

                // For TLV format, extract just the value
                if (pda.DataFormat == "TLV format")
                {
                    if (offset + 2 <= sourceData.Length)
                    {
                        // Handle tag
                        bool isOneByteTags = pda.Tag.TrimStart('0').Length <= 2;
                        int tagLength = isOneByteTags ? 1 : 2;
                        string tagInData = BitConverter.ToString(sourceData, offset, tagLength).Replace("-", "");
                        Debug.WriteLine($"Tag in data: {tagInData}");

                        // Handle length bytes
                        int lengthBytesStart = offset + tagLength;
                        int dataLength;
                        int numberOfLengthBytes = 1;

                        byte firstLengthByte = sourceData[lengthBytesStart];
                        if ((firstLengthByte & 0x80) != 0)  // Check if high bit is set
                        {
                            // Number of subsequent length bytes is indicated by low 7 bits
                            numberOfLengthBytes = (firstLengthByte & 0x7F) + 1;
                            Debug.WriteLine($"Extended length encoding: {numberOfLengthBytes} bytes");

                            // Calculate actual length from subsequent bytes
                            dataLength = 0;
                            for (int i = 1; i < numberOfLengthBytes; i++)
                            {
                                dataLength = (dataLength << 8) | sourceData[lengthBytesStart + i];
                            }
                        }
                        else
                        {
                            dataLength = firstLengthByte;
                        }

                        Debug.WriteLine($"Length bytes: {BitConverter.ToString(sourceData, lengthBytesStart, numberOfLengthBytes)}");
                        Debug.WriteLine($"Calculated data length: {dataLength}");

                        // Read the value
                        int valueOffset = lengthBytesStart + numberOfLengthBytes;
                        if (valueOffset + dataLength <= sourceData.Length)
                        {
                            byte[] actualValue = new byte[dataLength];
                            Array.Copy(sourceData, valueOffset, actualValue, 0, dataLength);
                            string value = BitConverter.ToString(actualValue).Replace("-", " ");
                            Debug.WriteLine($"Extracted value: {value}");
                            return value;
                        }
                        else
                        {
                            Debug.WriteLine("TLV structure incomplete");
                            return "TLV structure incomplete";
                        }
                    }
                }
                else // Non-TLV format
                {
                    if (offset + length <= sourceData.Length)
                    {
                        byte[] data = new byte[length];
                        Array.Copy(sourceData, offset, data, 0, length);
                        string value = BitConverter.ToString(data).Replace("-", " ");
                        Debug.WriteLine($"Extracted non-TLV value: {value}");
                        return value;
                    }
                    else
                    {
                        Debug.WriteLine("Data out of bounds");
                        return "Data out of bounds";
                    }
                }

                Debug.WriteLine("Invalid data");
                return "Invalid data";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error extracting data: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }


        private async void BtnLoadCDF_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "CDF Files (*.xml)|*.xml|All files (*.*)|*.*",
                Title = "Select CDF File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                await LoadCDFData(openFileDialog.FileName);
            }
        }

        private async Task LoadCDFData(string filename)
        {
            try
            {
                txtStatus.Text = "Loading CDF file...";
                _cdfData = await _cdfParser.ParseCdfXml(filename);

                // Update personalization data for CDF records
                foreach (var record in _personalizationRecords.Where(r => r.DataSource == "CDF"))
                {
                    string personalizationData = _cdfParser.GetPersonalizationData(
                        record.Tag,
                        _cdfData,
                        record.Length);

                    if (!string.IsNullOrEmpty(personalizationData))
                    {
                        record.PersonalizationData = personalizationData;
                        record.Status = "Ready";
                    }
                    else
                    {
                        record.Status = "Error: No data";
                    }
                }

                dgPersonalization.Items.Refresh();
                txtStatus.Text = "CDF data loaded successfully";
            }
            catch (Exception ex)
            {
                txtStatus.Text = "Error loading CDF";
                CustomMessageBox.Show($"Error loading CDF: {ex.Message}");
            }
        }

      

        private async void BtnCreateAlu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!IsDataReady())
                    return;

                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "ALU Files (*.alu)|*.alu|All files (*.*)|*.*",
                    Title = "Save ALU File",
                    DefaultExt = "alu"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    txtStatus.Text = "Creating ALU file...";

                    // Use the already parsed data
                    var aluBuilder = new AluBuilder(_altParser, _aluData);
                    aluBuilder.InsertPersonalizedData(_personalizationRecords);

                    await aluBuilder.SaveAluFile(saveFileDialog.FileName);

                    txtStatus.Text = "ALU file created successfully";
                    CustomMessageBox.Show("ALU file has been created successfully!");
                }
            }
            catch (Exception ex)
            {
                txtStatus.Text = "Error creating ALU file";
                CustomMessageBox.Show($"Error creating ALU file:\n{ex.Message}");
            }
        }

        private bool ValidatePersonalizationData()
        {
            return _personalizationRecords
                .Where(r => r.DataUsage == "Input to ALU")
                .All(r => r.Status == "Ready");
        }

        private async void BtnLoadKeys_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Key Files (*.xml)|*.xml|All files (*.*)|*.*",
                Title = "Select Key File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                await LoadKeyData(openFileDialog.FileName);
            }
        }

        private async Task LoadKeyData(string filename)
        {
            try
            {
                txtStatus.Text = "Loading key file...";
                _keyData = await _keyParser.ParseKeyXml(filename);

                Debug.WriteLine("\nLoaded keys in dictionary:");
                foreach (var kv in _keyData.KeyValues)
                {
                    Debug.WriteLine($"Key: {kv.Key}, Value: {kv.Value}");
                }

                // Update personalization data for key records
                foreach (var record in _personalizationRecords.Where(r => IsKeyData(r.Tag)))
                {
                    Debug.WriteLine($"\nProcessing record with tag: {record.Tag}");

                    // Try with original tag first, then normalized
                    if (_keyData.KeyValues.TryGetValue(record.Tag, out string keyValue) ||
                        _keyData.KeyValues.TryGetValue(record.Tag.TrimStart('0'), out keyValue))
                    {
                        record.PersonalizationData = keyValue;
                        record.Status = "Ready";
                        Debug.WriteLine($"Found key value for {record.Tag}: {keyValue}");
                    }
                    else
                    {
                        record.Status = "Error: Key not found";
                        Debug.WriteLine($"No key found for {record.Tag}");
                    }
                }

                dgPersonalization.Items.Refresh();
                txtStatus.Text = "Key data loaded successfully";
                ValidateKeyLoading();
            }
            catch (Exception ex)
            {
                txtStatus.Text = "Error loading keys";
                Debug.WriteLine($"Error: {ex.Message}");
                CustomMessageBox.Show($"Error loading keys: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ValidateKeyLoading()
        {
            var missingKeys = _personalizationRecords
                .Where(r => IsKeyData(r.Tag) && r.Status != "Ready")
                .Select(r => $"{r.Tag} ({r.TagDescription})")
                .ToList();

            // Check if any records have a status other than "Ready"
            if (missingKeys.Any())
            {
                Debug.WriteLine("Key data missing for:");
                CustomMessageBox.Show(
                    $"Missing or invalid key data for:\n{string.Join("\n", missingKeys)}",
                    "Key Data Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }


        //private void ValidateKeyData()
        //{
        //    var missingKeys = _personalizationRecords
        //        .Where(r => IsKeyData(r.Tag) && string.IsNullOrEmpty(r.PersonalizationData))
        //        .Select(r => r.TagDescription)
        //        .ToList();

        //    if (missingKeys.Any())
        //    {
        //        CustomMessageBox.Show(
        //            $"Missing key data for:\n{string.Join("\n", missingKeys)}",
        //            "Validation Warning",
        //            MessageBoxButton.OK,
        //            MessageBoxImage.Warning);
        //    }
        //}

        private bool IsDataReady()
        {
            // Check CDF data
            var unreadyCdfRecords = _personalizationRecords
                .Where(r => r.DataSource == "CDF" && r.Status != "Ready")
                .ToList();

            if (unreadyCdfRecords.Any())
            {
                CustomMessageBox.Show("Some CDF data is not ready. Please load CDF file first.",
                               "CDF Data Missing", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Check Key data
            var unreadyKeyRecords = _personalizationRecords
                .Where(r => IsKeyData(r.Tag) && r.Status != "Ready")
                .ToList();

            if (unreadyKeyRecords.Any())
            {
                CustomMessageBox.Show("Some Key data is not ready. Please load Keys file first.",
                               "Key Data Missing", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }


    }
}
