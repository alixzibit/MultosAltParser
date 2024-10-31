using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace MultosAltParser
{
    public class PdaRowBackgroundConverter : IValueConverter
    {
        private static readonly HashSet<string> HighlightedTags = new HashSet<string>
        {
            "DF42",
            "DF43",
            "DF44",
            "DF5E",
            //"DF5F",
            "DF60",
            "DF61",
            "9F46",
            //"008F",
            "0090",
            "90",
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PdaRecord pda)
            {
                // First priority: Highlight specific tags
                if (HighlightedTags.Contains(pda.Tag.TrimStart('0')))
                {
                    return new SolidColorBrush(Colors.DarkSlateBlue);
                }

                // Second priority: Highlight CDF records
                if (pda.DataSource == "CDF")
                {
                    return new SolidColorBrush(Colors.DarkGreen);
                }
            }

            return null; // Default background
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

