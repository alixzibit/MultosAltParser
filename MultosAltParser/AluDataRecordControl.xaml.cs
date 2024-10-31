using System;
using System.Windows.Controls;

namespace MultosAltParser
{
    public partial class AluDataRecordControl : UserControl
    {
        public AluDataRecordControl()
        {
            InitializeComponent();
        }

        public void DisplayAluData(AluDataRecord aluData)
        {
            // MCD Number
            txtMcdNumber.Text = BitConverter.ToString(aluData.McdNumber).Replace("-", " ");

            // Code Record
            txtCodeRecordLength.Text = $"{aluData.CodeRecordLength} (0x{aluData.CodeRecordLength:X4})";
            txtCodeRecord.Text = BitConverter.ToString(aluData.CodeRecord).Replace("-", " ");

            // Data Record
            txtDataRecordLength.Text = $"{aluData.DataRecordLength} (0x{aluData.DataRecordLength:X4})";
            txtDataRecord.Text = BitConverter.ToString(aluData.DataRecord).Replace("-", " ");

            // DIR Record
            txtDirRecordLength.Text = $"{aluData.DirRecordLength} (0x{aluData.DirRecordLength:X4})";
            txtDirRecord.Text = BitConverter.ToString(aluData.DirRecord).Replace("-", " ");

            // FCI Record
            txtFciRecordLength.Text = $"{aluData.FciRecordLength} (0x{aluData.FciRecordLength:X4})";
            txtFciRecord.Text = BitConverter.ToString(aluData.FciRecord).Replace("-", " ");

            // App Signature and KTU Length
            txtAppSignatureLength.Text = $"{aluData.AppSignatureLength} (0x{aluData.AppSignatureLength:X4})";
            txtKtuLength.Text = $"{aluData.KtuLength} (0x{aluData.KtuLength:X4})";
        }

        private string FormatUInt16ToHex(ushort value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToString(bytes).Replace("-", "");
        }
    }
}