#if WINDOWS
using Briefcase.Components.Services;
using Windows.System;
using WinUIKeyboardAccelerator = Microsoft.UI.Xaml.Input.KeyboardAccelerator;

namespace Briefcase.Maui.Services;

public class WindowsKeyboardShortcutService : IKeyboardShortcutService
{
    public event Action<KeyboardAction>? ShortcutTriggered;

    public void RegisterAccelerators(Microsoft.UI.Xaml.UIElement rootElement)
    {
        // Ctrl+N — New message
        var ctrlN = new WinUIKeyboardAccelerator { Modifiers = VirtualKeyModifiers.Control, Key = VirtualKey.N };
        ctrlN.Invoked += (_, _) => ShortcutTriggered?.Invoke(KeyboardAction.NewMessage);
        rootElement.KeyboardAccelerators.Add(ctrlN);

        // Ctrl+Shift+V — Paste from clipboard
        var ctrlShiftV = new WinUIKeyboardAccelerator { Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, Key = VirtualKey.V };
        ctrlShiftV.Invoked += (_, _) => ShortcutTriggered?.Invoke(KeyboardAction.PasteFromClipboard);
        rootElement.KeyboardAccelerators.Add(ctrlShiftV);

        // Delete — Move to trash
        var del = new WinUIKeyboardAccelerator { Key = VirtualKey.Delete };
        del.Invoked += (_, _) => ShortcutTriggered?.Invoke(KeyboardAction.MoveToTrash);
        rootElement.KeyboardAccelerators.Add(del);

        // Ctrl+P — Toggle pin
        var ctrlP = new WinUIKeyboardAccelerator { Modifiers = VirtualKeyModifiers.Control, Key = VirtualKey.P };
        ctrlP.Invoked += (_, _) => ShortcutTriggered?.Invoke(KeyboardAction.TogglePin);
        rootElement.KeyboardAccelerators.Add(ctrlP);

        // Ctrl+F — Focus search
        var ctrlF = new WinUIKeyboardAccelerator { Modifiers = VirtualKeyModifiers.Control, Key = VirtualKey.F };
        ctrlF.Invoked += (_, _) => ShortcutTriggered?.Invoke(KeyboardAction.FocusSearch);
        rootElement.KeyboardAccelerators.Add(ctrlF);
    }
}
#endif
