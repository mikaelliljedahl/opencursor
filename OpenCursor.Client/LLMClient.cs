using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

namespace OpenCursor.Client;

public static class LLMClient
{
    //public static async Task PrepareAndSend(DirectoryBrowser browser)
    //{
    //    if (!browser.MarkedEntries.Any())
    //    {
    //        Console.WriteLine("No files or directories marked. Press any key...");
    //        Console.ReadKey();
    //        return;
    //    }


    //    Console.Clear();
    //    Console.WriteLine("Prepared prompt for LLM:");
    //    Console.WriteLine(new string('-', 40));
    //    Console.WriteLine(prompt.Substring(0, Math.Min(prompt.Length, 1500)));
    //    Console.WriteLine(new string('-', 40));
    //    Console.WriteLine("Open Web Browser to proceed? (Y/N)");

    //    var key = Console.ReadKey(true);
    //    if (key.Key == ConsoleKey.Y)
    //    {
    //        // Send the prompt to the WebView2 app via HTTP POST
    //        await SendPromptToBrowserHost(prompt);
    //    }
    //    else
    //    {
    //        Console.WriteLine("Cancelled. Press any key...");
    //        Console.ReadKey();
    //    }
    //}

    //public static async Task PrepareAndSendSystemPrompt()
    //{
    //    var systemprompt = BuildSystemPrompt();
    //    await SendPromptToBrowserHost(systemprompt);
    //}

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
