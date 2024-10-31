using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultosAltParser
{
    public class AluBuilder
    {
        private readonly AltParser _altParser;
        private readonly AluDataRecord _aluData;  // Use the already parsed data
        private byte[] _finalAluData;  // Will contain the final ALU



        // Section sizes from ALT
        private readonly int _mcdNumberSize = 8;  // Fixed 8 bytes
        private readonly ushort _codeSize;
        private readonly ushort _dataSize;
        private readonly ushort _dirSize;
        private readonly ushort _fciSize;
        private readonly ushort _appSignatureSize; // Usually 0x0000
        private readonly ushort _ktuSize;         // Usually 0x0000

        // Track section offsets
        private readonly Dictionary<string, int> _sectionOffsets;

        public AluBuilder(AltParser altParser, AluDataRecord aluData)
        {
            try
            {
                _aluData = aluData ?? throw new ArgumentNullException(nameof(aluData));
                Debug.WriteLine("AluBuilder constructor started");

                // Calculate offsets first
                _sectionOffsets = CalculateSectionOffsets();
                Debug.WriteLine("Section offsets calculated");

                // Initialize ALU data array
                int totalSize = CalculateTotalSize();
                Debug.WriteLine($"Calculated total size: {totalSize}");
                _finalAluData = new byte[totalSize];
                Debug.WriteLine($"Initialized _finalAluData with length: {_finalAluData.Length}");

                // Copy sections
                CopyInitialSections();
                Debug.WriteLine("Initial sections copied");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AluBuilder constructor: {ex.Message}");
                throw;
            }
        }

        // Method to access the final ALU data
        public byte[] GetAluData()
        {
            return _finalAluData;
        }


        private void CopyInitialSections()
        {
            try
            {
                Debug.WriteLine("Starting to copy initial sections...");
                Debug.WriteLine($"_finalAluData length: {_finalAluData.Length}");

                // Debug offsets
                foreach (var offset in _sectionOffsets)
                {
                    Debug.WriteLine($"Offset {offset.Key}: {offset.Value}");
                }

                // 1. MCD Number (8 bytes of zeros)
                byte[] mcdNumber = new byte[8];  // Defaults to zeros
                Debug.WriteLine($"MCD Number offset: {_sectionOffsets["MCD"]}");
                Debug.WriteLine($"MCD Number length: {mcdNumber.Length}");

                try
                {
                    Array.Copy(mcdNumber, 0, _finalAluData, _sectionOffsets["MCD"], 8);
                    Debug.WriteLine("Successfully copied MCD Number");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error copying MCD Number: {ex.Message}");
                    throw;
                }
                // 2. Code Record
                // Write length (2 bytes, big endian)
                WriteUInt16BigEndian(_sectionOffsets["CodeLength"], _aluData.CodeRecordLength);
                // Copy code data
                Array.Copy(_aluData.CodeRecord, 0, _finalAluData, _sectionOffsets["Code"], _aluData.CodeRecordLength);
                Debug.WriteLine($"Copied Code Record section: {_aluData.CodeRecordLength} bytes");

                // 3. Data Record
                WriteUInt16BigEndian(_sectionOffsets["DataLength"], _aluData.DataRecordLength);
                Array.Copy(_aluData.DataRecord, 0, _finalAluData, _sectionOffsets["Data"], _aluData.DataRecordLength);
                Debug.WriteLine($"Copied Data Record section: {_aluData.DataRecordLength} bytes");

                // 4. DIR Record
                WriteUInt16BigEndian(_sectionOffsets["DirLength"], _aluData.DirRecordLength);
                Array.Copy(_aluData.DirRecord, 0, _finalAluData, _sectionOffsets["Dir"], _aluData.DirRecordLength);
                Debug.WriteLine($"Copied DIR Record section: {_aluData.DirRecordLength} bytes");

                // 5. FCI Record
                WriteUInt16BigEndian(_sectionOffsets["FciLength"], _aluData.FciRecordLength);
                Array.Copy(_aluData.FciRecord, 0, _finalAluData, _sectionOffsets["Fci"], _aluData.FciRecordLength);
                Debug.WriteLine($"Copied FCI Record section: {_aluData.FciRecordLength} bytes");

                // 6. Application Signature Length (2 bytes, zeros)
                WriteUInt16BigEndian(_sectionOffsets["AppSignatureLength"], 0);

                // 7. KTU Length (2 bytes, zeros)
                WriteUInt16BigEndian(_sectionOffsets["KtuLength"], 0);

                Debug.WriteLine("Completed copying all initial sections");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CopyInitialSections: {ex.Message}");
                throw new Exception($"Failed to copy initial sections: {ex.Message}", ex);
            }
        }
        private void WriteUInt16BigEndian(int offset, ushort value)
        {
            _finalAluData[offset] = (byte)(value >> 8);
            _finalAluData[offset + 1] = (byte)(value & 0xFF);
        }

        private Dictionary<string, int> CalculateSectionOffsets()
        {
            try
            {
                var offsets = new Dictionary<string, int>();
                int currentOffset = 0;

                // Log initial offset
                Debug.WriteLine($"Starting offset calculation at: {currentOffset}");

                // MCD Number (8 bytes)
                offsets.Add("MCD", currentOffset);
                Debug.WriteLine($"MCD offset: {currentOffset}");
                currentOffset += 8;


                // Code Record
                offsets["CodeLength"] = currentOffset;
                currentOffset += 2;  // 2 bytes for length
                offsets["Code"] = currentOffset;
                currentOffset += _aluData.CodeRecordLength;

                // Data Record
                offsets["DataLength"] = currentOffset;
                currentOffset += 2;  // 2 bytes for length
                offsets["Data"] = currentOffset;
                currentOffset += _aluData.DataRecordLength;

                // DIR Record
                offsets["DirLength"] = currentOffset;
                currentOffset += 2;  // 2 bytes for length
                offsets["Dir"] = currentOffset;
                currentOffset += _aluData.DirRecordLength;

                // FCI Record
                offsets["FciLength"] = currentOffset;
                currentOffset += 2;  // 2 bytes for length
                offsets["Fci"] = currentOffset;
                currentOffset += _aluData.FciRecordLength;

                // Application Signature Length
                offsets["AppSignatureLength"] = currentOffset;
                currentOffset += 2;

                // KTU Length
                offsets["KtuLength"] = currentOffset;
                currentOffset += 2;
                // Add total size
                offsets.Add("Total", currentOffset);

                Debug.WriteLine("Calculated section offsets:");
                foreach (var offset in offsets)
                {
                    Debug.WriteLine($"{offset.Key}: 0x{offset.Value:X4}");
                }

                return offsets;
            }

            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating offsets: {ex.Message}");
                throw;
            }

        }


        private int CalculateTotalSize()
        {
            int totalSize =
                8 +  // MCD Number (fixed 8 bytes)
                2 + _aluData.CodeRecordLength +    // Code Record (2 bytes length field + data)
                2 + _aluData.DataRecordLength +    // Data Record (2 bytes length field + data)
                2 + _aluData.DirRecordLength +     // DIR Record (2 bytes length field + data)
                2 + _aluData.FciRecordLength +     // FCI Record (2 bytes length field + data)
                2 +  // Application Signature Length (2 bytes)
                2;   // KTU Length (2 bytes)

            Debug.WriteLine($"Total ALU size calculation:");
            Debug.WriteLine($"MCD Number: 8 bytes");
            Debug.WriteLine($"Code Record: 2 + {_aluData.CodeRecordLength} bytes");
            Debug.WriteLine($"Data Record: 2 + {_aluData.DataRecordLength} bytes");
            Debug.WriteLine($"DIR Record: 2 + {_aluData.DirRecordLength} bytes");
            Debug.WriteLine($"FCI Record: 2 + {_aluData.FciRecordLength} bytes");
            Debug.WriteLine($"App Signature Length: 2 bytes");
            Debug.WriteLine($"KTU Length: 2 bytes");
            Debug.WriteLine($"Total Size: {totalSize} bytes");

            return totalSize;
        }


        public void InsertPersonalizedData(List<PersonalizationRecord> records)
        {
            foreach (var record in records.Where(r => r.Status == "Ready"))
            {
                try
                {
                    Debug.WriteLine($"\n=== Processing Record ===");
                    Debug.WriteLine($"Tag: {record.Tag}");
                    Debug.WriteLine($"Format: {record.DataFormat}");
                    Debug.WriteLine($"Address: {record.Address}");
                    Debug.WriteLine($"Data: {record.PersonalizationData}");

                    // Convert hex string to bytes (removing spaces)
                    string hexData = record.PersonalizationData.Replace(" ", "");
                    byte[] data = ConvertHexStringToBytes(hexData);
                    Debug.WriteLine($"Data as bytes: {BitConverter.ToString(data)}");

                    // Get base offset based on data category
                    int baseOffset = record.DataCategory == "FCI data" ?
                        _sectionOffsets["Fci"] : _sectionOffsets["Data"];

                    // Calculate target offset
                    int addressOffset = Convert.ToInt32(record.Address, 16);
                    int targetOffset = baseOffset + addressOffset;

                    Debug.WriteLine($"Base offset: 0x{baseOffset:X4}");
                    Debug.WriteLine($"Address offset: 0x{addressOffset:X4}");
                    Debug.WriteLine($"Target offset: 0x{targetOffset:X4}");

                    // Verify existing tag at target location
                    bool isOneByteTag = IsOneByteTag(record.Tag);
                    int tagLength = isOneByteTag ? 1 : 2;

                    byte[] existingTag = new byte[tagLength];
                    Array.Copy(_finalAluData, targetOffset, existingTag, 0, tagLength);
                    Debug.WriteLine($"Existing tag at target: {BitConverter.ToString(existingTag)}");

                    bool isTlv = record.DataFormat?.Equals("TLV format", StringComparison.OrdinalIgnoreCase) ?? false;
                    Debug.WriteLine($"Is TLV format: {isTlv}");

                    if (isTlv)
                    {
                        Debug.WriteLine("Processing as TLV data");
                        InsertTlvData(record, data, targetOffset);
                    }
                    else
                    {
                        Debug.WriteLine("Processing as non-TLV data");
                        Array.Copy(data, 0, _finalAluData, targetOffset, data.Length);
                    }

                    // Verify final data
                    byte[] verificationData = new byte[Math.Min(data.Length + tagLength + 1, _finalAluData.Length - targetOffset)];
                    Array.Copy(_finalAluData, targetOffset, verificationData, 0, verificationData.Length);
                    Debug.WriteLine($"Final data at location: {BitConverter.ToString(verificationData)}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error processing record {record.Tag}: {ex.Message}");
                    throw;
                }
            }
        }

        private void InsertTlvData(PersonalizationRecord record, byte[] value, int offset)
        {
            try
            {
                Debug.WriteLine($"\nTLV Insertion Details for Tag {record.Tag}:");
                Debug.WriteLine($"Target Offset: 0x{offset:X4}");

                // Determine if it's a one-byte or two-byte tag
                bool isOneByteTags = record.Tag.TrimStart('0').Length <= 2;
                int tagLength = isOneByteTags ? 1 : 2;

                Debug.WriteLine($"Tag type: {(isOneByteTags ? "One byte" : "Two bytes")}");
                Debug.WriteLine($"Tag length: {tagLength}");

                // Read current TLV structure to preserve tag bytes
                byte[] existingTag = new byte[tagLength];
                Array.Copy(_finalAluData, offset, existingTag, 0, tagLength);
                Debug.WriteLine($"Existing tag: {BitConverter.ToString(existingTag)}");

                // Calculate length encoding
                int valueLength = value.Length;
                int lengthBytesCount;
                byte[] lengthBytes;

                if (valueLength <= 0x7F)
                {
                    // Single byte length
                    lengthBytesCount = 1;
                    lengthBytes = new byte[] { (byte)valueLength };
                }
                else
                {
                    // Extended length encoding
                    if (valueLength <= 0xFF)
                    {
                        lengthBytesCount = 2;
                        lengthBytes = new byte[] { 0x81, (byte)valueLength };
                    }
                    else if (valueLength <= 0xFFFF)
                    {
                        lengthBytesCount = 3;
                        lengthBytes = new byte[] {
                    0x82,
                    (byte)(valueLength >> 8),
                    (byte)(valueLength & 0xFF)
                };
                    }
                    else
                    {
                        lengthBytesCount = 4;
                        lengthBytes = new byte[] {
                    0x83,
                    (byte)(valueLength >> 16),
                    (byte)(valueLength >> 8),
                    (byte)(valueLength & 0xFF)
                };
                    }
                }

                Debug.WriteLine($"Length bytes: {BitConverter.ToString(lengthBytes)}");

                // Calculate value offset
                int valueOffset = offset + tagLength + lengthBytesCount;
                Debug.WriteLine($"Value will be inserted at offset: 0x{valueOffset:X4}");
                Debug.WriteLine($"Value to insert: {BitConverter.ToString(value)}");

                // Keep existing tag bytes
                // Insert length bytes
                Array.Copy(lengthBytes, 0, _finalAluData, offset + tagLength, lengthBytesCount);
                // Insert value
                Array.Copy(value, 0, _finalAluData, valueOffset, value.Length);

                // Verify the result
                int totalLength = tagLength + lengthBytesCount + value.Length;
                byte[] verificationData = new byte[totalLength];
                Array.Copy(_finalAluData, offset, verificationData, 0, totalLength);
                Debug.WriteLine($"Final TLV structure: {BitConverter.ToString(verificationData)}");

                // Additional verification
                for (int i = 0; i < tagLength; i++)
                {
                    if (_finalAluData[offset + i] != existingTag[i])
                    {
                        throw new Exception($"Tag bytes were modified. Expected: {BitConverter.ToString(existingTag)}, " +
                                          $"Got: {BitConverter.ToString(_finalAluData, offset, tagLength)}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InsertTlvData: {ex.Message}");
                Debug.WriteLine($"Offset: 0x{offset:X4}, Value Length: {value.Length}");
                throw;
            }
        }

        // Helper method to determine tag length
        private bool IsOneByteTag(string tag)
        {
            // Remove leading zeros
            string normalizedTag = tag.TrimStart('0');

            // If the normalized tag length is 1 or 2 characters, it's a one-byte tag
            return normalizedTag.Length <= 2;
        }

        public async Task SaveAluFile(string filename)
        {
            try
            {
                ValidateAluData();

                Debug.WriteLine("\nALU File Structure for saving:");
                Debug.WriteLine($"Total Size: {_finalAluData.Length} bytes");
                Debug.WriteLine($"MCD Number: 8 bytes");
                Debug.WriteLine($"Code Section: {_aluData.CodeRecordLength} bytes");
                Debug.WriteLine($"Data Section: {_aluData.DataRecordLength} bytes");
                Debug.WriteLine($"DIR Section: {_aluData.DirRecordLength} bytes");
                Debug.WriteLine($"FCI Section: {_aluData.FciRecordLength} bytes");
                Debug.WriteLine($"Signature Length: 2 bytes");
                Debug.WriteLine($"KTU Length: 2 bytes");

                // Write the complete ALU file
                await File.WriteAllBytesAsync(filename, _finalAluData);
                Debug.WriteLine($"ALU file saved successfully to: {filename}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving ALU file: {ex.Message}");
                throw new Exception($"Error saving ALU file: {ex.Message}");
            }
        }

        private void ValidateAluData()
        {
            if (_finalAluData == null)
                throw new Exception("ALU data not initialized");

            // Check minimum size requirements
            int minimumSize = _sectionOffsets["Total"];  // Total from our offset calculations
            if (_finalAluData.Length < minimumSize)
                throw new Exception($"ALU data size ({_finalAluData.Length}) is less than minimum required size ({minimumSize})");
        }

        private void UpdateSectionLengths()
        {
            // No need to update lengths as they're fixed from ALT
            // Unless you need to modify them based on personalization
            Debug.WriteLine("Section lengths verified");
        }

        //private void WriteUInt16BigEndian(int offset, ushort value)
        //{
        //    if (offset + 1 >= _aluData.Length)
        //        throw new Exception($"Offset {offset:X4} out of bounds for writing length");

        //    // Write in big-endian format
        //    _aluData[offset] = (byte)(value >> 8);
        //    _aluData[offset + 1] = (byte)(value & 0xFF);
        //}


        private void UpdateAluHeader()
        {
            // Update ALU-specific header fields
            // This would include any differences between ALT and ALU headers
        }

        private byte[] ConvertHexStringToBytes(string hex)
        {
            // Remove any spaces
            hex = hex.Replace(" ", "");

            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hex string must have an even length");

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        // Helper methods for getting offsets
        public int GetCodeOffset() => _sectionOffsets["Code"];
        public int GetDataOffset() => _sectionOffsets["Data"];
        public int GetDirOffset() => _sectionOffsets["Dir"];
        public int GetFciOffset() => _sectionOffsets["Fci"];

    }
}
