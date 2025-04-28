using System;
using System.Collections.Generic;
using System.IO;

namespace OpenCursor.Client;

public class DirectoryBrowser
{
    public string CurrentDirectory { get; private set; } = "";
    public List<string> Entries { get; private set; } = new();
    public HashSet<string> MarkedEntries { get; private set; } = new();
    public int SelectedIndex { get; private set; } = 0;

    public void LoadDirectory(string path)
    {
        try
        {
            Entries.Clear();
            Entries.AddRange(Directory.GetDirectories(path));
            Entries.AddRange(Directory.GetFiles(path));
            SelectedIndex = 0;
            CurrentDirectory = path;
            MarkedEntries.Clear();
        }
        catch (Exception ex)
        {
            Console.Clear();
            Console.WriteLine("Error loading directory: " + ex.Message);
            Console.ReadKey();
        }
    }

    public void MoveUp()
    {
        if (Entries.Count == 0) return;
        SelectedIndex = (SelectedIndex - 1 + Entries.Count) % Entries.Count;
    }

    public void MoveDown()
    {
        if (Entries.Count == 0) return;
        SelectedIndex = (SelectedIndex + 1) % Entries.Count;
    }

    public void EnterSelected()
    {
        if (Entries.Count == 0) return;

        string selectedPath = Entries[SelectedIndex];

        if (Directory.Exists(selectedPath))
        {
            LoadDirectory(selectedPath);
        }
        else if (File.Exists(selectedPath))
        {
            FileViewer.ShowFile(selectedPath);
        }
    }

    public void GoUpDirectory()
    {
        var parent = Directory.GetParent(CurrentDirectory);
        if (parent != null)
        {
            LoadDirectory(parent.FullName);
        }
    }

    public void ToggleMark()
    {
        if (Entries.Count == 0) return;

        string selectedPath = Entries[SelectedIndex];
        if (MarkedEntries.Contains(selectedPath))
            MarkedEntries.Remove(selectedPath);
        else
            MarkedEntries.Add(selectedPath);
    }

    public void Unmark()
    {
        if (Entries.Count == 0) return;

        string selectedPath = Entries[SelectedIndex];
        MarkedEntries.Remove(selectedPath);
    }
}
