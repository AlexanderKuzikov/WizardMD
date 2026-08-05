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
        private static readonly string Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "wizardmd-preview.log");

        public static void Write(string message)
        {
            try
            {
                File.AppendAllText(
                    Path,
                    $"{DateTime.Now:HH:mm:ss.fff} [{Environment.CurrentManagedThreadId}] {message}\r\n");
            }
            catch
            {
                // лог никогда не должен ронять хендлер
            }
        }
    }
}
