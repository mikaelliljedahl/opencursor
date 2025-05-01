using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.IO;
using OpenCursor.Client.Commands;

namespace OpenCursor.Client
{
    public class LlmResponseParser
    {
        // Parses the raw LLM response string containing XML-like command tags
        // and returns a list of McpCommand objects.
        public List<IMcpCommand> ParseCommands(string rawResponse)
        {   
            var commands = new List<IMcpCommand>();
            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                return commands; // Return empty list if response is empty
            }

            // Basic check if it looks like XML might be present
            if (!rawResponse.Trim().Contains('<') || !rawResponse.Trim().Contains('>'))
            {
                 Console.WriteLine("Parser: Received response does not appear to contain XML tags. Skipping parse.");
                 // Potentially handle plain text differently here if needed
                 return commands;
            }

            try
            {
                // Attempt to wrap the content to handle potentially non-rooted fragments 
                // and ensure valid XML structure for parsing.
                // We expect commands like <create_file path="...">content</create_file>
                // or <execute_command>command line</execute_command>
                string wrappedXml = $"<root>{rawResponse}</root>"; 
                XDocument doc = XDocument.Parse(wrappedXml, LoadOptions.None);

                foreach (XElement element in doc.Root.Elements())
                {
                    string commandName = element.Name.LocalName;
                    string content = element.Value; // Content inside the tag
                    string path = element.Attribute("path")?.Value; // Path attribute for file ops

                    Console.WriteLine($"Parser: Found tag '{commandName}' Path='{path}' Content='{content.Substring(0, Math.Min(content.Length, 50))}...'");

                    switch (commandName.ToLowerInvariant())
                    {
                        case "create_file":
                            if (!string.IsNullOrEmpty(path))
                            {
                                commands.Add(new CreateFileCommand { RelativePath = path, Content = content });
                            }
                            else
                            {
                                Console.WriteLine($"Parser Warning: <create_file> tag missing 'path' attribute. Skipping.");
                            }
                            break;

                        case "update_file": // Assuming update_file also uses path + content
                             if (!string.IsNullOrEmpty(path))
                            {
                                // NOTE: McpProcessor might need adjustment if Update is different from Create
                                // For now, treating similarly for parsing structure
                                commands.Add(new UpdateFileCommand { RelativePath = path, Content = content }); 
                            }
                            else
                            {
                                Console.WriteLine($"Parser Warning: <update_file> tag missing 'path' attribute. Skipping.");
                            }
                            break;
                         
                        case "delete_file":
                             if (!string.IsNullOrEmpty(path))
                            {
                                commands.Add(new DeleteFileCommand { RelativePath = path });
                            }
                            else
                            {
                                Console.WriteLine($"Parser Warning: <delete_file> tag missing 'path' attribute. Skipping.");
                            }
                            break;

                        case "execute_command":
                            if (!string.IsNullOrWhiteSpace(content))
                            {
                                commands.Add(new ExecuteCommand { CommandLine = content });
                            }
                            else
                            {
                                Console.WriteLine($"Parser Warning: <execute_command> tag has empty content. Skipping.");
                            }
                            break;
                        
                        // Add cases for other commands as defined in systemprompt.md

                        default:
                            Console.WriteLine($"Parser Warning: Unknown command tag '{commandName}'. Skipping.");
                            break;
                    }
                }
            }
            catch (System.Xml.XmlException xmlEx)
            {
                // Handle cases where the response is not valid XML or contains unexpected structures
                Console.WriteLine($"Parser Error: Failed to parse LLM response as XML. Content: '{rawResponse.Substring(0, Math.Min(rawResponse.Length, 200))}...' Error: {xmlEx.Message}");
                // Optionally, return the raw string as a different type or log it
            }
             catch (Exception ex)
            {
                Console.WriteLine($"Parser Error: Unexpected error during parsing. Error: {ex.Message}");
            }

            return commands;
        }
    }
}
