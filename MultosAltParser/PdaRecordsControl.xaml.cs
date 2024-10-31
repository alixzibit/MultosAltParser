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
using static MultosAltParser.AltParser;

namespace MultosAltParser
{
    /// <summary>
    /// Interaction logic for PdaRecordsControl.xaml
    /// </summary>
    public partial class PdaRecordsControl : UserControl
    {
        private AltParser _parser;
        private List<PdaRecord> _pdaRecords;
        private AluDataRecord _aluData;

        public PdaRecordsControl()
        {
            InitializeComponent();
        }

        public void LoadPdaRecords(AltParser parser, List<PdaRecord> pdaRecords, AluDataRecord aluData)
        {
            _parser = parser;
            _pdaRecords = pdaRecords;
            _aluData = aluData;  // Store the ALU data
            dgPdaRecords.ItemsSource = _pdaRecords;
        }


        //private void DgPdaRecords_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    var selectedPda = dgPdaRecords.SelectedItem as PdaRecord;
        //    if (selectedPda == null || _aluData == null) return;

        //    try
        //    {
        //        // Convert hex address to decimal offset
        //        int offset = Convert.ToInt32(selectedPda.Address, 16);
        //        int length = Convert.ToInt32(selectedPda.Length, 16);

        //        // Get the appropriate data source from AluDataRecord
        //        byte[] sourceData = selectedPda.DataCategory == "FCI data" ?
        //                          _aluData.FciRecord : _aluData.DataRecord;

        //        if (sourceData == null)
        //        {
        //            txtLocation.Text = "Error: Source data not available";
        //            txtDataLength.Text = "N/A";
        //            txtDataValue.Text = "No data available";
        //            return;
        //        }

        //        // Calculate absolute position
        //        long absolutePosition = selectedPda.DataCategory == "FCI data" ?
        //            _parser.FciRecordStartPosition + offset :
        //            _parser.DataRecordStartPosition + offset;

        //        // Extract data
        //        if (offset + length <= sourceData.Length)
        //        {
        //            byte[] data = new byte[length];
        //            Array.Copy(sourceData, offset, data, 0, length);

        //            // Update UI
        //            txtLocation.Text = $"Offset: 0x{offset:X4} (Absolute: 0x{absolutePosition:X4})";
        //            txtDataLength.Text = $"{length} bytes (0x{length:X4})";
        //            txtDataValue.Text = BitConverter.ToString(data).Replace("-", " ");
        //        }
        //        else
        //        {
        //            txtLocation.Text = $"Invalid offset (0x{offset:X4} + {length} exceeds {sourceData.Length} bytes)";
        //            txtDataLength.Text = "N/A";
        //            txtDataValue.Text = "Data out of bounds";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        txtLocation.Text = "Error";
        //        txtDataLength.Text = "Error";
        //        txtDataValue.Text = $"Error extracting data: {ex.Message}";
        //    }

        //}

        private void DgPdaRecords_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedPda = dgPdaRecords.SelectedItem as PdaRecord;
            if (selectedPda == null || _aluData == null) return;

            try
            {
                Debug.WriteLine($"\nProcessing PDA Record:");
                Debug.WriteLine($"Tag: {selectedPda.Tag}");
                Debug.WriteLine($"Format: {selectedPda.DataFormat}");
                Debug.WriteLine($"Address: {selectedPda.Address}");

                int offset = Convert.ToInt32(selectedPda.Address, 16);
                int length = Convert.ToInt32(selectedPda.Length, 16);
                byte[] sourceData = selectedPda.DataCategory == "FCI data" ?
                                  _aluData.FciRecord : _aluData.DataRecord;

                if (sourceData == null)
                {
                    txtLocation.Text = "Error: Source data not available";
                    txtDataLength.Text = "N/A";
                    txtDataValue.Text = "No data available";
                    return;
                }

                if (selectedPda.DataFormat == "TLV format")
                {
                    if (offset + 2 <= sourceData.Length)
                    {
                        // Handle tag
                        bool isOneByteTags = selectedPda.Tag.TrimStart('0').Length <= 2;
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

                            // Update UI
                            txtLocation.Text = $"Offset: 0x{offset:X4} (TLV structure)";
                            txtDataLength.Text = $"Value Length: {dataLength} bytes";
                            txtDataValue.Text = $"Tag: {tagInData}, Length: {BitConverter.ToString(sourceData, lengthBytesStart, numberOfLengthBytes).Replace("-", " ")}, " +
                                              $"Value: {BitConverter.ToString(actualValue).Replace("-", " ")}";

                            Debug.WriteLine($"Value: {BitConverter.ToString(actualValue).Replace("-", " ")}");
                        }
                        else
                        {
                            txtDataValue.Text = "Error: TLV structure incomplete";
                            Debug.WriteLine("Error: TLV structure incomplete");
                        }
                    }
                }
                else
                {
                    // Non-TLV format
                    if (offset + length <= sourceData.Length)
                    {
                        byte[] data = new byte[length];
                        Array.Copy(sourceData, offset, data, 0, length);
                        txtLocation.Text = $"Offset: 0x{offset:X4}";
                        txtDataLength.Text = $"{length} bytes (0x{length:X4})";
                        txtDataValue.Text = BitConverter.ToString(data).Replace("-", " ");

                        Debug.WriteLine($"Non-TLV data: {BitConverter.ToString(data).Replace("-", " ")}");
                    }
                    else
                    {
                        txtLocation.Text = $"Invalid offset (0x{offset:X4} + {length} exceeds {sourceData.Length} bytes)";
                        txtDataLength.Text = "N/A";
                        txtDataValue.Text = "Data out of bounds";
                        Debug.WriteLine("Error: Data out of bounds");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                txtLocation.Text = "Error";
                txtDataLength.Text = "Error";
                txtDataValue.Text = $"Error extracting data: {ex.Message}";
            }
        }

        // Helper method to check if it's a one-byte tag
        private bool IsOneByteTag(string tag)
        {
            string normalizedTag = tag.TrimStart('0');
            return normalizedTag.Length <= 2;
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.csv)|*.csv",
                    DefaultExt = "csv",
                    FileName = "PDA_Records_Export.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (StreamWriter sw = new StreamWriter(saveFileDialog.FileName, false, Encoding.UTF8))
                    {
                        // Write headers
                        var headers = dgPdaRecords.Columns
                            .Select(c => $"\"{c.Header}\"")
                            .ToList();
                        sw.WriteLine(string.Join(",", headers));

                        // Write data rows
                        foreach (PdaRecord item in dgPdaRecords.Items)
                        {
                            var fields = new List<string>
                        {
                            QuoteCsvField(item.No),
                            QuoteCsvField(item.Pda),
                            QuoteCsvField(item.Tag),
                            QuoteCsvField(item.TagDescription),
                            QuoteCsvField(item.Length),
                            QuoteCsvField(item.Address),
                            QuoteCsvField(item.DataSource),
                            QuoteCsvField(item.DataUsage),
                            QuoteCsvField(item.DataFormat),
                            QuoteCsvField(item.InputPresence),
                            QuoteCsvField(item.LengthCheck),
                            QuoteCsvField(item.DataCategory),
                            QuoteCsvField(item.ProfileNo),
                            QuoteCsvField(item.ProfileDescription),
                            QuoteCsvField(item.Interface)
                        };

                            sw.WriteLine(string.Join(",", fields));
                        }
                    }

                    CustomMessageBox.Show(
                        "Export completed successfully!",
                        "Export Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(
                    $"Error exporting data: {ex.Message}",
                    "Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private string QuoteCsvField(string field)
        {
            if (string.IsNullOrEmpty(field)) return "\"\"";

            // Replace any quotes in the field with double quotes
            field = field.Replace("\"", "\"\"");

            // Wrap the field in quotes
            return $"\"{field}\"";
        }
    }
}
