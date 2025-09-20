using MonkeyLoader.Resonite;
using HarmonyLib;
using FE = FrooxEngine;
using UIX = FrooxEngine.UIX;
using Elements.Core;
using SkyFrost.Base;
using FrooxEngine;

namespace Mod.ResdbTools;

[HarmonyPatchCategory(nameof(InventoryBrowserPatch))]
[HarmonyPatch(typeof(FE.InventoryBrowser))]
internal sealed class InventoryBrowserPatch : ResoniteMonkey<InventoryBrowserPatch>
{
    public override bool CanBeDisabled => true;

    [HarmonyPostfix]
    [HarmonyPatch("OnItemSelected")]
    public static void OnItemSelectedPostfix(
        FE.InventoryBrowser __instance,
        FE.BrowserItem currentItem
    )
    {
        if (!Enabled) return;


        // Don't allow interacting with inventory of other players
        if (!__instance.CanInteract(__instance.LocalUser)) return;

        if (currentItem is not FE.InventoryItemUI currentInventoryItem) return;
        var specialItemType = FE.InventoryBrowser.ClassifyItem(currentInventoryItem);

        var record = currentInventoryItem.Item ?? currentInventoryItem.Directory.EntryRecord;
        if (record is null) return;

        var buttons = __instance._buttonsRoot.Target?[0];
        if (buttons is null) return;

        // Add button
        const string SHOW_RECORD_BUTTON_TAG = "Mod.ResdbTools.ShowRecordButton";
        buttons.FindChild(child => child.Tag == SHOW_RECORD_BUTTON_TAG, maxDepth: 0)?.Destroy();
        UIX.UIBuilder ui = new(buttons);
        FE.RadiantUI_Constants.SetupDefaultStyle(ui);
        var showRecordButton = ui.Button(
            OfficialAssets.Graphics.Icons.Inspector.Help,
            ModLocaleKeys.InventoryShowRecord
        );
        showRecordButton.Slot.Tag = SHOW_RECORD_BUTTON_TAG;
        showRecordButton.Slot.PersistentSelf = false;
        showRecordButton.LocalPressed += (button, data) => ShowRecord(__instance, record);

        // Only show button for local user
        var activeOverride = showRecordButton.Slot.AttachComponent<FE.ValueUserOverride<bool>>();
        activeOverride.Default.Value = false;
        activeOverride.SetOverride(__instance.LocalUser, true);
        activeOverride.Target.ForceLink(showRecordButton.Slot.ActiveSelf_Field);
    }

    /// <summary>
    /// Displays a record viewer popup.
    /// </summary>
    private static void ShowRecord(FE.InventoryBrowser browser, FE.Store.Record record)
    {
        //Logger.Info(() => $"Record: {JsonConvert.SerializeObject(record)}");
        var overlayManager = browser.Slot.GetComponentInParents<FE.ModalOverlayManager>();
        var title = ModLocaleKeys.RecordEditorTitle(record);
        FE.Slot container;
        if (overlayManager is not null)
        {
            container = overlayManager.OpenModalOverlay(new float2(.4f, .6f), title).Slot;
        }
        else
        {
            var panel = browser.LocalUserSpace.AddSlot("Record Viewer");
            panel.LocalScale *= 0.001f;
            panel.PositionInFrontOfUser(float3.Backward);
            var ui = FE.RadiantUI_Panel.SetupPanel(panel, title, new float2(500, 400), pinButton: false);
            FE.RadiantUI_Constants.SetupEditorStyle(ui);
            container = ui.Root;
        }

        RecordViewer viewer = new(container);
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
    private static void UpdateRecord(FE.InventoryBrowser browser, FE.Store.Record updatedRecord)
    {
        switch (updatedRecord.RecordType)
        {
            case RecordTypes.DIRECTORY: break;
            case RecordTypes.LINK: break;
            default: return;
        }
        FE.RecordDirectory dir = browser.CurrentDirectory;
        if (dir is null) return;

        // Update record if found
        var subdir = dir.subdirectories.Find(subdir => updatedRecord.IsSameRecord(subdir.EntryRecord));
        if (subdir is null) return;
        subdir.Name = updatedRecord.Name; // Update name

        // Prevent directory from showing up as an object
        dir.records.RemoveAll(rec => updatedRecord.IsSameRecord(rec));

        // Rebuild UIX
        var ui = browser.BeginGeneratingNewDirectory(dir, out var folders, out var items, UIX.SlideSwapRegion.Slide.None);
        browser.UpdateDirectoryItems(ui, folders, items);
    }
}