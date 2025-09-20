using System;
using System.Reflection;
using System.Threading.Tasks;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using SkyFrost.Base;
using FE = FrooxEngine;
using UIX = FrooxEngine.UIX;

namespace Mod.ResdbTools;

internal sealed class RecordViewer(FE.Slot root)
{
    private readonly FE.Slot slot = root;
    private FE.Store.Record? record = null;
    private UIX.TextField? nameField = null;
    private FE.Sync<Uri>? thumbnail = null;
    private UIX.TextField? newAsset = null;
    public event Action<FE.Store.Record>? Saved;

    public async Task FetchRecord(Uri url)
    {
        slot.World.RunSynchronously(() => BuildLoadingUI());
        var result = await slot.Engine.RecordManager.FetchRecord(url);

        slot.World.RunSynchronously(() =>
        {
            if (!result.IsOK)
            {
                BuildErrorUI(result.Content ?? "Unknown");
                return;
            }

            record = result.Entity;
            BuildEditorUI();
        });
    }

    private UIX.UIBuilder BeginBuildUI()
    {
        slot.DestroyChildren();
        UIX.UIBuilder ui = new(slot);
        FE.RadiantUI_Constants.SetupBaseStyle(ui);
        ui.VerticalLayout(8f, forceExpandHeight: false);
        ui.Style.MinHeight = 32f;
        ui.Style.PreferredHeight = 32f;
        return ui;
    }

    private void BuildLoadingUI()
    {
        var ui = BeginBuildUI();
        ui.Text(ModLocaleKeys.RecordEditorLoading);
    }

    private void BuildErrorUI(string error)
    {
        var ui = BeginBuildUI();
        var text = ui.Text(ModLocaleKeys.RecordEditorError(error));
        text.Color.Value = FE.RadiantUI_Constants.Hero.RED;
    }

    private void BuildEditorUI()
    {
        var ui = BeginBuildUI();
        if (record is null) return;

        nameField = BuildLabeledTextField(ui, ModLocaleKeys.RecordEditorName, record.Name);
        if (record.RecordType == RecordTypes.OBJECT)
        {
            Uri? thumbnailUri = null;
            if (!String.IsNullOrEmpty(record.ThumbnailURI))
            {
                Uri.TryCreate(record.ThumbnailURI, UriKind.Absolute, out thumbnailUri);
            }
            thumbnail = BuildLabeledTextureField(ui, ModLocaleKeys.RecordEditorThumbnail, thumbnailUri);

            newAsset = BuildLabeledTextField(ui, ModLocaleKeys.RecordEditorAsset, "");
            newAsset.Text.Content.OnValueChange += value =>
            {
                var valid = Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri is not null;
                ResdbTools.Logger.Info(() => $"Changed: {uri}");
                newAsset.Text.Color.Value = valid ? FE.RadiantUI_Constants.Hero.GREEN : FE.RadiantUI_Constants.Hero.RED;
            };
        }

        // Copy URL buttons
        ui.Spacer(16f);
        foreach (var action in AvailableCopyActions(record))
        {
            var copyRecordUrlButton = ui.Button(ModLocaleKeys.RecordEditorCopyUrl(action));
            copyRecordUrlButton.LocalPressed += (button, data) => CopyUrl(action);
        }

        {
            var layout = ui.Empty("Spacer").AttachComponent<UIX.LayoutElement>();
            layout.FlexibleHeight.Value = 1f;
            layout.MinHeight.Value = 0f;
            layout.PreferredHeight.Value = 0f;
        }

        ui.Style.PreferredHeight = 48f;
        ui.HorizontalLayout(6f);

        ui.PushStyle();
        ui.Style.ButtonColor = FE.RadiantUI_Constants.MidLight.GREEN;
        var saveButton = ui.Button(ModLocaleKeys.Save);
        saveButton.LocalPressed += (button, data) => button.World.Coroutines.StartTask(async delegate
        {
            await Save();
        });
        ui.PopStyle();

        var cancelButton = ui.Button(ModLocaleKeys.Cancel);
        cancelButton.LocalPressed += (button, data) => Close();
    }

    private void CopyUrl(RecordCopyUrlAction action)
    {
        if (record is null) return;
        var profile = slot.Engine.PlatformProfile;

        var assets = SkyFrost.Base.SkyFrostConfig.SKYFROST_PRODUCTION.AssetInterface;
        if (assets.Cloud is null)
        {
            // For some reason, Resonite doesn't initialize its own asset interface?
            assets.Initialize(Engine.Current.Cloud);
        }

        string result = action switch
        {
            RecordCopyUrlAction.Record => record.GetUrl(profile).ToString(),
            RecordCopyUrlAction.Asset => record.AssetURI,
            RecordCopyUrlAction.Web => record.GetWebUrl(profile).ToString(),
            RecordCopyUrlAction.WebAsset => assets.DBToHttp(new Uri(record.AssetURI), DB_Endpoint.Default).ToString(),
            _ => throw new InvalidOperationException(),
        };
        slot.Engine.InputInterface.Clipboard?.SetText(result);
    }

    private static RecordCopyUrlAction[] AvailableCopyActions(FE.Store.Record record)
    {
        return record.RecordType switch
        {
            RecordTypes.DIRECTORY => [RecordCopyUrlAction.Record],
            RecordTypes.LINK => [RecordCopyUrlAction.Record, RecordCopyUrlAction.Asset],
            RecordTypes.OBJECT => [
                RecordCopyUrlAction.Record,
                RecordCopyUrlAction.Asset,
                record.Tags.Contains(RecordTags.WorldOrb) ? RecordCopyUrlAction.Web : RecordCopyUrlAction.WebAsset,
            ],
            _ => []
        };
    }

    private async Task Save()
    {
        if (record is null) return;

        // Modify record
        if (nameField is not null) record.Name = nameField.Text.Content.Value;
        if (thumbnail?.Value is not null) record.ThumbnailURI = thumbnail.Value.ToString();

        if (newAsset is not null && Uri.TryCreate(newAsset.Text.Content.Value, UriKind.Absolute, out var uri) && uri is not null)
        {
            record.AssetURI = uri.ToString();
        }

        // Remove path characters.
        // Path characters, while technically possible, will result in the inventory freaking out.
        if (record.RecordType == RecordTypes.DIRECTORY && record.Name.Contains('/'))
        {
            record.Name = record.Name.Replace('/', ' ');
        }

        // Save record
        slot.World.RunSynchronously(() => BuildLoadingUI());
        var result = await slot.Engine.RecordManager.SaveRecord(record).ConfigureAwait(continueOnCapturedContext: false);
        if (result.saved)
        {
            Saved?.Invoke(record);
            slot.World.RunSynchronously(() => Close());
        }
        else
        {
            slot.World.RunSynchronously(() => BuildErrorUI("Failed"));
        }
    }

    private void Close()
    {
        var container = slot.GetComponentInParents<FE.IUIContainer>();
        if (container is not null)
        {
            container.CloseContainer();
        }
        else
        {
            FE.ObjectRootExtensions.GetObjectRoot(slot).Destroy();
        }
    }

    private static void BeginBuildLabeledField(UIX.UIBuilder ui, LocaleString label)
    {
        ui.HorizontalLayout(8f);
        ui.Text(label, bestFit: true, alignment: Alignment.MiddleLeft, parseRTF: false);
    }

    private static UIX.TextField BuildLabeledTextField(UIX.UIBuilder ui, LocaleString label, string value)
    {
        BeginBuildLabeledField(ui, label);
        var textField = ui.TextField(value, parseRTF: false);
        textField.Slot.GetComponentOrAttach<UIX.LayoutElement>().MinWidth.Value = 375f;
        ui.NestOut();
        return textField;
    }

    private static FE.Sync<Uri> BuildLabeledTextureField(UIX.UIBuilder ui, LocaleString label, Uri? texture)
    {
        ui.PushStyle();
        ui.Style.MinHeight = 96f;
        BeginBuildLabeledField(ui, label);
        ui.HorizontalLayout(0f, 0f, Alignment.MiddleCenter);
        ui.PopStyle();
        var panel = ui.Panel();
        var panelLayout = panel.Slot.GetComponentOrAttach<UIX.LayoutElement>();
        panelLayout.MinWidth.Value = 80f;
        panelLayout.MinHeight.Value = 80f;
        var button = panel.Slot.AttachComponent<UIX.Button>();
        var staticTexture = panel.Slot.AttachComponent<FE.StaticTexture2D>();
        var image = ui.RawImage(staticTexture, preserveAspect: true);
        image.InteractionTarget.Value = false;
        if (texture is not null) staticTexture.URL.Value = texture;
        ui.NestOut();
        ui.NestOut();
        ui.NestOut();
        button.LocalPressed += (button, data) =>
        {
            FE.Grabber grabber = button.Slot.World.GetLocalUserGrabberWithItems(data.source.Slot);
            ResdbTools.Logger.Info(() => $"Grabber: {grabber.LocalExternallyHeldItem}");
            if (grabber is null) return;
            var held = grabber.LocalExternallyHeldItem ?? grabber.HolderSlot;
            if (held is null) return;
            foreach (var refProxy in held.GetComponentsInChildren<FE.ReferenceProxy>())
            {
                ResdbTools.Logger.Info(() => $"Proxy: {refProxy.Reference.Target?.GetType()}");
                if (refProxy.Reference.Target is FE.StaticTexture2D tex)
                {
                    staticTexture.URL.Value = tex.URL.Value;
                    return;
                }
            }
        };
        return staticTexture.URL;
    }
}

internal enum RecordCopyUrlAction
{
    /// <summary>Record (<pre>resrec://</pre>) URL</summary>
    Record,
    /// <summary>Database (<pre>resdb://</pre>) URL.</summary>
    Asset,
    /// <summary>Web (<pre>https://</pre>) URL</summary>
    Web,
    /// <summary>Web (<pre>https://</pre>) URL to a raw data file</summary>
    WebAsset,
}
