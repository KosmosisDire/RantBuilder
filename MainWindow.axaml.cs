using System.IO;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
namespace RantBuilder;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var container = new Canvas();
        var parseButton = new Button();
        parseButton.Content = "Parse";
        parseButton.Click += async (sender, args) =>
        {
            // Get top level from the current control. Alternatively, you can use Window reference instead.
            var topLevel = TopLevel.GetTopLevel(this);

            // Start async operation to open the dialog.
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Text File",
                AllowMultiple = false
            });

            container.Children.Add(new RosNode(System.Uri.UnescapeDataString(files[0].Path.AbsolutePath)));
        };

        container.Children.Add(parseButton);
        Content = container;
    }

}