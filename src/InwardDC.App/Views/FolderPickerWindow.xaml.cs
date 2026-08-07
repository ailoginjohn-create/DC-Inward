using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace InwardDC.App.Views;

/// <summary>A lightweight WPF-only folder browser used instead of the WinForms dialog.</summary>
public partial class FolderPickerWindow : Window
{
    private sealed class FolderNode
    {
        public FolderNode(string path)
        {
            Path = path;
            Name = string.IsNullOrEmpty(path)
                ? "This PC"
                : System.IO.Path.GetFileName(path.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar));
            if (string.IsNullOrEmpty(Name))
                Name = path;
        }

        public string Path { get; }
        public string Name { get; }

        public override string ToString() => Name;
    }

    public string? SelectedPath { get; private set; }

    public FolderPickerWindow(string? initialPath = null)
    {
        InitializeComponent();

        var nodes = new List<FolderNode>();
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.IsReady)
                nodes.Add(new FolderNode(drive.RootDirectory.FullName));
        }

        DataContext = new { RootNodes = nodes };
        SelectedPath = initialPath;
    }

    private void OnExpanded(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is TreeViewItem item && item.ItemsSource is null)
        {
            var node = item.DataContext as FolderNode;
            if (node is null)
                return;

            var children = new List<FolderNode>();
            try
            {
                foreach (var dir in Directory.GetDirectories(node.Path))
                {
                    var info = new DirectoryInfo(dir);
                    if ((info.Attributes & FileAttributes.Hidden) == 0 && (info.Attributes & FileAttributes.System) == 0)
                        children.Add(new FolderNode(dir));
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (IOException)
            {
            }

            item.ItemsSource = children;
        }
    }

    private void OnSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is FolderNode node)
            SelectedPath = node.Path;
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(SelectedPath))
        {
            MessageBox.Show("Select a folder first.", "Select Folder", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        DialogResult = true;
        Close();
    }
}
