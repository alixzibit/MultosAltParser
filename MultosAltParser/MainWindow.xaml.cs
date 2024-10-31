using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Windows;
using System.Diagnostics;
using System.Windows.Shell;
using System.Windows.Input;

namespace MultosAltParser
{
    public partial class MainWindow : BaseWindow
    {
        private AltParser _parser;
        private string _currentFilePath;



        public MainWindow()
        {
            InitializeComponent();
        }

        protected void CloseClick(object sender, RoutedEventArgs e)
        {
            Close();
            e.Handled = true;
        }


        private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "ALT Files (*.alt)|*.alt|All files (*.*)|*.*",
                Title = "Select ALT File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    _currentFilePath = openFileDialog.FileName;
                    txtSelectedFile.Text = $"Selected file: {System.IO.Path.GetFileName(_currentFilePath)}";

                    _parser = new AltParser(_currentFilePath);

                    // Parse headers first
                    var fileHeader = _parser.ParseFileHeader();
                    DisplayFileHeader(fileHeader);

                    var templateHeader = _parser.ParseTemplateHeader();
                    DisplayTemplateHeader(templateHeader);

                    // Parse ALU data which includes PDA section
                    var aluData = _parser.ParseAluDataRecord();
                    aluDataControl.DisplayAluData(aluData);

                    Debug.WriteLine("Parsing PDA Records...");
                    var pdaRecords = _parser.ParsePdaRecords();  // This should NOT call ParseAluDataSections again
                    pdaRecordsControl.LoadPdaRecords(_parser, pdaRecords, aluData);

                    // Load data into Data Preparation control
                    dataPreparationControl.LoadAltData(_parser, pdaRecords, aluData);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Error reading ALT file: {ex.Message}", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private string FormatUInt32ToHex(uint value)
        {
            // Convert to byte array and reverse for big-endian
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        private string FormatUInt16ToHex(ushort value)
        {
            // Convert to byte array and reverse for big-endian
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        private void DisplayFileHeader(AltFileHeader header)
        {
            txtFileType.Text = header.FileTypeCode;
            txtProtectionMethod.Text = $"0x{header.FileProtectionMethodId:X2}";
            txtStructureMethod.Text = $"0x{header.FileStructureMethodId:X2}";
            txtConsignmentId.Text = header.ConsignmentId;
            txtFileDate.Text = header.FileDate.ToString("yyyy-MM-dd");
            txtFileTime.Text = header.FileTime.ToString(@"hh\:mm\:ss");
            txtConsignmentFileId.Text = header.ConsignmentFileId;
            txtIssuerId.Text = header.IssuerIdentifier;
            //txtMultosIssuerId.Text = $"0x{header.MultosIssuerId:X8}";
            // Fix MULTOS Issuer ID display (12345678 format)
            txtMultosIssuerId.Text = FormatUInt32ToHex(header.MultosIssuerId);
            txtFileHash.Text = BitConverter.ToString(header.FileHash).Replace("-", " ");
            //txtTemplateCount.Text = header.NumberOfTemplates.ToString();
            // Fix Number of Templates display (0001 format)
            txtTemplateCount.Text = FormatUInt16ToHex(header.NumberOfTemplates);
        }

        private void DisplayTemplateHeader(TemplateHeader header)
        {
            txtIssuerTemplateId.Text = header.IssuerTemplateId;
            txtSoftwareProductId.Text = header.SoftwareProductId;
            //txtKmaHashModulus.Text = $"0x{header.KmaHashModulusId:X4}";
            // Fix KMA Hash Modulus display (0274 format)
            txtKmaHashModulus.Text = FormatUInt16ToHex(header.KmaHashModulusId);
            txtCertificateSerial.Text = BitConverter.ToString(header.CertificateSerialNumber).Replace("-", " ");
            txtProviderKeySetId.Text = $"0x{header.ApplicationProviderPkKeySetId:X2}";
            txtMasterKeyIndex.Text = $"0x{header.IssuerMasterKeyIndex:X2}";
            //txtAluRecordLength.Text = header.AluDataRecordLength.ToString();
            // Fix ALU Record Length display (00002C7B format)
            txtAluRecordLength.Text = FormatUInt32ToHex(header.AluDataRecordLength);
            //txtPdaRecordLength.Text = header.PdaRecordLength.ToString();
            //txtPdaRecordCount.Text = header.NumberOfPdaRecords.ToString();
            //txtSessionDataLength.Text = header.SessionDataLength.ToString();
            // Fix PDA Record Length display (0008 format)
            txtPdaRecordLength.Text = FormatUInt16ToHex(header.PdaRecordLength);

            // Fix PDA Record Count display (0027 format)
            txtPdaRecordCount.Text = FormatUInt16ToHex(header.NumberOfPdaRecords);

            // Fix Session Data Length display (00E5 format)
            txtSessionDataLength.Text = FormatUInt16ToHex(header.SessionDataLength);
        }

       
    }

    public class AltFileHeader
    {
        public string FileTypeCode { get; set; }         // 4 bytes - "CALT"
        public byte FileProtectionMethodId { get; set; } // 1 byte - 0x00
        public byte FileStructureMethodId { get; set; }  // 1 byte - 0x01
        public string ConsignmentId { get; set; }        // 10 bytes ASCII
        public DateTime FileDate { get; set; }           // 4 bytes YYYYMMDD
        public TimeSpan FileTime { get; set; }           // 3 bytes HHMMSS
        public string ConsignmentFileId { get; set; }    // 8 bytes ASCII
        public string IssuerIdentifier { get; set; }     // 4 bytes BCD
        public byte[] Rfu { get; set; }                  // 4 bytes
        public uint MultosIssuerId { get; set; }         // 4 bytes
        public byte[] FileHash { get; set; }             // 20 bytes SHA-1
        public ushort NumberOfTemplates { get; set; }    // 2 bytes
    }

    public class TemplateHeader
    {
        public string IssuerTemplateId { get; set; }     // 8 bytes
        public string SoftwareProductId { get; set; }    // 8 bytes
        public ushort KmaHashModulusId { get; set; }     // 2 bytes
        public byte[] CertificateSerialNumber { get; set; } // 3 bytes
        public byte ApplicationProviderPkKeySetId { get; set; } // 1 byte
        public byte IssuerMasterKeyIndex { get; set; }   // 1 byte
        public uint AluDataRecordLength { get; set; }    // 4 bytes
        public ushort PdaRecordLength { get; set; }      // 2 bytes - 0x0008
        public ushort NumberOfPdaRecords { get; set; }   // 2 bytes
        public ushort SessionDataLength { get; set; }    // 2 bytes
    }

    public class AluDataRecord
    {
        public byte[] McdNumber { get; set; }           // 8 bytes
        public ushort CodeRecordLength { get; set; }    // 2 bytes
        public byte[] CodeRecord { get; set; }          // Variable length
        public ushort DataRecordLength { get; set; }    // 2 bytes
        public byte[] DataRecord { get; set; }          // Variable length
        public ushort DirRecordLength { get; set; }     // 2 bytes
        public byte[] DirRecord { get; set; }           // Variable length
        public ushort FciRecordLength { get; set; }     // 2 bytes
        public byte[] FciRecord { get; set; }           // Variable length
        public ushort AppSignatureLength { get; set; }  // 2 bytes - 0x0000
        public ushort KtuLength { get; set; }           // 2 bytes - 0x0000
        
    }

   
}

   