namespace WizardMD.Core;

/// <summary>
/// Идентификаторы COM-превью WizardMD.Preview — единый источник правды
/// для регистратора (App) и COM-класса (Preview). Не менять без перерегистрации.
/// </summary>
public static class PreviewInfo
{
    public const string Clsid = "48A5B98A-BFE6-4E21-9CAA-876A31963DC2";
    public const string AppId = "72D320C1-5FBC-407B-9807-2B41A92C4153";
    public const string ProgId = "WizardMD.Preview";
    public const string DisplayName = "WizardMD Markdown Preview";
    public const string ClassName = "WizardMD.Preview.PreviewHandler";
    public const string IPreviewHandlerIid = "{8895B1C6-B41F-4C1C-A562-0D564250836F}";
}
