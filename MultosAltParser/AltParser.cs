using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultosAltParser
{
    public class AltParser
    {
        private readonly BinaryReader _reader;
        private bool _isFileHeaderParsed = false;
        private bool _isTemplateHeaderParsed = false;
        private bool _isAluDataParsed = false;
        private bool _isPdaRecordsParsed = false;
        private List<PdaRecord> _pdaRecords;


        public AltFileHeader FileHeader { get; private set; }
        public TemplateHeader TemplateHeader { get; private set; }
        public byte[] RawData { get; private set; }

        // Add properties for the various sections
        public byte[] Code { get; private set; }
        public ushort CodeSize { get; private set; }
        public byte[] Data { get; private set; }
        public ushort DataSize { get; private set; }
        public byte[] DirFileRec { get; private set; }
        public ushort DirFileRecSize { get; private set; }
        public byte[] FciRec { get; private set; }
        public ushort FciRecSize { get; private set; }
        public byte[] PdaRec { get; private set; }
        public ushort PdaRecSize { get; private set; }
        public long DataRecordStartPosition { get; private set; }
        public long FciRecordStartPosition { get; private set; }







        private ushort ReadUInt16BigEndian()
        {
            byte[] bytes = _reader.ReadBytes(2);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToUInt16(bytes, 0);
        }


        public AltParser(string filePath)
        {
            // Read entire file into memory
            RawData = File.ReadAllBytes(filePath);
            _reader = new BinaryReader(File.OpenRead(filePath));
        }

       

        public AltFileHeader ParseFileHeader()
        {
            var header = new AltFileHeader();

            // Read file type code - "CALT"
            byte[] fileTypeBytes = _reader.ReadBytes(4);
            header.FileTypeCode = System.Text.Encoding.ASCII.GetString(fileTypeBytes);

            if (header.FileTypeCode != "CALT")
                throw new InvalidDataException("Invalid ALT file - Expected 'CALT' header");

            header.FileProtectionMethodId = _reader.ReadByte();
            header.FileStructureMethodId = _reader.ReadByte();

            // Read Consignment ID (10 bytes ASCII)
            byte[] consignmentBytes = _reader.ReadBytes(10);
            header.ConsignmentId = System.Text.Encoding.ASCII.GetString(consignmentBytes);

            // Read File Date (4 bytes binary, not BCD)
            byte[] dateBytes = _reader.ReadBytes(4);
            // Convert binary date bytes to DateTime
            int year = dateBytes[0] * 256 + dateBytes[1];
            int month = dateBytes[2];
            int day = dateBytes[3];
            header.FileDate = new DateTime(year, month, day);

            // Read File Time (3 bytes binary, not BCD)
            byte[] timeBytes = _reader.ReadBytes(3);
            header.FileTime = new TimeSpan(timeBytes[0], timeBytes[1], timeBytes[2]);

            // Read Consignment File ID (8 bytes ASCII)
            byte[] consignmentFileBytes = _reader.ReadBytes(8);
            header.ConsignmentFileId = System.Text.Encoding.ASCII.GetString(consignmentFileBytes);

            // Read Issuer Identifier (4 bytes BCD)
            byte[] issuerIdBytes = _reader.ReadBytes(4);
            header.IssuerIdentifier = BitConverter.ToString(issuerIdBytes).Replace("-", "");

            header.Rfu = _reader.ReadBytes(4);
            header.MultosIssuerId = _reader.ReadUInt32();
            header.FileHash = _reader.ReadBytes(20);
            header.NumberOfTemplates = _reader.ReadUInt16();

            FileHeader = header;
            _isFileHeaderParsed = true;

            return header;
        }

        public TemplateHeader ParseTemplateHeader()
        {
            var header = new TemplateHeader();

            // Read Issuer Template ID (8 bytes)
            byte[] issuerTemplateBytes = _reader.ReadBytes(8);
            header.IssuerTemplateId = BitConverter.ToString(issuerTemplateBytes).Replace("-", "");

            // Read Software Product ID (8 bytes)
            byte[] softwareProductBytes = _reader.ReadBytes(8);
            header.SoftwareProductId = BitConverter.ToString(softwareProductBytes).Replace("-", "");

            header.KmaHashModulusId = _reader.ReadUInt16();
            header.CertificateSerialNumber = _reader.ReadBytes(3);
            header.ApplicationProviderPkKeySetId = _reader.ReadByte();
            header.IssuerMasterKeyIndex = _reader.ReadByte();
            header.AluDataRecordLength = _reader.ReadUInt32();
            header.PdaRecordLength = ReadUInt16BigEndian();//_reader.ReadUInt16();
            header.NumberOfPdaRecords = ReadUInt16BigEndian();
            header.SessionDataLength = _reader.ReadUInt16();

            TemplateHeader = header;
            _isTemplateHeaderParsed = true;
            return header;
        }



        public AluDataRecord ParseAluDataRecord()
        {
            var aluData = new AluDataRecord();

            try
            {
                Debug.WriteLine($"Before MCD: Position={_reader.BaseStream.Position}, Length={_reader.BaseStream.Length}");

                // Validate and read MCD Number
                if (_reader.BaseStream.Length - _reader.BaseStream.Position < 8)
                    throw new EndOfStreamException("Not enough data for MCD Number");
                aluData.McdNumber = _reader.ReadBytes(8);

                Debug.WriteLine($"After MCD: Position={_reader.BaseStream.Position}, Length={_reader.BaseStream.Length}");

                // Read CodeRecordLength in big-endian
                if (_reader.BaseStream.Length - _reader.BaseStream.Position < 2)
                    throw new EndOfStreamException("Not enough data for CodeRecordLength");

                aluData.CodeRecordLength = ReadUInt16BigEndian();
                Debug.WriteLine($"CodeRecordLength: {aluData.CodeRecordLength:X4} ({aluData.CodeRecordLength} bytes)");

                // Validate and read Code Record
                if (_reader.BaseStream.Length - _reader.BaseStream.Position < aluData.CodeRecordLength)
                    throw new EndOfStreamException($"Not enough data for CodeRecord (need {aluData.CodeRecordLength} bytes)");

                aluData.CodeRecord = _reader.ReadBytes(aluData.CodeRecordLength);
                DataRecordStartPosition = _reader.BaseStream.Position;
                Debug.WriteLine($"After CodeRecord: Position={_reader.BaseStream.Position}, Length={_reader.BaseStream.Length}");

                // Read DataRecordLength in big-endian
                if (_reader.BaseStream.Length - _reader.BaseStream.Position < 2)
                    throw new EndOfStreamException("Not enough data for DataRecordLength");

                aluData.DataRecordLength = ReadUInt16BigEndian();
                Debug.WriteLine($"DataRecordLength: {aluData.DataRecordLength:X4} ({aluData.DataRecordLength} bytes)");

                // Validate and read Data Record
                if (_reader.BaseStream.Length - _reader.BaseStream.Position < aluData.DataRecordLength)
                    throw new EndOfStreamException($"Not enough data for DataRecord (need {aluData.DataRecordLength} bytes)");

                aluData.DataRecord = _reader.ReadBytes(aluData.DataRecordLength);
                Debug.WriteLine($"After DataRecord: Position={_reader.BaseStream.Position}, Length={_reader.BaseStream.Length}");

                // Read DirRecordLength in big-endian
                if (_reader.BaseStream.Length - _reader.BaseStream.Position < 2)
                    throw new EndOfStreamException("Not enough data for DirRecordLength");

                aluData.DirRecordLength = ReadUInt16BigEndian();
                Debug.WriteLine($"DirRecordLength: {aluData.DirRecordLength:X4} ({aluData.DirRecordLength} bytes)");

                // Validate and read DIR Record
                if (_reader.BaseStream.Length - _reader.BaseStream.Position < aluData.DirRecordLength)
                    throw new EndOfStreamException($"Not enough data for DirRecord (need {aluData.DirRecordLength} bytes)");

                aluData.DirRecord = _reader.ReadBytes(aluData.DirRecordLength);
                FciRecordStartPosition = _reader.BaseStream.Position;
                Debug.WriteLine($"After DirRecord: Position={_reader.BaseStream.Position}, Length={_reader.BaseStream.Length}");

                // Read FciRecordLength in big-endian
                if (_reader.BaseStream.Length - _reader.BaseStream.Position < 2)
                    throw new EndOfStreamException("Not enough data for FciRecordLength");

                aluData.FciRecordLength = ReadUInt16BigEndian();
                Debug.WriteLine($"FciRecordLength: {aluData.FciRecordLength:X4} ({aluData.FciRecordLength} bytes)");

                // Validate and read FCI Record
                if (_reader.BaseStream.Length - _reader.BaseStream.Position < aluData.FciRecordLength)
                    throw new EndOfStreamException($"Not enough data for FciRecord (need {aluData.FciRecordLength} bytes)");

                aluData.FciRecord = _reader.ReadBytes(aluData.FciRecordLength);
                Debug.WriteLine($"After FciRecord: Position={_reader.BaseStream.Position}, Length={_reader.BaseStream.Length}");

                // Read App Signature Length and KTU Length in big-endian
                if (_reader.BaseStream.Length - _reader.BaseStream.Position < 4)
                    throw new EndOfStreamException("Not enough data for AppSignature and KTU lengths");

                aluData.AppSignatureLength = ReadUInt16BigEndian();
                aluData.KtuLength = ReadUInt16BigEndian();

                // Read PDA Records if present
                if (TemplateHeader.NumberOfPdaRecords > 0)
                {
                    PdaRecSize = (ushort)(TemplateHeader.NumberOfPdaRecords * 8);

                    if (_reader.BaseStream.Length - _reader.BaseStream.Position < PdaRecSize)
                        throw new EndOfStreamException($"Not enough data for PDA Records (need {PdaRecSize} bytes)");

                    PdaRec = _reader.ReadBytes(PdaRecSize);
                    Debug.WriteLine($"Read {PdaRec.Length} bytes of PDA data");
                    if (PdaRec.Length >= 8)
                    {
                        Debug.WriteLine($"First PDA record: {BitConverter.ToString(PdaRec, 0, 8)}");
                    }
                }

                return aluData;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing ALU data record at position {_reader.BaseStream.Position}: {ex.Message}", ex);
            }
        }


        public bool ParseAlt(string filePath, ref string errorMessage)
        {
            try
            {
                // Parse File Header
                FileHeader = ParseFileHeader();

                // Parse Template Header
                TemplateHeader = ParseTemplateHeader();

                // Parse ALU Data sections
                ParseAluDataSections();

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }


        public void EnsureAluDataParsed()
        {
            try
            {
                if (!_isAluDataParsed)
                {
                    // If we haven't parsed sections yet, parse them
                    ParseAluDataSections();
                }
                else
                {
                    Debug.WriteLine("ALU data already parsed, using cached values");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in EnsureAluDataParsed: {ex.Message}");
                throw;
            }
        }

            public bool HasValidSections()
        {
            EnsureAluDataParsed();
            return CodeSize > 0 && DataSize > 0 &&
                   DirFileRecSize > 0 && FciRecSize > 0 &&
                   Code != null && Data != null &&
                   DirFileRec != null && FciRec != null;
        }

        private void ParseAluDataSections()
        {
            if (_isAluDataParsed) return;
            if (!_isTemplateHeaderParsed || TemplateHeader == null)
                throw new InvalidOperationException("Template Header must be parsed before parsing ALU data sections");

            try
            {
                // Store current position
                long currentPosition = _reader.BaseStream.Position;
                Debug.WriteLine($"Current stream position: {currentPosition}");

                // Reset to start of ALU data sections if needed
                if (currentPosition >= _reader.BaseStream.Length)
                {
                    Debug.WriteLine("Stream position at end, resetting to ALU data start position");
                    _reader.BaseStream.Position = 106;  // Position after MCD
                }
                Debug.WriteLine($"\nSection Starting Positions:");
                Debug.WriteLine($"Before Code Record: Position={_reader.BaseStream.Position}");

                // Read Code Record
                CodeSize = ReadUInt16BigEndian();
                Code = _reader.ReadBytes(CodeSize);
                Debug.WriteLine($"Before Data Record: Position={_reader.BaseStream.Position}");

                // Read Data Record
                DataSize = ReadUInt16BigEndian();
                Data = _reader.ReadBytes(DataSize);
                Debug.WriteLine($"Before DIR Record: Position={_reader.BaseStream.Position}");

                // Read DIR Record
                DirFileRecSize = ReadUInt16BigEndian();
                DirFileRec = _reader.ReadBytes(DirFileRecSize);
                Debug.WriteLine($"Before FCI Record: Position={_reader.BaseStream.Position}");

                // Read FCI Record
                FciRecSize = ReadUInt16BigEndian();
                FciRec = _reader.ReadBytes(FciRecSize);
                Debug.WriteLine($"Before PDA Records: Position={_reader.BaseStream.Position}");
                _isAluDataParsed = true;
                // Skip 4 padding bytes before PDA records
                _reader.ReadBytes(4);
                Debug.WriteLine($"After padding, before PDA Records: Position={_reader.BaseStream.Position}");

                // Read PDA Records if present
                if (TemplateHeader.NumberOfPdaRecords > 0)
                {
                    Debug.WriteLine($"Reading {TemplateHeader.NumberOfPdaRecords} PDA records ({TemplateHeader.NumberOfPdaRecords * 8} bytes)");
                    PdaRecSize = (ushort)(TemplateHeader.NumberOfPdaRecords * 8); // Each PDA record is 8 bytes
                    PdaRec = _reader.ReadBytes(PdaRecSize);
                    Debug.WriteLine($"PDA Records read: {(PdaRec != null ? PdaRec.Length : 0)} bytes");

                    // Add verification of first PDA record
                    if (PdaRec != null && PdaRec.Length >= 8)
                    {
                        Debug.WriteLine($"First PDA record bytes: {BitConverter.ToString(PdaRec, 0, 8)}");
                        // Should show: 03-80-00-5A-00-0A-15-4E
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ParseAluDataSections: {ex.Message}");
                throw;
            }

            _isAluDataParsed = true;
            }


        //FormatUInt16ToHex
        private string FormatUInt16ToHex(ushort value)
        {
            // Convert to byte array and reverse for big-endian
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        public List<PdaRecord> ParsePdaRecords()
        {
            // If already parsed, return cached records
            if (_isPdaRecordsParsed && _pdaRecords != null)
                return _pdaRecords;

            // Ensure prerequisites are met
            if (TemplateHeader == null)
            {
                throw new InvalidOperationException("Template Header must be parsed before parsing PDA records");
            }

            // Debug information
            Debug.WriteLine($"PDA Record Count (hex): {FormatUInt16ToHex(TemplateHeader.NumberOfPdaRecords)}");
            Debug.WriteLine($"PDA Record Count (decimal): {TemplateHeader.NumberOfPdaRecords}");
            Debug.WriteLine($"PDA Record data initialized: {(PdaRec != null ? "Yes" : "No")}");
            if (PdaRec != null)
            {
                Debug.WriteLine($"PDA Record data length: {PdaRec.Length}");
                Debug.WriteLine($"First 8 bytes of PDA data: {BitConverter.ToString(PdaRec, 0, Math.Min(8, PdaRec.Length))}");
            }

            _pdaRecords = new List<PdaRecord>();

            if (TemplateHeader.NumberOfPdaRecords == 0 || PdaRec == null)
            {
                Debug.WriteLine("No PDAs found in the template.");
                return _pdaRecords;
            }

            try
            {
                // Verify expected total size
                int expectedSize = TemplateHeader.NumberOfPdaRecords * 8;
                if (PdaRec.Length < expectedSize)
                {
                    throw new InvalidOperationException(
                        $"PDA Record data too short. Expected {expectedSize} bytes, but have {PdaRec.Length}");
                }

                for (int i = 0; i < TemplateHeader.NumberOfPdaRecords; i++)
                {
                    int offset = i * 8;
                    Debug.WriteLine($"Parsing PDA record {i + 1} at offset {offset:X4}");
                    Debug.WriteLine($"PDA bytes: {BitConverter.ToString(PdaRec, offset, 8)}");

                    var pda = new PdaRecord
                    {
                        No = string.Format("{0:000}", i + 1),
                        Pda = BitConverter.ToString(PdaRec, offset, 8).Replace("-", ""),
                        Tag = BitConverter.ToString(PdaRec, offset + 2, 2).Replace("-", ""),
                        Length = BitConverter.ToString(PdaRec, offset + 4, 2).Replace("-", ""),
                        Address = BitConverter.ToString(PdaRec, offset + 6, 2).Replace("-", ""),
                        DataSource = ((PdaRec[offset] & 0x80) == 0x80) ? "System" : "CDF",
                        DataUsage = ((PdaRec[offset] & 0x40) == 0x40) ? "Input to system" : "Input to ALU",
                        DataFormat = ((PdaRec[offset + 1] & 0x80) == 0x80) ? "TLV format" : "Data only",
                        InputPresence = ((PdaRec[offset + 1] & 0x40) == 0x40) ? "Optional" : "Mandatory",
                        LengthCheck = ((PdaRec[offset + 1] & 0x20) == 0x20) ? "Maximum length" : "Exact length",
                        DataCategory = ((PdaRec[offset + 1] & 0x08) == 0x08) ? "FCI data" : "Application data",
                        ProfileNo = (PdaRec[offset] & 0x0F).ToString(),
                        ProfileDescription = PdaRecord.GetProfileDescription(PdaRec[offset] & 0x0F),
                        Interface = GetInterface((byte)(PdaRec[offset] & 0x30))
                    };

                    _pdaRecords.Add(pda);
                    Debug.WriteLine($"Parsed PDA {pda.No}: Tag={pda.Tag}, Address={pda.Address}");
                }

                _isPdaRecordsParsed = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error parsing PDA records: {ex.Message}");
                throw;
            }

            return _pdaRecords;
        }

        

        private string GetInterface(int interfaceType)  // Changed parameter type to int
        {
            switch (interfaceType >> 4)
            {
                case 0: return "-";
                case 1: return "L";
                case 2: return "C";
                case 3: return "B";
                default: return "-";
            }
        }





        public void Dispose()
        {
            _reader?.Dispose();
        }



    }
}
