using HarmonyLib;

using ResoniteModLoader;

namespace ResdbTools;

/// <summary>
/// Extends the inventory browser with tools to get and modify Resonite database records.
/// </summary>
public sealed class ResdbTools : ResoniteMod
{

    /// <inheritdoc/>
    internal const string VERSION_CONSTANT = "1.0.0";

    /// <inheritdoc/>
    public override string Name => "ResdbTools";

    /// <inheritdoc/>
    public override string Author => "Colin Tim Barndt";

    /// <inheritdoc/>
    public override string Version { get; } =
        System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

    /// <inheritdoc/>
    public override string Link => "https://github.com/ColinTimBarndt/resonite-resdb-tools-mod";

    internal static ModConfiguration? Config;

    /// <inheritdoc/>
    public override void OnEngineInit()
    {
        Config = GetConfiguration();

        Harmony harmony = new("cat.colin.ResdbTools");
        harmony.PatchAll();
    }

    /// <summary>When checked, enables ResdbTools.</summary>
    [AutoRegisterConfigKey]
    public static ModConfigurationKey<bool> Enabled = new("Enabled", "When checked, enables ResdbTools", computeDefault: () => true);

}
