using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace WizardMD.App;

/// <summary>
/// Ассоциация .md → WizardMD.App: ProgID + OpenWithProgids (HKCU, без админа).
/// ProgID по умолчанию для .md не переопределяется — текущий редактор не трогаем.
/// </summary>
public static class FileAssociation
{
    private const string MdKey = @"Software\Classes\.md";
    private const string ProgIdKey = @"Software\Classes\WizardMD.md";

    public static void Register()
    {
        var exe = Path.Combine(AppContext.BaseDirectory, "WizardMD.exe");
        if (!File.Exists(exe))
        {
            throw new FileNotFoundException($"WizardMD.exe не найден: {exe}", exe);
        }

        using (var md = Registry.CurrentUser.CreateSubKey(MdKey))
        {
            using (var openWith = md.CreateSubKey("OpenWithProgids"))
            {
                openWith.SetValue("WizardMD.md", "");
            }
        }

        using (var progId = Registry.CurrentUser.CreateSubKey(ProgIdKey))
        {
            progId.SetValue("", "Markdown (WizardMD)");
            using (var icon = progId.CreateSubKey("DefaultIcon"))
            {
                icon.SetValue("", $"\"{exe}\",0");
            }

            using (var shell = progId.CreateSubKey(@"shell\open\command"))
            {
                shell.SetValue("", $"\"{exe}\" \"%1\"");
            }
        }
    }

    public static void Unregister()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(ProgIdKey, throwOnMissingSubKey: false);
            using (var md = Registry.CurrentUser.OpenSubKey(MdKey, writable: true))
            {
                md?.DeleteValue("WizardMD.md", throwOnMissingValue: false);
                using var openWith = md?.OpenSubKey("OpenWithProgids", writable: true);
                openWith?.DeleteValue("WizardMD.md", throwOnMissingValue: false);
            }
        }
        catch (Exception)
        {
            // ignore
        }
    }
}
