using System;
using System.Runtime.InteropServices;

namespace WizardMD.Preview
{
    /// <summary>
    /// Минимальный COM-класс для диагностики mscoree-активации в нативных
    /// процессах (cscript/Explorer): БЕЗ зависимостей WinForms/Core.
    /// Временный, удалить после починки.
    /// </summary>
    [ComVisible(true)]
    [Guid("F1C0A1B2-0000-4000-8000-000000000012")]
    public sealed class MinimalCom
    {
        public string Ping() => "pong";
    }
}