using HarmonyLib;

using ResoniteModLoader;

namespace ResdbTools;

public sealed class ResdbTools : ResoniteMod
{
    internal const string VERSION_CONSTANT = "1.0.0";
    public override string Name => "ResdbTools";
    public override string Author => "Colin Tim Barndt";
    public override string Version => VERSION_CONSTANT;
    public override string Link => "https://github.com/ColinTimBarndt/resonite-resdb-tools-mod";

    internal static ModConfiguration? Config;

    public override void OnEngineInit()
    {
        Config = GetConfiguration();

        Harmony harmony = new("cat.colin.ResdbTools");
        harmony.PatchAll();
    }

    [AutoRegisterConfigKey]
    public static ModConfigurationKey<bool> Enabled = new("Enabled", "When checked, enables ResdbTools", computeDefault: () => true);
}
