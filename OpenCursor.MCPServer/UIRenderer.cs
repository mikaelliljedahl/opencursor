using System;
using System.IO;

namespace OpenCursor.Client;

public class UIRenderer
{
    private readonly DirectoryBrowser _browser;

    public UIRenderer(DirectoryBrowser browser)
    {
        _browser = browser;
    }

    public void Draw()
    {
        Console.Clear();
        Console.WriteLine($"Current Directory: {_browser.CurrentDirectory}");
        Console.WriteLine(new string('-', Console.WindowWidth));

        for (int i = 0; i < _browser.Entries.Count; i++)
        {
            string entry = _browser.Entries[i];
            bool isSelected = i == _browser.SelectedIndex;
            bool isMarked = _browser.MarkedEntries.Contains(entry);

            if (isSelected)
                Console.BackgroundColor = ConsoleColor.Gray;

            string name = Path.GetFileName(entry);
            if (Directory.Exists(entry))
                name = "[DIR] " + name;

            if (isMarked)
                name = "[*] " + name;
            else
                name = "    " + name;

            Console.WriteLine(name);

            Console.ResetColor();
        }

        Console.WriteLine(new string('-', Console.WindowWidth));
        Console.WriteLine("F1=Help | Enter=Open | Backspace=Up | + = Mark | - = Unmark | F10=Exit");
    }
}
