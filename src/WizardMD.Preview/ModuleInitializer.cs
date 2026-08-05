using System;
using System.IO;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// net48 не содержит ModuleInitializerAttribute (C# 9+). Объявляем его сами —
    /// компилятор распознаёт атрибут по имени и генерирует вызов из модуля.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class ModuleInitializerAttribute : Attribute
    {
    }
}

namespace WizardMD.Preview
{
    /// <summary>
    /// Модульный инициализатор: позволяет отличить «CLR вообще не загрузила
    /// сборку» от «класс не создаётся». Временный диагностический инструмент.
    /// </summary>
    internal static class ModuleInitializer
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        internal static void Init()
        {
            try
            {
                var pid = System.Diagnostics.Process.GetCurrentProcess().Id;
                var line = $"{DateTime.Now:HH:mm:ss.fff} [{Environment.CurrentManagedThreadId}] MODULE LOADED pid={pid}\r\n";
                var path = Path.Combine(Path.GetTempPath(), "wizardmd-preview.log");
                File.AppendAllText(path, line);
            }
            catch
            {
                // лог никогда не должен ронять хендлер
            }
        }
    }
}