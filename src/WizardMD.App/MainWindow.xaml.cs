using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace WizardMD.App;

public partial class MainWindow : Window
{
    private string? _currentFile;
    private string? _currentMarkdown;
    private bool _dark;

    public MainWindow() : this(null)
    {
    }

    public MainWindow(string? filePath)
    {
        InitializeComponent();
        _currentFile = filePath;
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, (_, _) => OpenButton_Click(null!, null!)));
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await WebView.EnsureCoreWebView2Async();
        if (_currentFile != null && File.Exists(_currentFile))
        {
            OpenFile(_currentFile);
        }
        else
        {
            Render("## WizardMD\n\nОткройте `.md` файл через **Открыть…** (Ctrl+O) или перетащите его в окно.");
        }
    }

    private void OpenFile(string path)
    {
        try
        {
            _currentFile = Path.GetFullPath(path);
            _currentMarkdown = File.ReadAllText(path);
            Title = $"{Path.GetFileName(path)} — WizardMD";
            FilePathText.Text = _currentFile;
            Render(_currentMarkdown);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось открыть файл:\n{ex.Message}", "WizardMD", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Render(string markdown)
    {
        _currentMarkdown = markdown;
        WebView.NavigateToString(MarkdownPage.Build(markdown, _dark));
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Открыть Markdown",
            Filter = "Markdown (*.md;*.markdown;*.mdown)|*.md;*.markdown;*.mdown|Все файлы (*.*)|*.*"
        };
        if (dlg.ShowDialog(this) == true)
        {
            OpenFile(dlg.FileName);
        }
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        _dark = !_dark;
        ThemeButton.Content = _dark ? "Светлая тема" : "Тёмная тема";
        if (_currentMarkdown != null) Render(_currentMarkdown);
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0 && File.Exists(files[0]))
        {
            OpenFile(files[0]);
        }
        e.Handled = true;
    }
}
