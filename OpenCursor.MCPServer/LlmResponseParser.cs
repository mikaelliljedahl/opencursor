using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenCursor.Client.Commands;
using OpenCursor.Client.Handlers;
using OpenCursor.MCPServer.Handlers; // Ensure this using directive is present

namespace OpenCursor.Client
{
    public class LlmResponseParser
    {
        // Helper class to represent the incoming JSON command structure
        private class McpCommandRequest
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("parameters")]
            public JsonElement Parameters { get; set; } // Use JsonElement for flexible parameter parsing
        }

        // Options for deserialization (can be customized further if needed)
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true // Handles minor case variations if JsonPropertyName isn't used everywhere
            // Add other options like custom converters if necessary
        };

        /// <summary>
        /// Parse commands from the LLM response.
        /// </summary>
        /// <param name="llmResponse"></param>
        /// <returns></returns>
        public IEnumerable<IMcpCommand> ParseCommands(string llmResponse)
        {
            if (string.IsNullOrWhiteSpace(llmResponse))
            {
                yield break; // No commands to parse
            }


            // Basic cleanup: Trim whitespace and potential markdown code fences
            string jsonContent = llmResponse.Trim();


            if (!jsonContent.Contains("'''"))
                yield break; // No code fences to process

            if (jsonContent.StartsWith("```json"))
            {
                jsonContent = jsonContent.Substring(7);
                jsonContent = jsonContent.TrimEnd('`');
            }
            else if (jsonContent.StartsWith("```")) // Handle generic code fence
            {
                jsonContent = jsonContent.Substring(3);
                jsonContent = jsonContent.TrimEnd('`');
            }
            jsonContent = jsonContent.Trim(); // Trim again after removing fences


            // Check if the content is likely JSON (starts with { or [)
            if (!jsonContent.StartsWith("{") && !jsonContent.StartsWith("["))
            {
                // If it doesn't look like JSON, maybe it's just plain text - treat as no command
                Console.WriteLine($"LLM Response Parser: Response does not appear to be JSON: {jsonContent.Substring(0, Math.Min(jsonContent.Length, 100))}...");
                yield break;
            }


            List<McpCommandRequest> commandRequests = new List<McpCommandRequest>();

            try
            {
                // Attempt to deserialize as a single command object first
                if (jsonContent.StartsWith("{"))
                {
                    var singleCommand = JsonSerializer.Deserialize<McpCommandRequest>(jsonContent, _jsonOptions);
                    if (singleCommand != null && !string.IsNullOrEmpty(singleCommand.Name))
                    {
                        commandRequests.Add(singleCommand);
                    }
                    else
                    {
                        // Log if it looks like an object but fails to deserialize meaningfully
                        Console.WriteLine($"LLM Response Parser: Failed to deserialize as a single command object: {jsonContent.Substring(0, Math.Min(jsonContent.Length, 100))}...");
                    }
                }
                // If it starts with '[', attempt to deserialize as an array
                else if (jsonContent.StartsWith("["))
                {
                    var multipleCommands = JsonSerializer.Deserialize<List<McpCommandRequest>>(jsonContent, _jsonOptions);
                    if (multipleCommands != null)
                    {
                        commandRequests.AddRange(multipleCommands.Where(cmd => cmd != null && !string.IsNullOrEmpty(cmd.Name)));
                    }
                    else
                    {
                        // Log if it looks like an array but fails to deserialize meaningfully
                        Console.WriteLine($"LLM Response Parser: Failed to deserialize as a command array: {jsonContent.Substring(0, Math.Min(jsonContent.Length, 100))}...");
                    }
                }
                // If neither { nor [, we already exited above
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"LLM Response Parser: JSON Deserialization failed: {ex.Message}. Content: {jsonContent.Substring(0, Math.Min(jsonContent.Length, 100))}...");
                yield break; // Exit if JSON is fundamentally invalid
            }


            // --- Convert McpCommandRequest objects to specific IMcpCommand instances ---
            foreach (var request in commandRequests)
            {
                IMcpCommand? command = null;


                // Find the appropriate command type based on the command name
                var handler = McpCommandHandlerRegistry.CreateHandler(request.Name);

                if (handler == null)
                {
                    continue;
                    // throw new InvalidOperationException($"No handler found for command '{request.Name}'");
                }


                if (handler != null)
                {

                    // Deserialize using the found command type
                    command = request as IMcpCommand;

                    // Handle null content fields for specific commands
                    if (command is CreateFileCommand cfCmd) cfCmd.Content ??= string.Empty;
                    if (command is UpdateFileCommand ufCmd) ufCmd.Content ??= string.Empty;
                    if (command is EditFileCommand efCmd) efCmd.CodeEdit ??= string.Empty;

                    // Validate required fields for specific commands
                    if (command is ListDirCommand ldCmd && string.IsNullOrWhiteSpace(ldCmd.RelativePath))
                    {
                        Console.WriteLine($"LLM Response Parser: Missing 'relative_workspace_path' for list_dir command.");
                        command = null;
                    }
                    else if (command is DeleteFileCommand delCmd && string.IsNullOrWhiteSpace(delCmd.TargetFile))
                    {
                        Console.WriteLine($"LLM Response Parser: Missing 'target_file' for delete_file command.");
                        command = null;
                    }
                    else if (command is ReadFileCommand rfCmd && string.IsNullOrWhiteSpace(rfCmd.RelativePath))
                    {
                        Console.WriteLine($"LLM Response Parser: Missing 'relative_workspace_path' for read_file command.");
                        command = null;
                    }
                    else if (command is CodebaseSearchCommand csCmd && string.IsNullOrWhiteSpace(csCmd.Query))
                    {
                        Console.WriteLine($"LLM Response Parser: Missing 'query' for codebase_search command.");
                        command = null;
                    }
                    else if (command is RunTerminalCommand rtCmd && string.IsNullOrWhiteSpace(rtCmd.CommandLine))
                    {
                        Console.WriteLine($"LLM Response Parser: Missing 'command' for run_terminal_cmd command.");
                        command = null;
                    }
                    else if (command is GrepSearchCommand gsCmd && string.IsNullOrWhiteSpace(gsCmd.Query))
                    {
                        Console.WriteLine($"LLM Response Parser: Missing 'query' for grep_search command.");
                        command = null;
                    }
                    else if (command is FileSearchCommand fsCmd && string.IsNullOrWhiteSpace(fsCmd.Query))
                    {
                        Console.WriteLine($"LLM Response Parser: Missing 'query' for file_search command.");
                        command = null;
                    }
                    else if (command is ReapplyCommand raCmd && string.IsNullOrWhiteSpace(raCmd.TargetFile))
                    {
                        Console.WriteLine($"LLM Response Parser: Missing 'target_file' for reapply command.");
                        command = null;
                    }
                    else if (command is ParallelApplyCommand paCmd &&
                        (string.IsNullOrWhiteSpace(paCmd.EditPlan) || paCmd.EditRegions == null || !paCmd.EditRegions.Any()))
                    {
                        Console.WriteLine($"LLM Response Parser: Missing required parameter(s) for parallel_apply command (edit_plan, edit_regions).");
                        command = null;
                    }

                    if (command != null)
                    {
                        yield return command;
                    }
                }
                else
                {
                    Console.WriteLine($"LLM Response Parser: No command type found for: {request.Name}");
                }

            

                if (command != null)
                {
                    yield return command; // Return the successfully parsed command
                }
            }
        }
    }
}