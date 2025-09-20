using System.ComponentModel;
using Elements.Core;
using SkyFrost.Base;
using FE = FrooxEngine;

namespace Mod.ResdbTools;

/// <summary>
/// Translation key constants.
/// </summary>
internal static class ModLocaleKeys
{
    /// <summary>
    /// Translation key for the inventory button label that is showing the record for the selected item.
    /// </summary>
    public static LocaleString InventoryShowRecord { get; } = "Mod.ResdbTools.Inventory.ShowRecord".AsLocaleKey();

    /// <summary>
    /// Translation key shown when a record is being fetched.
    /// </summary>
    public static LocaleString RecordEditorLoading { get; } = "Mod.ResdbTools.RecordEditor.Loading".AsLocaleKey();

    /// <summary>
    /// Translation key shown for the name of a record.
    /// </summary>
    public static LocaleString RecordEditorName { get; } = "Mod.ResdbTools.RecordEditor.Name".AsLocaleKey();

    /// <summary>
    /// Translation key shown for the icon/thumbnail of a record.
    /// </summary>
    public static LocaleString RecordEditorThumbnail { get; } = "Mod.ResdbTools.RecordEditor.Thumbnail".AsLocaleKey();

    /// <summary>
    /// Translation key shown for the (new) asset of a record.
    /// </summary>
    public static LocaleString RecordEditorAsset { get; } = "Mod.ResdbTools.RecordEditor.Asset".AsLocaleKey();

    /// <summary>
    /// Translation key shown when fetching or saving a record failed.
    /// </summary>
    public static LocaleString RecordEditorError(string message) => "Mod.ResdbTools.RecordEditor.Error".AsLocaleKey("message", message);

    /// <summary>
    /// Translation key for copying a record (<pre>resrec://</pre>) URL.
    /// </summary>
    public static LocaleString RecordEditorCopyUrl(RecordCopyUrlAction action) => (action switch
    {
        RecordCopyUrlAction.Record => "Mod.ResdbTools.RecordEditor.CopyRecordUrl",
        RecordCopyUrlAction.Asset => "Mod.ResdbTools.RecordEditor.CopyAssetUrl",
        RecordCopyUrlAction.Web => "Mod.ResdbTools.RecordEditor.CopyWebUrl",
        RecordCopyUrlAction.WebAsset => "Mod.ResdbTools.RecordEditor.CopyWebAssetUrl",
        _ => throw new InvalidEnumArgumentException()
    }).AsLocaleKey();

    /// <summary>
    /// Official Resonite locale key for saving something.
    /// </summary>
    public static LocaleString Save { get; } = "General.Save".AsLocaleKey();

    /// <summary>
    /// Official Resonite locale key for canceling an action.
    /// </summary>
    public static LocaleString Cancel { get; } = "General.Cancel".AsLocaleKey();

    /// <summary>
    /// Translation key for the title of the record editor.
    /// </summary>
    public static LocaleString RecordEditorTitle(FE.Store.Record record) => (record.RecordType switch
    {
        RecordTypes.DIRECTORY => "Mod.ResdbTools.RecordEditor.Title.Directory",
        RecordTypes.LINK => "Mod.ResdbTools.RecordEditor.Title.Link",
        RecordTypes.WORLD => "Mod.ResdbTools.RecordEditor.Title.World",
        _ => "Mod.ResdbTools.RecordEditor.Title.Object",
    }).AsLocaleKey();
}