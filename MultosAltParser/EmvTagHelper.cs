using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using System.Xml;

public class EmvTagHelper
{
    private static Dictionary<string, string> _tagDescriptions;

    public static void LoadTagDescriptions()
    {
        _tagDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // Load embedded resource
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream("MultosAltParser.EMVTagList.xml"))
            using (XmlReader reader = XmlReader.Create(stream))
            {
                var doc = XDocument.Load(reader);
                foreach (var tagElement in doc.Root.Elements("Tag"))
                {
                    string tag = tagElement.Attribute("Tag").Value;
                    string description = tagElement.Attribute("Description").Value;
                    _tagDescriptions[tag] = description;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading EMV tags: {ex.Message}");
        }
    }

    public static string GetTagDescription(string tag)
    {
        if (_tagDescriptions == null)
        {
            LoadTagDescriptions();
        }

        // Remove leading zeros and try to match
        string normalizedTag = tag.TrimStart('0');

        if (_tagDescriptions.TryGetValue(normalizedTag, out string description))
        {
            return description;
        }

        return "Unknown Tag";
    }
}