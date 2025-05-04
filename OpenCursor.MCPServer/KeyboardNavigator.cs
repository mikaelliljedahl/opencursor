using System;

namespace OpenCursor.Client;

public class KeyboardNavigator
{
    private readonly DirectoryBrowser _browser;
    private readonly UIRenderer _ui;

    public KeyboardNavigator(DirectoryBrowser browser, UIRenderer ui)
    {
        _browser = browser;
        _ui = ui;
    }

    public bool HandleKeyPress()
    {
        var key = Console.ReadKey(true);

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                _browser.MoveUp();
                break;
            case ConsoleKey.DownArrow:
                _browser.MoveDown();
                break;
            case ConsoleKey.Enter:
                _browser.EnterSelected();
                break;
            case ConsoleKey.Backspace:
                _browser.GoUpDirectory();
                break;
            case ConsoleKey.F1:
                HelpViewer.ShowHelp();
                break;
            case ConsoleKey.F10:
                return false; // Exit
            case ConsoleKey.Add: // Numpad +
            case ConsoleKey.OemPlus: // + key
                _browser.ToggleMark();
                break;
            case ConsoleKey.Subtract: // Numpad -
            case ConsoleKey.OemMinus: // - key
                _browser.Unmark();
                break;
        }

        return true;
    }
}
