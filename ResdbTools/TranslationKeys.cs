using Elements.Core;
using SkyFrost.Base;
using Record = FrooxEngine.Store.Record;

namespace ResdbTools;

/// <summary>
/// Translation key constants.
/// </summary>
internal static class ModLocaleKeys
{
    private const string INVENTORY = "Mod.ResdbTools.Inventory";
    private const string RECORD_EDITOR = "Mod.ResdbTools.RecordEditor";

    /// <summary>
    /// Translation key for the inventory button label that is showing the record for the selected item.
    /// </summary>
    public static LocaleString InventoryShowRecord { get; } = $"{INVENTORY}.ShowRecord".AsLocaleKey();

    /// <summary>
    /// Translation key shown when a record is being fetched.
    /// </summary>
    public static LocaleString RecordEditorLoading { get; } = $"{RECORD_EDITOR}.Loading".AsLocaleKey();

    /// <summary>
    /// Translation key shown for the name of a record.
    /// </summary>
    public static LocaleString RecordEditorName { get; } = $"{RECORD_EDITOR}.Name".AsLocaleKey();

    /// <summary>
    /// Translation key shown for the icon/thumbnail of a record.
    /// </summary>
    public static LocaleString RecordEditorThumbnail { get; } = $"{RECORD_EDITOR}.Thumbnail".AsLocaleKey();

    /// <summary>
    /// Translation key shown for the (new) asset of a record.
    /// </summary>
    public static LocaleString RecordEditorAsset { get; } = $"{RECORD_EDITOR}.Asset".AsLocaleKey();

    /// <summary>
    /// Translation key shown when fetching or saving a record failed.
    /// </summary>
    public static LocaleString RecordEditorError(string message) => $"{RECORD_EDITOR}.Error".AsLocaleKey("message", message);

    private static readonly LocaleString RecordEditorCopyUrlRecord = $"{RECORD_EDITOR}.CopyRecordUrl".AsLocaleKey();
    private static readonly LocaleString RecordEditorCopyUrlAsset = $"{RECORD_EDITOR}.CopyAssetUrl".AsLocaleKey();
    private static readonly LocaleString RecordEditorCopyUrlWeb = $"{RECORD_EDITOR}.CopyWebUrl".AsLocaleKey();
    private static readonly LocaleString RecordEditorCopyUrlWebAsset = $"{RECORD_EDITOR}.CopyWebAssetUrl".AsLocaleKey();

    /// <summary>
    /// Translation key for copying a record (<pre>resrec://</pre>) URL.
    /// </summary>
    public static LocaleString RecordEditorCopyUrl(RecordCopyUrlAction action) => (action switch
    {
        RecordCopyUrlAction.Record => RecordEditorCopyUrlRecord,
        RecordCopyUrlAction.Asset => RecordEditorCopyUrlAsset,
        RecordCopyUrlAction.Web => RecordEditorCopyUrlWeb,
        RecordCopyUrlAction.WebAsset => RecordEditorCopyUrlWebAsset,
        _ => throw new ArgumentException("Invalid RecordCopyUrlAction")
    });

    /// <summary>
    /// Official Resonite locale key for saving something.
    /// </summary>
    public static LocaleString Save { get; } = "General.Save".AsLocaleKey();

    /// <summary>
    /// Official Resonite locale key for canceling an action.
    /// </summary>
    public static LocaleString Cancel { get; } = "General.Cancel".AsLocaleKey();

    private static readonly LocaleString RecordEditorTitleDirectory = $"{RECORD_EDITOR}.Title.Directory".AsLocaleKey();
    private static readonly LocaleString RecordEditorTitleLink = $"{RECORD_EDITOR}.Title.Link".AsLocaleKey();
    private static readonly LocaleString RecordEditorTitleWorld = $"{RECORD_EDITOR}.Title.World".AsLocaleKey();
    private static readonly LocaleString RecordEditorTitleObject = $"{RECORD_EDITOR}.Title.Object".AsLocaleKey();

    /// <summary>
    /// Translation key for the title of the record editor.
    /// </summary>
    public static LocaleString RecordEditorTitle(Record record) => (record.RecordType switch
    {
        RecordTypes.DIRECTORY => RecordEditorTitleDirectory,
        RecordTypes.LINK => RecordEditorTitleLink,
        RecordTypes.WORLD => RecordEditorTitleWorld,
        _ => RecordEditorTitleObject,
    });
}