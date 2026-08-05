using System;
using System.IO;

namespace WizardMD.Preview
{
    /// <summary>
    /// Диагностический лог вызовов COM-превью — %TEMP%\wizardmd-preview.log.
    /// Временный инструмент (Фаза 4 diagnose), удалить после починки.
    /// </summary>
    internal static class DebugLog
    {
        private static readonly string[] Paths =
        {
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wizardmd-preview.log"),
            @"D:\GitHub\WizardMD\preview-debug.log"
        };

        public static void Write(string message)
        {
            try
            {
                var line = $"{DateTime.Now:HH:mm:ss.fff} [{Environment.CurrentManagedThreadId}] {message}\r\n";
                foreach (var p in Paths)
                {
                    try { File.AppendAllText(p, line); } catch { }
                }
            }
            catch
            {
                // лог никогда не должен ронять хендлер
            }
        }
    }
}
