using System.IO;

public class PdaRecord
{
    // Properties
    public string No { get; set; }
    public string Pda { get; set; }
    //public string Tag { get; set; }
    public string Length { get; set; }
    public string Address { get; set; }
    public string DataSource { get; set; }
    public string DataUsage { get; set; }
    public string DataFormat { get; set; }
    public string InputPresence { get; set; }
    public string LengthCheck { get; set; }
    public string DataCategory { get; set; }
    public string ProfileNo { get; set; }
    public string Interface { get; set; }
    public string ProfileDescription { get; set; }
    private string _tag;

    // Computed properties
    public string PdaEx => Pda.Substring(0, 4);
    public string TagEx => !Tag.Substring(0, 2).Equals("00") ? Tag : Tag.Substring(2, 2);
    public string DataCategoryEx => !DataCategory.Equals("Application data") ? "fci" : "data";
    public string LengthCheckEx => !LengthCheck.Equals("Exact length") ? "max" : "exact";

    public string Tag
    {
        get => _tag;
        set
        {
            _tag = value;
            TagDescription = EmvTagHelper.GetTagDescription(value);
        }
    }
    public string TagDescription { get; set; }



    // Constants
    public const byte PDA_DATA_SOURCE_MASK = 128;  // 0x80
    public const byte PDA_DATA_USAGE_MASK = 64;    // 0x40
    public const byte PDA_INTERFACE_TYPE_MASK = 48; // 0x30
    public const byte PDA_INTERFACE_TYPE_MASK_MCAR2 = 1;
    public const byte PDA_PROFILE_NUMBER_MASK = 15; // 0x0F
    public const byte PDA_PROFILE_NUMBER_MASK_MCAR2 = 6;
    public const byte PDA_DATA_FORMAT_MASK = 128;   // 0x80
    public const byte PDA_INPUT_PRESENCE_MASK = 64; // 0x40
    public const byte PDA_LENGTH_CHECK_MASK = 32;   // 0x20
    public const byte PDA_DATA_CATEGORY_MASK = 8;   // 0x08
    public const byte DATA_CATEGORY_DATA_RECORD = 0;
    public const byte DATA_CATEGORY_FCI_RECORD = 8;
    public const byte DATA_FORMAT_DATA_ONLY = 0;
    public const byte DATA_FORMAT_TLV = 128;

    public static string GetProfileDescription(int profileNo)
    {
        return profileNo switch
        {
            0 => "Profile 0 (Contact Legacy)",
            1 => "Profile 1 (Contactless Legacy)",
            2 => "Profile 2 (Contact Non-Legacy)",
            3 => "Profile 3 (Contactless Non-Legacy)",
            _ => $"Unknown Profile ({profileNo})"
        };
    }

    //// Static method to parse PDA records from a byte array
    //public static List<PdaRecord> ParsePdaRecords(byte[] pdaData, int pdaCount)
    //{
    //    var pdaList = new List<PdaRecord>();

    //    try
    //    {
    //        if (pdaCount == 0 || pdaData == null || pdaData.Length < pdaCount * 8)
    //            return pdaList;

    //        using (var memoryStream = new MemoryStream(pdaData))
    //        using (var reader = new BinaryReader(memoryStream))
    //        {
    //            for (int i = 0; i < pdaCount; i++)
    //            {
    //                byte[] pdaBytes = reader.ReadBytes(8);
    //                var pda = new PdaRecord
    //                {
    //                    No = string.Format("{0:000}", i + 1),
    //                    Pda = BitConverter.ToString(pdaBytes).Replace("-", ""),
    //                    Tag = BitConverter.ToString(pdaBytes, 2, 2).Replace("-", ""),
    //                    Length = BitConverter.ToString(pdaBytes, 4, 2).Replace("-", ""),
    //                    Address = BitConverter.ToString(pdaBytes, 6, 2).Replace("-", ""),
    //                    DataSource = (pdaBytes[0] & PDA_DATA_SOURCE_MASK) == PDA_DATA_SOURCE_MASK ? "System" : "CDF",
    //                    DataUsage = (pdaBytes[0] & PDA_DATA_USAGE_MASK) == PDA_DATA_USAGE_MASK ? "Input to system" : "Input to ALU",
    //                    DataFormat = (pdaBytes[1] & PDA_DATA_FORMAT_MASK) == DATA_FORMAT_TLV ? "TLV format" : "Data only",
    //                    InputPresence = (pdaBytes[1] & PDA_INPUT_PRESENCE_MASK) == PDA_INPUT_PRESENCE_MASK ? "Optional" : "Mandatory",
    //                    LengthCheck = (pdaBytes[1] & PDA_LENGTH_CHECK_MASK) == PDA_LENGTH_CHECK_MASK ? "Maximum length" : "Exact length",
    //                    DataCategory = (pdaBytes[1] & PDA_DATA_CATEGORY_MASK) == DATA_CATEGORY_FCI_RECORD ? "FCI data" : "Application data",
    //                    ProfileNo = (pdaBytes[0] & PDA_PROFILE_NUMBER_MASK).ToString()
    //                };

    //                // Set interface based on interface type mask
    //                pda.Interface = GetInterface((byte)(pdaBytes[0] & PDA_INTERFACE_TYPE_MASK));

    //                pdaList.Add(pda);
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        throw new Exception($"Error parsing PDA records: {ex.Message}");
    //    }

    //    return pdaList;
    //}

    //private static string GetInterface(byte interfaceType)
    //{
    //    switch (interfaceType >> 4)
    //    {
    //        case 0: return "-";
    //        case 1: return "L";
    //        case 2: return "C";
    //        case 3: return "B";
    //        default: return "-";
    //    }
    //}
}
