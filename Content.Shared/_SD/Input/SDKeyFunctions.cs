using Robust.Shared.Input;

namespace Content.Shared._SD.Input;

/// <summary>
/// Space Dream keybinds, kept separate from upstream <see cref="Content.Shared.Input.ContentKeyFunctions"/>.
/// </summary>
[KeyFunctions]
public static class SDKeyFunctions
{
    public static readonly BoundKeyFunction ToggleRaiseShield = "ToggleRaiseShield";
    public static readonly BoundKeyFunction ResistGrab = "ResistGrab";
}
