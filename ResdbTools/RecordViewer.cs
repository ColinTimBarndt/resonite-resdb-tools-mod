using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using SkyFrost.Base;
using Record = FrooxEngine.Store.Record;

namespace ResdbTools;

internal sealed class RecordViewer(Slot root, RecordDirectory? directory)
{
    private readonly Slot _slot = root;
    private readonly RecordDirectory? _directory = directory;
    private Record? _record = null;
    private TextField? _nameField = null;
    private Sync<Uri>? _thumbnail = null;
    private TextField? _newAsset = null;
    public event Action<Record>? Saved;

    public async Task FetchRecord(Uri url)
    {
        _slot.World.RunSynchronously(() => BuildLoadingUI());
        var result = await _slot.Engine.RecordManager.FetchRecord(url);

        _slot.World.RunSynchronously(() =>
        {
            if (!result.IsOK)
            {
                BuildErrorUI(result.Content ?? "Unknown");
                return;
            }

            _record = result.Entity;
            BuildEditorUI();
        });
    }

    private UIBuilder BeginBuildUI()
    {
        _slot.DestroyChildren();
        UIBuilder ui = new(_slot);
        RadiantUI_Constants.SetupBaseStyle(ui);
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
        text.Color.Value = RadiantUI_Constants.Hero.RED;
    }

    private void BuildEditorUI()
    {
        var ui = BeginBuildUI();
        if (_record is null) return;

        _nameField = BuildLabeledTextField(ui, ModLocaleKeys.RecordEditorName, _record.Name);
        if (_record.RecordType == RecordTypes.OBJECT)
        {
            Uri? thumbnailUri = null;
            if (!String.IsNullOrEmpty(_record.ThumbnailURI))
            {
                Uri.TryCreate(_record.ThumbnailURI, UriKind.Absolute, out thumbnailUri);
            }
            _thumbnail = BuildLabeledTextureField(ui, ModLocaleKeys.RecordEditorThumbnail, thumbnailUri);

            _newAsset = BuildLabeledTextField(ui, ModLocaleKeys.RecordEditorAsset, "");
            _newAsset.Text.Content.OnValueChange += value =>
            {
                var valid = Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri is not null;
                _newAsset.Text.Color.Value = valid ? RadiantUI_Constants.Hero.GREEN : RadiantUI_Constants.Hero.RED;
            };
        }

        // Copy URL buttons
        ui.Spacer(16f);
        foreach (var action in AvailableCopyActions(_record))
        {
            var copyRecordUrlButton = ui.Button(ModLocaleKeys.RecordEditorCopyUrl(action));
            copyRecordUrlButton.LocalPressed += (button, data) => CopyUrl(action);
        }

        {
            var layout = ui.Empty("Spacer").AttachComponent<LayoutElement>();
            layout.FlexibleHeight.Value = 1f;
            layout.MinHeight.Value = 0f;
            layout.PreferredHeight.Value = 0f;
        }

        ui.Style.PreferredHeight = 48f;
        ui.HorizontalLayout(6f);

        ui.PushStyle();
        ui.Style.ButtonColor = RadiantUI_Constants.MidLight.GREEN;
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
        if (_record is null) return;
        var profile = _slot.Engine.PlatformProfile;

        string result = action switch
        {
            RecordCopyUrlAction.Record => _record.GetUrl(profile).ToString(),
            RecordCopyUrlAction.Asset => _record.AssetURI,
            RecordCopyUrlAction.Web => _record.GetWebUrl(profile).ToString(),
            RecordCopyUrlAction.WebAsset => _slot.Engine.Cloud.Assets.DBToHttp(new Uri(_record.AssetURI), DB_Endpoint.Default).ToString(),
            _ => throw new InvalidOperationException(),
        };
        _slot.Engine.InputInterface.Clipboard?.SetText(result);
    }

    private static RecordCopyUrlAction[] AvailableCopyActions(Record record)
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
        if (_record is null) return;

        // Modify record
        if (_nameField is not null) _record.Name = _nameField.Text.Content.Value;
        if (_thumbnail?.Value is not null) _record.ThumbnailURI = _thumbnail.Value.ToString();

        if (_newAsset is not null && Uri.TryCreate(_newAsset.Text.Content.Value, UriKind.Absolute, out var uri) && uri is not null)
        {
            _record.AssetURI = uri.ToString();
        }

        // Remove path characters.
        // Path characters, while technically possible, will result in the inventory freaking out.
        if (_record.RecordType == RecordTypes.DIRECTORY && _record.Name.Contains('/'))
        {
            _record.Name = _record.Name.Replace('/', ' ');
        }

        // Save record
        _slot.World.RunSynchronously(() => BuildLoadingUI());
        var result = await _slot.Engine.RecordManager.SaveRecord(_record).ConfigureAwait(continueOnCapturedContext: false);
        if (result.saved)
        {
            Saved?.Invoke(_record);
            _slot.World.RunSynchronously(() => Close());
        }
        else
        {
            _slot.World.RunSynchronously(() => BuildErrorUI("Failed"));
        }
    }

    private void Close()
    {
        var container = _slot.GetComponentInParents<IUIContainer>();
        if (container is not null)
        {
            container.CloseContainer();
        }
        else
        {
            ObjectRootExtensions.GetObjectRoot(_slot).Destroy();
        }
    }

    private static void BeginBuildLabeledField(UIBuilder ui, LocaleString label)
    {
        ui.HorizontalLayout(8f);
        ui.Text(label, bestFit: true, alignment: Alignment.MiddleLeft, parseRTF: false);
    }

    private static TextField BuildLabeledTextField(UIBuilder ui, LocaleString label, string value)
    {
        BeginBuildLabeledField(ui, label);
        var textField = ui.TextField(value, parseRTF: false);
        textField.Slot.GetComponentOrAttach<LayoutElement>().MinWidth.Value = 375f;
        ui.NestOut();
        return textField;
    }

    private static Sync<Uri> BuildLabeledTextureField(UIBuilder ui, LocaleString label, Uri? texture)
    {
        ui.PushStyle();
        ui.Style.MinHeight = 96f;
        BeginBuildLabeledField(ui, label);
        ui.HorizontalLayout(0f, 0f, Alignment.MiddleCenter);
        ui.PopStyle();
        var panel = ui.Panel();
        var panelLayout = panel.Slot.GetComponentOrAttach<LayoutElement>();
        panelLayout.MinWidth.Value = 80f;
        panelLayout.MinHeight.Value = 80f;
        var button = panel.Slot.AttachComponent<Button>();
        var staticTexture = panel.Slot.AttachComponent<StaticTexture2D>();
        var image = ui.RawImage(staticTexture, preserveAspect: true);
        image.InteractionTarget.Value = false;
        if (texture is not null) staticTexture.URL.Value = texture;
        ui.NestOut();
        ui.NestOut();
        ui.NestOut();
        button.LocalPressed += (button, data) =>
        {
            Grabber grabber = button.Slot.World.GetLocalUserGrabberWithItems(data.source.Slot);
            if (grabber is null) return;
            var held = grabber.LocalExternallyHeldItem ?? grabber.HolderSlot;
            if (held is null) return;
            foreach (var refProxy in held.GetComponentsInChildren<ReferenceProxy>())
            {
                if (refProxy.Reference.Target is StaticTexture2D tex)
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
