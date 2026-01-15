using FrooxEngine;
using HarmonyLib;

[HarmonyPatch(typeof(RecordDirectory))]
internal static class RecordDirectoryPatch
{
    [HarmonyReversePatch]
    [HarmonyPatch(nameof(RecordDirectory.Name), MethodType.Setter)]
    internal static void SetName(RecordDirectory instance, string name)
        => throw new NotImplementedException();
}
