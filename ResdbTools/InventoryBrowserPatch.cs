using System.Runtime.CompilerServices;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using SkyFrost.Base;
using Record = FrooxEngine.Store.Record;

namespace ResdbTools;

[HarmonyPatch(typeof(InventoryBrowser))]
internal static class InventoryBrowserPatch
{
    [HarmonyPostfix]
    [HarmonyPatch("OnItemSelected")]
    public static void OnItemSelectedPostfix(
        InventoryBrowser __instance,
        BrowserItem currentItem
    )
    {
        if (!ResdbTools.Config!.GetValue<bool>(ResdbTools.Enabled)) return;

        // Don't allow interacting with inventory of other players
        if (!__instance.CanInteract(__instance.LocalUser)) return;

        if (currentItem is not InventoryItemUI currentInventoryItem) return;
        var specialItemType = InventoryBrowser.ClassifyItem(currentInventoryItem);

        Record? record = GetItem(currentInventoryItem) ?? GetDirectory(currentInventoryItem)?.EntryRecord;
        if (record is null) return;

        var buttons = GetButtonsRoot(__instance).Target?[0];
        if (buttons is null) return;

        // Add button
        const string SHOW_RECORD_BUTTON_TAG = "ResdbTools.ShowRecordButton";
        buttons.FindChild(child => child.Tag == SHOW_RECORD_BUTTON_TAG, maxDepth: 0)?.Destroy();
        UIBuilder ui = new(buttons);
        RadiantUI_Constants.SetupDefaultStyle(ui);
        var showRecordButton = ui.Button(
            OfficialAssets.Graphics.Icons.Inspector.Help,
            ModLocaleKeys.InventoryShowRecord
        );
        showRecordButton.Slot.Tag = SHOW_RECORD_BUTTON_TAG;
        showRecordButton.Slot.PersistentSelf = false;
        var dir = __instance.CurrentDirectory;
        showRecordButton.LocalPressed += (button, data) => ShowRecord(__instance, record, dir);

        // Only show button for local user
        var activeOverride = showRecordButton.Slot.AttachComponent<ValueUserOverride<bool>>();
        activeOverride.Default.Value = false;
        activeOverride.SetOverride(__instance.LocalUser, true);
        activeOverride.Target.ForceLink(showRecordButton.Slot.ActiveSelf_Field);
    }

    /// <summary>
    /// Displays a record viewer popup.
    /// </summary>
    private static void ShowRecord(InventoryBrowser browser, Record record, RecordDirectory? dir)
    {
        //Logger.Info(() => $"Record: {JsonConvert.SerializeObject(record)}");
        var overlayManager = browser.Slot.GetComponentInParents<ModalOverlayManager>();
        var title = ModLocaleKeys.RecordEditorTitle(record);
        Slot container;
        if (overlayManager is not null)
        {
            container = overlayManager.OpenModalOverlay(new float2(.4f, .6f), title).Slot;
        }
        else
        {
            var panel = browser.LocalUserSpace.AddSlot("Record Viewer");
            panel.LocalScale *= 0.001f;
            panel.PositionInFrontOfUser(float3.Backward);
            var ui = RadiantUI_Panel.SetupPanel(panel, title, new float2(500, 400), pinButton: false);
            RadiantUI_Constants.SetupEditorStyle(ui);
            container = ui.Root;
        }

        RecordViewer viewer = new(container, dir);
        viewer.Saved += (updatedRecord) =>
        {
            // When the editor is saved, update the inventory
            if (browser.IsDestroyed) return;
            browser.World.RunSynchronously(() =>
            {
                UpdateRecord(browser, updatedRecord);
            });

        };
        browser.World.Coroutines.StartTask(async delegate
        {
            await viewer.FetchRecord(record.GetUrl(browser.Engine.PlatformProfile));
        });
    }

    /// <summary>
    /// Updates the current inventory to reflect changes made to a record.
    /// </summary>
    private static void UpdateRecord(InventoryBrowser browser, Record updatedRecord)
    {
        switch (updatedRecord.RecordType)
        {
            case RecordTypes.DIRECTORY: break;
            case RecordTypes.LINK: break;
            default: return;
        }
        RecordDirectory dir = browser.CurrentDirectory;
        if (dir is null) return;

        // Update record if found
        var subdir = GetSubdirectories(dir).Find(subdir => updatedRecord.IsSameRecord(subdir.EntryRecord));
        if (subdir is null) return;
        RecordDirectoryPatch.SetName(subdir, updatedRecord.Name); // Update name

        // Prevent directory from showing up as an object
        GetRecords(dir).RemoveAll(rec => updatedRecord.IsSameRecord(rec));

        // Rebuild UIX
        var ui = BeginGeneratingNewDirectory(browser, dir, out var folders, out var items, SlideSwapRegion.Slide.None);
        UpdateDirectoryItems(browser, ui, folders, items);
    }

    [HarmonyReversePatch]
    [HarmonyPatch("BeginGeneratingNewDirectory")]
    private static UIBuilder BeginGeneratingNewDirectory(InventoryBrowser instance, RecordDirectory dir, out GridLayout folders, out GridLayout items, SlideSwapRegion.Slide slide)
        => throw new NotImplementedException();

    [HarmonyReversePatch]
    [HarmonyPatch("UpdateDirectoryItems")]
    private static void UpdateDirectoryItems(InventoryBrowser instance, UIBuilder ui, GridLayout folders, GridLayout items)
        => throw new NotImplementedException();

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "Item")]
    private extern static ref Record? GetItem(InventoryItemUI instance);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "Directory")]
    private extern static ref RecordDirectory? GetDirectory(InventoryItemUI instance);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_buttonsRoot")]
    private extern static ref SyncRef<Slot> GetButtonsRoot(BrowserDialog instance);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "subdirectories")]
    private extern static ref List<RecordDirectory> GetSubdirectories(RecordDirectory instance);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "records")]
    private extern static ref List<Record> GetRecords(RecordDirectory instance);
}
