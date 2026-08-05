using System.IO;
using Microsoft.Win32;

namespace WizardMD.App;

/// <summary>
/// Снятие MOTW (Zone.Identifier) с файлов: --unblock &lt;файл&gt; и пункт
/// «Разблокировать» в контекстном меню всех файлов (HKCU, без админа).
/// MOTW ставится браузером при скачивании и блокирует превью в панели
/// Проводника (неподписанные хендлеры). Снятие метки — единственный способ
/// показать превью без подписи хендлера.
/// </summary>
public static class Motw
{
    private const string MdShellKey = @"Software\Classes\*\shell\WizardMDUnblock";
    private const string MdShellCommand = MdShellKey + @"\command";
    private const string MdShellIcon = MdShellKey + @"\icon";
    private const string LegacyMdKey = @"Software\Classes\.md\shell\WizardMDUnblock";

    public static void Unblock(string path)
    {
        path = Path.GetFullPath(path);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Файл не найден: {path}", path);
        }

        DeleteZoneStream(path);
    }

    public static void UnblockDirectory(string dir)
    {
        dir = Path.GetFullPath(dir);
        if (!Directory.Exists(dir))
        {
            throw new DirectoryNotFoundException($"Папка не найдена: {dir}");
        }

        foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
        {
            DeleteZoneStream(file);
        }
    }

    private static void DeleteZoneStream(string file)
    {
        try
        {
            File.Delete(file + ":Zone.Identifier");
        }
        catch
        {
            // файл без MOTW или readonly — не критично
        }
    }

    public static void RegisterContextMenu()
    {
        var exe = Path.Combine(AppContext.BaseDirectory, "WizardMD.exe");
        if (!File.Exists(exe))
        {
            throw new FileNotFoundException($"WizardMD.exe не найден: {exe}", exe);
        }

        // убрать старую .md-запись, если осталась от прошлой версии
        DeleteKeyTree(LegacyMdKey);

        using (var shell = Registry.CurrentUser.CreateSubKey(MdShellKey))
        {
            shell.SetValue("", "Разблокировать для превью");
        }
        using (var icon = Registry.CurrentUser.CreateSubKey(MdShellIcon))
        {
            icon.SetValue("", $"\"{exe}\",0");
        }
        using (var command = Registry.CurrentUser.CreateSubKey(MdShellCommand))
        {
            command.SetValue("", $"\"{exe}\" --unblock \"%1\"");
        }
    }

    public static void UnregisterContextMenu()
    {
        DeleteKeyTree(MdShellKey);
        DeleteKeyTree(LegacyMdKey);
    }

    private static void DeleteKeyTree(string path)
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(path, throwOnMissingSubKey: false);
        }
        catch
        {
            // ignore
        }
    }
}
