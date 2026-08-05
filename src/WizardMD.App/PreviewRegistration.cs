using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using WizardMD.Core;

namespace WizardMD.App;

/// <summary>
/// Ручная регистрация COM-превью в HKCU (без админа): mscoree + Assembly/Class/CodeBase,
/// shellex на .md, PreviewHandlers. Работает через `--register-preview` / `--unregister-preview`.
/// </summary>
public static class PreviewRegistration
{
    private const string Clsid = "{" + PreviewInfo.Clsid + "}";
    private const string AppId = "{" + PreviewInfo.AppId + "}";
    private const string ClsidRoot = @"Software\Classes\CLSID\" + Clsid;
    private const string AppIdRoot = @"Software\Classes\AppID\" + AppId;
    private const string ProgIdRoot = @"Software\Classes\" + PreviewInfo.ProgId;
    private const string MdShellex = @"Software\Classes\.md\shellex\" + PreviewInfo.IPreviewHandlerIid;
    private const string PreviewHandlers = @"Software\Microsoft\Windows\CurrentVersion\PreviewHandlers";

    public static void Register(string dllPath)
    {
        dllPath = Path.GetFullPath(dllPath);
        if (!File.Exists(dllPath))
        {
            throw new FileNotFoundException(
                $"WizardMD.Preview.dll не найден: {dllPath}\nУкажите путь аргументом: --register-preview <путь\\WizardMD.Preview.dll>",
                dllPath);
        }

        var assemblyName = AssemblyName.GetAssemblyName(dllPath).FullName;
        var mscoree = Path.Combine(Environment.SystemDirectory, "mscoree.dll");
        var codeBase = new Uri(dllPath).AbsoluteUri;

        using (var clsid = Registry.CurrentUser.CreateSubKey(ClsidRoot))
        {
            clsid.SetValue("", PreviewInfo.DisplayName);
            clsid.SetValue("AppID", AppId);
            clsid.SetValue("EnablePreviewHandler", 1);
            clsid.SetValue("AutomaticallyPreviewUntrustedFiles", 1);
            using (var inproc = clsid.CreateSubKey("InprocServer32"))
            {
                inproc.SetValue("", mscoree);
                inproc.SetValue("Assembly", assemblyName);
                inproc.SetValue("Class", PreviewInfo.ClassName);
                inproc.SetValue("CodeBase", codeBase);
                inproc.SetValue("ThreadingModel", "Both");
            }
        }

        using (var appId = Registry.CurrentUser.CreateSubKey(AppIdRoot))
        {
            appId.SetValue("", PreviewInfo.DisplayName);
            appId.SetValue("DllSurrogate", @"%SystemRoot%\system32\prevhost.exe", RegistryValueKind.ExpandString);
        }

        using (var progId = Registry.CurrentUser.CreateSubKey(ProgIdRoot))
        {
            progId.SetValue("", PreviewInfo.DisplayName);
            using (var clsidLink = progId.CreateSubKey("CLSID"))
            {
                clsidLink.SetValue("", PreviewInfo.Clsid);
            }
        }

        using (var shellex = Registry.CurrentUser.CreateSubKey(MdShellex))
        {
            shellex.SetValue("", Clsid);
        }

        using (var handlers = Registry.CurrentUser.CreateSubKey(PreviewHandlers))
        {
            handlers.SetValue(Clsid, PreviewInfo.DisplayName);
        }
    }

    public static void Unregister()
    {
        DeleteKey(ClsidRoot);
        DeleteKey(AppIdRoot);
        DeleteKey(ProgIdRoot);
        DeleteKey(MdShellex);
        using (var handlers = Registry.CurrentUser.OpenSubKey(PreviewHandlers, writable: true))
        {
            handlers?.DeleteValue(Clsid, throwOnMissingValue: false);
        }
    }

    private static void DeleteKey(string path)
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
