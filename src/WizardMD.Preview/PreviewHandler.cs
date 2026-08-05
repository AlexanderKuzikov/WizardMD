using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using WizardMD.Core;
using WizardMD.Preview.Com;

namespace WizardMD.Preview
{
    /// <summary>
    /// COM-превью Markdown для Проводника: IPreviewHandler + IInitializeWithFile.
    /// UI живёт на собственном STA-потоке (скрытая форма + Application.Run),
    /// WebBrowser legacy — WebView2 в процессе Explorer не грузим (AGENTS).
    /// </summary>
    [ComVisible(true)]
    [Guid(PreviewInfo.Clsid)]
    [ProgId(PreviewInfo.ProgId)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public sealed class PreviewHandler : IPreviewHandler, IInitializeWithFile, IObjectWithSite
    {
        private readonly object _sync = new object();
        private readonly ManualResetEventSlim _ready = new ManualResetEventSlim(false);
        private Thread? _uiThread;
        private PreviewForm? _form;
        private string? _filePath;

        public PreviewHandler()
        {
            DebugLog.Write("PreviewHandler ctor");
        }

        public void Initialize(string pszFilePath, uint grfMode)
        {
            DebugLog.Write($"Initialize path={pszFilePath}");
            _filePath = pszFilePath;
        }

        public void SetWindow(IntPtr hwnd, ref RECT rect)
        {
            DebugLog.Write($"SetWindow hwnd=0x{hwnd.ToInt64():X} rect={rect.Left},{rect.Top},{rect.Right},{rect.Bottom}");
            try
            {
                EnsureUi();
                var r = rect;
                var form = _form!;
                form.Invoke((MethodInvoker)delegate
                {
                    NativeMethods.SetParent(form.Handle, hwnd);
                    MoveInto(form, r);
                    form.Show();
                });
                DebugLog.Write("SetWindow done");
            }
            catch (Exception ex)
            {
                DebugLog.Write($"SetWindow EX: {ex}");
                throw;
            }
        }

        public void SetRect(ref RECT rect)
        {
            DebugLog.Write($"SetRect {rect.Left},{rect.Top},{rect.Right},{rect.Bottom}");
            var form = _form;
            if (form == null) return;
            var r = rect;
            form.Invoke((MethodInvoker)delegate { MoveInto(form, r); });
        }

        public void DoPreview()
        {
            DebugLog.Write("DoPreview");
            try
            {
                EnsureUi();
                string html;
                try
                {
                    if (_filePath != null && File.Exists(_filePath))
                    {
                        html = HtmlPage.Build(File.ReadAllText(_filePath), dark: false);
                    }
                    else
                    {
                        html = HtmlPage.Build("> *(WizardMD.Preview: файл не найден)*", dark: false);
                    }
                }
                catch (Exception ex)
                {
                    html = HtmlPage.Build("> **Ошибка чтения файла:**\n\n```\n" + ex.Message + "\n```", dark: false);
                }

                var form = _form!;
                form.Invoke((MethodInvoker)delegate { form.ShowContent(html); });
                DebugLog.Write("DoPreview done");
            }
            catch (Exception ex)
            {
                DebugLog.Write($"DoPreview EX: {ex}");
                throw;
            }
        }

        public void Unload()
        {
            DebugLog.Write("Unload");
            Thread? t;
            PreviewForm? form;
            lock (_sync)
            {
                t = _uiThread;
                form = _form;
                _uiThread = null;
                _form = null;
            }

            if (t != null)
            {
                try
                {
                    if (form != null)
                    {
                        form.Invoke((MethodInvoker)delegate
                        {
                            NativeMethods.SetParent(form.Handle, IntPtr.Zero);
                            form.Close();
                            form.Dispose();
                        });
                    }
                }
                catch (Exception ex)
                {
                    DebugLog.Write($"Unload close EX: {ex.Message}");
                }

                if (!t.Join(TimeSpan.FromSeconds(5)))
                {
                    DebugLog.Write("Unload: поток не завершился за 5с");
                }
            }

            _filePath = null;
            DebugLog.Write("Unload done");
        }

        public void SetFocus()
        {
            DebugLog.Write("SetFocus");
            var form = _form;
            if (form == null) return;
            try
            {
                form.Invoke((MethodInvoker)delegate { form.Focus(); });
            }
            catch (Exception ex)
            {
                DebugLog.Write($"SetFocus EX: {ex.Message}");
            }
        }

        public void QueryFocus(out IntPtr phwnd)
        {
            var form = _form;
            phwnd = form != null && form.IsHandleCreated ? form.Handle : IntPtr.Zero;
            DebugLog.Write($"QueryFocus hwnd=0x{phwnd.ToInt64():X}");
        }

        public uint TranslateAccelerator(ref MSG pmsg)
        {
            // ключи превью не перехватываем — отдаём хосту
            return 1; // S_FALSE
        }

        public void SetSite(object pUnkSite)
        {
            DebugLog.Write($"SetSite {pUnkSite?.GetType().FullName ?? "null"}");
            // сайт не используем — держать ссылку на объект Explorer не нужно
        }

        public void GetSite(ref Guid riid, out object ppvSite)
        {
            ppvSite = null!; // сайт не храним
            DebugLog.Write("GetSite -> null");
        }

        private void EnsureUi()
        {
            lock (_sync)
            {
                if (_form != null) return;

                DebugLog.Write("EnsureUi: создание STA-потока");
                _ready.Reset();
                _uiThread = new Thread(new ThreadStart(delegate
                {
                    try
                    {
                        var form = new PreviewForm();
                        _form = form;
                        form.Show();
                        DebugLog.Write("UI: форма создана и показана");
                        _ready.Set();
                        Application.Run(form);
                        DebugLog.Write("UI: message loop завершён");
                    }
                    catch (Exception ex)
                    {
                        DebugLog.Write($"UI thread EX: {ex}");
                        try { _ready.Set(); } catch { }
                    }
                }))
                {
                    IsBackground = true
                };
                _uiThread.SetApartmentState(ApartmentState.STA);
                _uiThread.Start();

                if (!_ready.Wait(TimeSpan.FromSeconds(10)))
                {
                    throw new COMException("WizardMD.Preview: таймаут создания UI-потока");
                }
            }
        }

        private static void MoveInto(PreviewForm form, RECT r)
        {
            NativeMethods.MoveWindow(
                form.Handle,
                r.Left,
                r.Top,
                Math.Max(1, r.Right - r.Left),
                Math.Max(1, r.Bottom - r.Top),
                true);
        }
    }
}
