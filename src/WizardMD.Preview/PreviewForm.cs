using System;
using System.Windows.Forms;

namespace WizardMD.Preview
{
    /// <summary>
    /// Контейнер превью: скрытая форма без рамки + WebBrowser (MSHTML legacy).
    /// Живёт на собственном STA-потоке с message loop — не блокирует Explorer.
    /// </summary>
    internal sealed class PreviewForm : Form
    {
        private readonly WebBrowser _browser = new WebBrowser
        {
            Dock = DockStyle.Fill,
            AllowNavigation = false,
            AllowWebBrowserDrop = false,
            ScriptErrorsSuppressed = true,
            ScrollBarsEnabled = true
        };

        public PreviewForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            ControlBox = false;
            StartPosition = FormStartPosition.Manual;
            Controls.Add(_browser);
        }

        public void ShowContent(string html)
        {
            DebugLog.Write("ShowContent: загрузка DocumentText, html.Length=" + html.Length);
            try
            {
                _browser.Stop();
                _browser.DocumentText = html;
                DebugLog.Write("ShowContent done");
            }
            catch (Exception ex)
            {
                DebugLog.Write($"ShowContent EX: {ex}");
                throw;
            }
        }

        public void StopLoading()
        {
            try
            {
                _browser.Stop();
            }
            catch
            {
                // ignore
            }
        }
    }
}
