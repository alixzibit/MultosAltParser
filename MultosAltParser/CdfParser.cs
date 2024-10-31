using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Xml.Linq;

namespace MultosAltParser
{
    public class CardholderData
    {
        public string Pan { get; set; }                  // Multiple instances: 005A
        public string CardholderName { get; set; }       // 5F20
        public string ExpiryDate { get; set; }           // Multiple instances: 5F24
        public string ServiceCode { get; set; }          // 5F30
        public string PanSequenceNumber { get; set; }    // 5F34
        public string Track1Data { get; set; }           // 9F1F
        public string Track2Data { get; set; }           // Multiple instances: 0057
        public string PinBlock { get; set; }             // DF45 (Optional)
        public string DF47 { get; set; }                 // Optional
    }

    public class CdfParser
    {
        public readonly Dictionary<string, (string Property, Func<string, string> Converter)> _tagMapping =
            new Dictionary<string, (string, Func<string, string>)>
        {
        // Tag -> (Property name, Conversion function)
        { "5A", ("Pan", ConvertPanToHex) },
        { "5F20", ("CardholderName", ConvertNameToHex) },
        { "5F24", ("ExpiryDate", ConvertDateToHex) },
        { "5F30", ("ServiceCode", ConvertServiceCodeToHex) },
        { "5F34", ("PanSequenceNumber", ConvertPsnToHex) },
        { "9F1F", ("Track1Data", ConvertTrack1ToHex) },
        { "57", ("Track2Data", ReturnTrack2Asis) },
        { "DF45", ("PinBlock", ConvertPinBlockToHex) },
        { "DF47", ("DF47", ReturnDf47) }
        };

        public async Task<CardholderData> ParseCdfXml(string filename)
        {
            try
            {
                XDocument doc = await XDocument.LoadAsync(
                    new FileStream(filename, FileMode.Open),
                    LoadOptions.None,
                    CancellationToken.None);

                var cardholderData = new CardholderData();
                var root = doc.Element("CardholderData");
                if (root == null)
                    throw new Exception("Invalid CDF format: Missing CardholderData root element");

                // Parse each field
                foreach (var element in root.Elements())
                {
                    var property = typeof(CardholderData).GetProperty(element.Name.LocalName);
                    if (property != null)
                    {
                        property.SetValue(cardholderData, element.Value);
                    }
                }

                return cardholderData;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing CDF file: {ex.Message}", ex);
            }
        }

        public string GetPersonalizationData(string tag, CardholderData cdfData, string length)
        {
            string normalizedTag = tag.TrimStart('0');

            if (_tagMapping.TryGetValue(normalizedTag, out var mapping))
            {
                var propertyInfo = typeof(CardholderData).GetProperty(mapping.Property);
                if (propertyInfo != null)
                {
                    string value = (string)propertyInfo.GetValue(cdfData);
                    if (!string.IsNullOrEmpty(value))
                    {
                        // Convert the value using the appropriate conversion function
                        return mapping.Converter(value);
                    }
                }
            }

            return null;
        }

        public string ConvertToHex(string value, string tagId)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            switch (tagId)
            {
                case "5A": // PAN
                case "57": // Track 2
                    return ConvertNumericToHex(value);

                case "5F20": // Cardholder Name
                    return ConvertAsciiToHex(value.PadRight(26));

                case "5F24": // Expiry Date
                case "5F25": // Effective Date
                    return ConvertDateToHex(value);

                case "5F34": // PSN
                    return ConvertNumericToHex(value.PadLeft(2, '0'));

                default:
                    return ConvertAsciiToHex(value);
            }
        }

        private string ConvertAsciiToHex(string ascii)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(ascii);
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        private string ConvertNumericToHex(string numeric)
        {
            // Remove any spaces or separators
            numeric = new string(numeric.Where(char.IsDigit).ToArray());

            // Pad with F if odd length
            if (numeric.Length % 2 != 0)
                numeric += "F";

            // Convert each pair of digits to hex
            StringBuilder hex = new StringBuilder();
            for (int i = 0; i < numeric.Length; i += 2)
            {
                hex.Append(numeric.Substring(i, 2));
            }
            return hex.ToString();
        }

        //private string ConvertDateToHex(string date)
        //{
        //    // Expecting YYMM format
        //    if (date.Length != 4)
        //        throw new ArgumentException("Date must be in YYMM format");

        //    return ConvertNumericToHex(date);
        //}

        private static string ConvertPanToHex(string pan)
        {
            // Remove any spaces or separators
            pan = new string(pan.Where(char.IsDigit).ToArray());
            return ConvertNumericToHex(pan, true);  // Pad with F if needed
        }

        private static string ConvertNameToHex(string name)
        {
            // Convert to uppercase and pad with spaces to fixed length
            name = name.ToUpper().PadRight(26);
            return BitConverter.ToString(Encoding.ASCII.GetBytes(name)).Replace("-", "");
        }

        private static string ConvertDateToHex(string date)
        {
            // Convert YYMMDD to hex
            if (date.Length != 6)
                throw new ArgumentException("Date must be in YYMMDD format");
            return ConvertNumericToHex(date, false);
        }

        private static string ConvertServiceCodeToHex(string code)
        {
            if (code.Length != 4)
                throw new ArgumentException("Service Code must be 4 digits");
            return ConvertNumericToHex(code, false);
        }

        private static string ConvertTrack1ToHex(string track1)
        {
            return track1; // Already in hex format from input
        }

        private static string ReturnTrack2Asis(string track2)
        {
            return track2; // Already in hex format from input
        }
        private static string ReturnDf47(string df47)
        {
            return df47; // Already in hex format from input
        }

        private static string ConvertTrack2ToHex(string track2)
        {
            // Handle track 2 format with 'D' as separator
            track2 = track2.Replace("D", "=");
            return ConvertNumericToHex(track2, true);
        }

        private static string ConvertPsnToHex(string psn)
        {
            return ConvertNumericToHex(psn.PadLeft(2, '0'), false);
        }

        private static string ConvertPinBlockToHex(string pinBlock)
        {
            // Optional field - might need specific handling
            return pinBlock;
        }

        private static string ConvertNumericToHex(string numeric, bool padWithF)
        {
            numeric = new string(numeric.Where(c => char.IsDigit(c) || c == '=').ToArray());
            if (padWithF && numeric.Length % 2 != 0)
                numeric += "F";

            return numeric;
        }
    }

   
    
}
