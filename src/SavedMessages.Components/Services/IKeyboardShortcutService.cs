namespace SavedMessages.Components.Services;

public enum KeyboardAction
{
    NewMessage,
    PasteFromClipboard,
    MoveToTrash,
    TogglePin,
    FocusSearch
}

public interface IKeyboardShortcutService
{
    event Action<KeyboardAction>? ShortcutTriggered;
}
