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

        public void Initialize(string pszFilePath, uint grfMode)
        {
            _filePath = pszFilePath;
        }

        public void SetWindow(IntPtr hwnd, ref RECT rect)
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
        }

        public void SetRect(ref RECT rect)
        {
            var form = _form;
            if (form == null) return;
            var r = rect;
            form.Invoke((MethodInvoker)delegate { MoveInto(form, r); });
        }

        public void DoPreview()
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
        }

        public void Unload()
        {
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
                    if (form != null) form.Invoke((MethodInvoker)form.Close);
                }
                catch (Exception)
                {
                    // форма могла быть уже закрыта/разрушена — поток background, процесс сам завершит
                }

                if (!t.Join(TimeSpan.FromSeconds(5)))
                {
                    // не завершился — background-поток умрёт вместе с процессом Explorer
                }
            }

            _filePath = null;
        }

        public void SetFocus()
        {
            var form = _form;
            if (form == null) return;
            try
            {
                form.Invoke((MethodInvoker)delegate { form.Focus(); });
            }
            catch (Exception)
            {
                // ignore
            }
        }

        public void QueryFocus(out IntPtr phwnd)
        {
            var form = _form;
            phwnd = form != null && form.IsHandleCreated ? form.Handle : IntPtr.Zero;
        }

        public uint TranslateAccelerator(ref MSG pmsg)
        {
            // ключи превью не перехватываем — отдаём хосту
            return 1; // S_FALSE
        }

        public void SetSite(object pUnkSite)
        {
            // сайт не используем — держать ссылку на объект Explorer не нужно
        }

        public void GetSite(ref Guid riid, out object ppvSite)
        {
            ppvSite = null!; // сайт не храним
        }

        private void EnsureUi()
        {
            lock (_sync)
            {
                if (_form != null) return;

                _ready.Reset();
                _uiThread = new Thread(new ThreadStart(delegate
                {
                    var form = new PreviewForm();
                    _form = form;
                    form.Show();
                    _ready.Set();
                    Application.Run(form);
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
