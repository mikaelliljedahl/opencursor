using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenCursor.Client.Commands; // Ensure this using directive is present

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

        public IEnumerable<IMcpCommand> ParseCommands(string llmResponse)
        {
            if (string.IsNullOrWhiteSpace(llmResponse))
            {
                yield break; // No commands to parse
            }

            // Basic cleanup: Trim whitespace and potential markdown code fences
            string jsonContent = llmResponse.Trim();
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
                try
                {
                    // Use the command classes directly for deserializing parameters
                    // The [JsonPropertyName] attributes on the command classes handle mapping
                    switch (request.Name.ToLowerInvariant()) // Use lower case for comparison
                    {
                        case "create_file":
                            command = request.Parameters.Deserialize<CreateFileCommand>(_jsonOptions);
                            if (command is CreateFileCommand cfCmd) cfCmd.Content ??= string.Empty; // Ensure content isn't null
                            break;

                        case "update_file":
                            command = request.Parameters.Deserialize<UpdateFileCommand>(_jsonOptions);
                            if (command is UpdateFileCommand ufCmd) ufCmd.Content ??= string.Empty; // Ensure content isn't null
                            break;

                        case "delete_file":
                            command = request.Parameters.Deserialize<DeleteFileCommand>(_jsonOptions);
                            // Correct validation to use TargetFile
                            if (command is DeleteFileCommand delCmd && string.IsNullOrWhiteSpace(delCmd.TargetFile))
                            {
                                Console.WriteLine($"LLM Response Parser: Missing 'target_file' for delete_file command.");
                                command = null; // Invalidate
                            }
                            break;

                        case "read_file":
                            command = request.Parameters.Deserialize<ReadFileCommand>(_jsonOptions);
                            // Basic validation: Check if path is provided
                            if (command is ReadFileCommand rfCmd && string.IsNullOrWhiteSpace(rfCmd.RelativePath))
                            {
                                Console.WriteLine($"LLM Response Parser: Missing 'relative_workspace_path' for read_file command.");
                                command = null; // Invalidate the command
                            }
                            break;
                        
                        // --- Add New Command Cases --- 
                        case "codebase_search":
                            command = request.Parameters.Deserialize<CodebaseSearchCommand>(_jsonOptions);
                            if (command is CodebaseSearchCommand csCmd && string.IsNullOrWhiteSpace(csCmd.Query))
                            {
                                Console.WriteLine($"LLM Response Parser: Missing 'query' for codebase_search command.");
                                command = null;
                            }
                            break;

                        case "run_terminal_cmd":
                            command = request.Parameters.Deserialize<RunTerminalCommand>(_jsonOptions);
                            if (command is RunTerminalCommand rtCmd && string.IsNullOrWhiteSpace(rtCmd.CommandLine))
                            {
                                Console.WriteLine($"LLM Response Parser: Missing 'command' for run_terminal_cmd command.");
                                command = null;
                            }
                            break;

                        case "list_dir":
                            command = request.Parameters.Deserialize<ListDirCommand>(_jsonOptions);
                            // Path is required for list_dir
                            if (command is ListDirCommand ldCmd && string.IsNullOrWhiteSpace(ldCmd.RelativePath))
                            {
                                Console.WriteLine($"LLM Response Parser: Missing 'relative_workspace_path' for list_dir command.");
                                command = null;
                            }
                            break;

                        case "grep_search":
                            command = request.Parameters.Deserialize<GrepSearchCommand>(_jsonOptions);
                            if (command is GrepSearchCommand gsCmd && string.IsNullOrWhiteSpace(gsCmd.Query))
                            {
                                Console.WriteLine($"LLM Response Parser: Missing 'query' for grep_search command.");
                                command = null;
                            }
                            break;

                        case "file_search":
                            command = request.Parameters.Deserialize<FileSearchCommand>(_jsonOptions);
                            if (command is FileSearchCommand fsCmd && string.IsNullOrWhiteSpace(fsCmd.Query))
                            {
                                Console.WriteLine($"LLM Response Parser: Missing 'query' for file_search command.");
                                command = null;
                            }
                            break;

                        case "edit_file":
                            command = request.Parameters.Deserialize<EditFileCommand>(_jsonOptions);
                            if (command is EditFileCommand efCmd && 
                                (string.IsNullOrWhiteSpace(efCmd.TargetFile) || string.IsNullOrWhiteSpace(efCmd.Instructions) || string.IsNullOrWhiteSpace(efCmd.CodeEdit)))
                            {
                                Console.WriteLine($"LLM Response Parser: Missing required parameter(s) for edit_file command (target_file, instructions, code_edit).");
                                command = null;
                            }
                            break;
                        
                        case "reapply":
                            command = request.Parameters.Deserialize<ReapplyCommand>(_jsonOptions);
                            if (command is ReapplyCommand raCmd && string.IsNullOrWhiteSpace(raCmd.TargetFile))
                            {
                                Console.WriteLine($"LLM Response Parser: Missing 'target_file' for reapply command.");
                                command = null;
                            }
                            break;

                        case "parallel_apply":
                            command = request.Parameters.Deserialize<ParallelApplyCommand>(_jsonOptions);
                            if (command is ParallelApplyCommand paCmd && 
                                (string.IsNullOrWhiteSpace(paCmd.EditPlan) || paCmd.EditRegions == null || !paCmd.EditRegions.Any()))
                            {
                                Console.WriteLine($"LLM Response Parser: Missing required parameter(s) for parallel_apply command (edit_plan, edit_regions).");
                                command = null;
                            }
                            // Could add deeper validation for EditRegions if needed
                            break;
                        // ---------------------------

                        default:
                            Console.WriteLine($"LLM Response Parser: Unsupported command name '{request.Name}'");
                            break;
                    }
                }
                catch (JsonException paramEx)
                {
                    // Log detailed error if parameter deserialization fails for a specific command
                    Console.WriteLine($"LLM Response Parser: Failed to deserialize parameters for command '{request.Name}': {paramEx.Message}. Parameters JSON: {request.Parameters.GetRawText()}");
                }
                catch (Exception ex) // Catch broader exceptions during processing
                {
                    Console.WriteLine($"LLM Response Parser: Error processing command '{request.Name}': {ex.Message}");
                }

                if (command != null)
                {
                    yield return command; // Return the successfully parsed command
                }
            }
        }
        // Note: Removed old XML parsing methods like ExtractCommand, ExtractParameter, UnescapeXml
    }
}