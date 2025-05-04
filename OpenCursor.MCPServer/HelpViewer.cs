using System;

namespace OpenCursor.Client;

public static class HelpViewer
{
    public static void ShowHelp()
    {
        Console.Clear();
        Console.WriteLine("OpenCursor Help:");
        Console.WriteLine("- Navigate with Up/Down arrows.");
        Console.WriteLine("- Enter: Open (Select) folder/file.");
        Console.WriteLine("- Backspace: Go up a directory.");
        Console.WriteLine("- F1: Show this help.");
        Console.WriteLine("- F10: Exit application.");
        Console.WriteLine("\nPress any key to return...");
        Console.ReadKey();
    }
}
