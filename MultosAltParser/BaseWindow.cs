using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shell;

namespace MultosAltParser
{
    public class BaseWindow : Window
    {
        public BaseWindow()
        {
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = false;

            WindowChrome chrome = new WindowChrome
            {
                CaptionHeight = 36,  // Changed from 32 to 36
                ResizeBorderThickness = new Thickness(5),
                CornerRadius = new CornerRadius(0),
                GlassFrameThickness = new Thickness(0),
                UseAeroCaptionButtons = false
            };
            WindowChrome.SetWindowChrome(this, chrome);
        }

        protected void MinimizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
            e.Handled = true;
        }

        //protected void MaximizeClick(object sender, RoutedEventArgs e)
        //{
        //    WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        //    e.Handled = true;
        //}

        protected void MaximizeClick(object sender, RoutedEventArgs e)
        {
            // Set maximum dimensions to respect the taskbar area
            MaxHeight = SystemParameters.WorkArea.Height;
            MaxWidth = SystemParameters.WorkArea.Width;

            // Toggle between Maximized and Normal window states
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            e.Handled = true;
        }


        protected void CloseClick(object sender, RoutedEventArgs e)
        {
            Close();
            e.Handled = true;
        }

    }
}