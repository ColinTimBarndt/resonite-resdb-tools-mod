using MonkeyLoader.Resonite;

namespace Mod.ResdbTools;

internal class ResdbTools : ResoniteMonkey<ResdbTools>
{
    protected override bool OnEngineReady()
    {
        if (!base.OnEngineReady()) return false;

        Logger.Info(() => "Resdb Tools loaded.");

        return true;
    }
}
