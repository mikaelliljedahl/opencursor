using OpenAI.Chat;
using System.Text.Json;
using System.Text.RegularExpressions;


namespace OpenCursor.Host.LlmClient;


public static class ToolCallExtractor
{
    private static readonly Regex ToolCallRegex = new(@"\{[\s\S]*?\}", RegexOptions.Compiled);

    public static ToolCall? TryExtractToolCall(string responseText)
    {
        var json = ExtractFirstCompleteJsonBlock(responseText);
        if (json == null) return null;

        try
        {
            return JsonSerializer.Deserialize<ToolCall>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }


    public static string? ExtractFirstCompleteJsonBlock(string text)
    {
        int depth = 0;
        int start = -1;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '{')
            {
                if (depth == 0)
                    start = i;
                depth++;
            }
            else if (text[i] == '}')
            {
                depth--;
                if (depth == 0 && start != -1)
                    return text.Substring(start, i - start + 1);
            }
        }

        return null; // No complete JSON object found
    }


}

//"tool": "ReadFile",
//"parameters": {
//  "relativePath": "requirements.txt",
//  "shouldReadEntireFile": false,
//  "startLine": 1,
//  "endLine": 10
//}


public class ToolCall
{
    public string tool { get; set; }
    public string tool_name { get => tool; set => tool = value; }
    public Dictionary<string, object> parameters { get; set; }
}

//public class Parameters // example
//{
//    public string relativePath { get; set; }
//    public bool shouldReadEntireFile { get; set; }
//    public int startLine { get; set; }
//    public int endLine { get; set; }
//}

