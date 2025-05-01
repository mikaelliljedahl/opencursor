using System;
using System.IO;

namespace OpenCursor.Client;

public static class FileViewer
{
    public static void ShowFile(string filePath)
    {
        Console.Clear();
        Console.WriteLine($"--- {Path.GetFileName(filePath)} ---");

        try
        {
            var lines = File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                Console.WriteLine(line);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error reading file: " + ex.Message);
        }

        Console.WriteLine("\n--- Press any key to return ---");
        Console.ReadKey();
    }
}
