using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Xml.Linq;
using System.Diagnostics;

namespace MultosAltParser
{
    public class KeyData
    {
        public Dictionary<string, string> KeyValues { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public class KeyDataParser
    {
        // Use the same tags as defined in IsKeyData
        public static readonly HashSet<string> KeyTags = new HashSet<string>
    {
        "DF42", "DF43", "DF44", "DF5E",
        "DF60", "DF61", "9F46", "0090",
    };

        public async Task<KeyData> ParseKeyXml(string filename)
        {
            try
            {
                var keyData = new KeyData();
                XDocument doc = await XDocument.LoadAsync(
                    new FileStream(filename, FileMode.Open),
                    LoadOptions.None,
                    CancellationToken.None);

                // Add null checks and better error handling
                if (doc.Root == null)
                    throw new Exception("Invalid XML: Root element not found");

                // Directly look for Key elements without assuming a KeyData parent
                var keyElements = doc.Root.Elements("Key");
                if (!keyElements.Any())
                    throw new Exception("No Key elements found in XML");

                foreach (var element in keyElements)
                {
                    var tagAttribute = element.Attribute("Tag");
                    if (tagAttribute == null)
                    {
                        Debug.WriteLine("Warning: Key element found without Tag attribute");
                        continue;
                    }

                    string tag = tagAttribute.Value;
                    if (!string.IsNullOrEmpty(tag))
                    {
                        string normalizedTag = tag.TrimStart('0');

                        // Check against the normalized and original tag
                        if (KeyTags.Contains(normalizedTag) || KeyTags.Contains(tag))
                        {
                            string keyValue = element.Value?.Trim();
                            if (!string.IsNullOrEmpty(keyValue))
                            {
                                // Store the value using the original tag format
                                keyData.KeyValues[tag] = keyValue; // Use the original tag here
                                Debug.WriteLine($"Loaded key for tag: {tag}"); // Log the original tag
                            }
                            else
                            {
                                Debug.WriteLine($"Warning: Empty key value for tag: {tag}");
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"Warning: Unrecognized key tag: {normalizedTag}");
                        }
                    }
                }

                if (!keyData.KeyValues.Any())
                    throw new Exception("No valid keys found in XML file");

                return keyData;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error parsing key file: {ex.Message}");
                throw new Exception($"Error parsing key file: {ex.Message}", ex);
            }
        }
    }
}
