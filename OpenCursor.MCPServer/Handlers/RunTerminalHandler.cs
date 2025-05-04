using OpenCursor.Client.Commands;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace OpenCursor.Client.Handlers
{
    public class RunTerminalHandler : IMcpCommandHandler
    {
        
        public string CommandName => "run_terminal_cmd";

        public bool CanHandle(IMcpCommand command) => command is RunTerminalCommand;

        public async Task<string> HandleCommand(IMcpCommand command, string workspaceRoot)
        {
            if (command is not RunTerminalCommand terminalCmd)
            {
                throw new ArgumentException($"Expected RunTerminalCommand, got {command.GetType().Name}");
            }

            StringBuilder sb = new StringBuilder();

            try
            {
                sb.AppendLine($"\n[Run Terminal] Attempting to run: {terminalCmd.CommandLine}");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"{terminalCmd.CommandLine}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workspaceRoot
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();

                    // Capture output/error streams
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();

                    if (!terminalCmd.IsBackground)
                    {
                        await process.WaitForExitAsync();
                        sb.AppendLine("--- Command Output ---");
                        if (!string.IsNullOrWhiteSpace(output)) sb.AppendLine(output);
                        if (!string.IsNullOrWhiteSpace(error)) Console.Error.WriteLine(error);
                        sb.AppendLine($"--- Command Finished (Exit Code: {process.ExitCode}) ---");
                        if (process.ExitCode != 0) throw new Exception($"Command failed with exit code {process.ExitCode}");
                    }
                    else
                    {
                        sb.AppendLine($"Command started in background (PID: {process.Id})");
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Failed to run command '{terminalCmd.CommandLine}': {ex.Message}";
                
            }

            return sb.ToString();
        }
    }
}
