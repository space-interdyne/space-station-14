using Content.Shared._SD.Input;
using Robust.Shared.Input;

namespace Content.Client.Options.UI.Tabs;

public sealed partial class KeyRebindTab
{
    private static void AddSDKeybinds(Action<BoundKeyFunction> addButton)
    {
        addButton(SDKeyFunctions.ToggleRaiseShield);
    }
}
