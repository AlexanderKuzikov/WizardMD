using System;
using System.IO;
using System.Windows;
using WizardMD.Core;

namespace WizardMD.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var args = e.Args;

        if (args.Length > 0 && args[0].StartsWith("--"))
        {
            var exitCode = HandleCommand(args);
            Shutdown(exitCode);
            return;
        }

        string? file = args.Length > 0 ? args[0] : null;
        new MainWindow(file).Show();
    }

    private static int HandleCommand(string[] args)
    {
        try
        {
            switch (args[0])
            {
                case "--register-preview":
                {
                    var dll = FindPreviewDll(args);
                    PreviewRegistration.Register(dll);
                    MessageBox.Show(
                        $"COM-превью зарегистрировано:\n{dll}\n\nПерезапустите Проводник (explorer.exe), чтобы применить.",
                        "WizardMD", MessageBoxButton.OK, MessageBoxImage.Information);
                    return 0;
                }
                case "--unregister-preview":
                    PreviewRegistration.Unregister();
                    MessageBox.Show(
                        "COM-превью удалено из реестра. Перезапустите Проводник, чтобы применить.",
                        "WizardMD", MessageBoxButton.OK, MessageBoxImage.Information);
                    return 0;
                case "--register":
                    FileAssociation.Register();
                    MessageBox.Show(
                        "Ассоциация .md → WizardMD зарегистрирована (OpenWithProgids + ProgID, без переопределения текущего редактора по умолчанию).",
                        "WizardMD", MessageBoxButton.OK, MessageBoxImage.Information);
                    return 0;
                case "--unregister":
                    FileAssociation.Unregister();
                    MessageBox.Show("Ассоциация .md → WizardMD удалена.", "WizardMD", MessageBoxButton.OK, MessageBoxImage.Information);
                    return 0;
                case "--unblock":
                {
                    if (args.Length < 2)
                    {
                        MessageBox.Show("Использование: --unblock <путь\\файл>", "WizardMD", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return 1;
                    }
                    Motw.Unblock(args[1]);
                    return 0;
                }
                case "--unblock-register":
                    Motw.RegisterContextMenu();
                    MessageBox.Show("Пункт «Разблокировать» добавлен в контекстное меню .md.", "WizardMD", MessageBoxButton.OK, MessageBoxImage.Information);
                    return 0;
                case "--unblock-unregister":
                    Motw.UnregisterContextMenu();
                    MessageBox.Show("Пункт «Разблокировать» удалён из контекстного меню .md.", "WizardMD", MessageBoxButton.OK, MessageBoxImage.Information);
                    return 0;
                default:
                    MessageBox.Show(
                        $"Неизвестная команда: {args[0]}\n\nДоступно:\n--register-preview [путь\\WizardMD.Preview.dll]\n--unregister-preview\n--register\n--unregister\n--unblock <файл>\n--unblock-register\n--unblock-unregister",
                        "WizardMD", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return 1;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "WizardMD", MessageBoxButton.OK, MessageBoxImage.Error);
            return 1;
        }
    }

    private static string FindPreviewDll(string[] args)
    {
        if (args.Length > 1 && File.Exists(args[1]))
        {
            return args[1];
        }

        var srcDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var previewBin = Path.Combine(srcDir, "WizardMD.Preview", "bin");
        foreach (var cfg in new[] { "Debug", "Release" })
        {
            var candidate = Path.Combine(previewBin, cfg, "net48", PreviewInfo.ProgId + ".dll");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return args.Length > 1 ? args[1] : Path.Combine(previewBin, "WizardMD.Preview.dll");
    }
}
