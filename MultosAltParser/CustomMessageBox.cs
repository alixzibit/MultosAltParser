using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Imaging;

public class CustomMessageBox : Window
{
    private MessageBoxResult _result = MessageBoxResult.None;

    public CustomMessageBox(string message, string title, MessageBoxButton buttons, MessageBoxImage icon)
    {
        Style = (Style)Application.Current.Resources["MessageBoxWindowStyle"];
        Title = title;
        Owner = Application.Current.MainWindow;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var content = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }
            }
        };

        // Message text
        var messageText = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.Resources["TextBrush"],
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(10)
        };

        // Buttons panel
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(10)
        };

        // Add buttons based on MessageBoxButton enum
        switch (buttons)
        {
            case MessageBoxButton.OK:
                AddButton(buttonPanel, "OK", MessageBoxResult.OK, true);
                break;
            case MessageBoxButton.OKCancel:
                AddButton(buttonPanel, "OK", MessageBoxResult.OK, true);
                AddButton(buttonPanel, "Cancel", MessageBoxResult.Cancel, false);
                break;
            case MessageBoxButton.YesNo:
                AddButton(buttonPanel, "Yes", MessageBoxResult.Yes, true);
                AddButton(buttonPanel, "No", MessageBoxResult.No, false);
                break;
            case MessageBoxButton.YesNoCancel:
                AddButton(buttonPanel, "Yes", MessageBoxResult.Yes, true);
                AddButton(buttonPanel, "No", MessageBoxResult.No, false);
                AddButton(buttonPanel, "Cancel", MessageBoxResult.Cancel, false);
                break;
        }

        Grid.SetRow(messageText, 0);
        Grid.SetRow(buttonPanel, 1);

        content.Children.Add(messageText);
        content.Children.Add(buttonPanel);

        Content = content;
    }

    private void AddButton(StackPanel panel, string text, MessageBoxResult result, bool isDefault)
    {
        var button = new Button
        {
            Content = text,
            MinWidth = 100,
            Margin = new Thickness(5, 0, 0, 0),
            IsDefault = isDefault
        };

        button.Click += (s, e) =>
        {
            _result = result;
            DialogResult = true;
            Close();
        };

        panel.Children.Add(button);
    }

    public new MessageBoxResult ShowDialog()
    {
        base.ShowDialog();
        return _result;
    }

    public static MessageBoxResult Show(string message)
    {
        return Show(message, "", MessageBoxButton.OK, MessageBoxImage.None);
    }




    public static MessageBoxResult Show(string message, string title)
    {

        return Show(message, title, MessageBoxButton.OK, MessageBoxImage.None);
    }

    public static MessageBoxResult Show(string message, string title, MessageBoxButton buttons)
    {
        return Show(message, title, buttons, MessageBoxImage.None);
    }

    public static MessageBoxResult Show(string message, string title, MessageBoxButton buttons, MessageBoxImage icon)
    {
        var messageBox = new CustomMessageBox(message, title, buttons, icon);
        return messageBox.ShowDialog();
    }

}
