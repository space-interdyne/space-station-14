using Content.Shared._SD.Input;
using Robust.Shared.Input;

namespace Content.Client._SD.Input;

/// <summary>
/// Registers Space Dream key functions into engine input contexts.
/// </summary>
public static class SDContentContexts
{
    public static void AddHumanFunctions(IInputCmdContext human)
    {
        human.AddFunction(SDKeyFunctions.ToggleRaiseShield);
        human.AddFunction(SDKeyFunctions.ResistGrab);
        human.AddFunction(SDKeyFunctions.Sprint);
    }
}
