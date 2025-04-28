using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenCursor.Client;

public static class LLMClient
{
    public static async Task PrepareAndSend(DirectoryBrowser browser)
    {
        if (!browser.MarkedEntries.Any())
        {
            Console.WriteLine("No files or directories marked. Press any key...");
            Console.ReadKey();
            return;
        }

        var prompt = BuildPromptWithSelectedFiles(browser);

        Console.Clear();
        Console.WriteLine("Prepared prompt for LLM:");
        Console.WriteLine(new string('-', 40));
        Console.WriteLine(prompt.Substring(0, Math.Min(prompt.Length, 1500)));
        Console.WriteLine(new string('-', 40));
        Console.WriteLine("Open Web Browser to proceed? (Y/N)");

        var key = Console.ReadKey(true);
        if (key.Key == ConsoleKey.Y)
        {
            // Send the prompt to the WebView2 app via HTTP POST
            await SendPromptToBrowserHost(prompt);
        }
        else
        {
            Console.WriteLine("Cancelled. Press any key...");
            Console.ReadKey();
        }
    }

    private static async Task SendPromptToBrowserHost(string prompt)
    {
        using (HttpClient client = new HttpClient())
        {
            // Prepare the data to be sent
            var content = new StringContent(prompt, Encoding.UTF8, "application/json");

            try
            {
                // Send POST request
                HttpResponseMessage response = await client.PostAsync("http://localhost:12345/prompt", content);

                // Check if the response is successful
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Prompt sent successfully.");
                }
                else
                {
                    Console.WriteLine($"Failed to send prompt. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                // Handle any errors
                Console.WriteLine($"Error sending prompt: {ex.Message}");
            }
        }
    }
    private static string BuildPromptWithSelectedFiles(DirectoryBrowser browser)
    {
        var request = new LlmRequest
        {
            Instructions = """
            You are helping edit a software project.
            Below are the current selected files.
            You are allowed to update files, delete files, and create new files.
            To perform an action, reply with MCP (Minimal Command Protocol):
            
            @CREATE_FILE <relative_path>
            <file_content>
            @END

            @UPDATE_FILE <relative_path>
            <new_file_content>
            @END

            @DELETE_FILE <relative_path>
            @END

            Do not modify files unless explicitly told to. You can also suggest changes if unsure.
            """,
            Files = browser.MarkedEntries
                .Where(File.Exists)
                .Select(path => new LlmFile
                {
                    RelativePath = Path.GetRelativePath("/", path),
                    Content = File.ReadAllText(path)
                })
                .ToList()
        };

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        var fullPrompt = new StringBuilder();
        fullPrompt.AppendLine(request.Instructions);
        fullPrompt.AppendLine();
        fullPrompt.AppendLine("Here are the selected files:");
        fullPrompt.AppendLine(json);

        return fullPrompt.ToString();
    }

    private class LlmRequest
    {
        public string Instructions { get; set; }
        public List<LlmFile> Files { get; set; }
    }

    private class LlmFile
    {
        public string RelativePath { get; set; }
        public string Content { get; set; }
    }
}
