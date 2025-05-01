using OpenCursor.Client.Commands;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace OpenCursor.Client.Handlers
{
    public class RunTerminalHandler : IMcpCommandHandler
    {
        
        public string CommandName => "run_terminal_cmd";

        public bool CanHandle(IMcpCommand command) => command is RunTerminalCommand;

        public async Task HandleCommand(IMcpCommand command, string workspaceRoot)
        {
            if (command is not RunTerminalCommand terminalCmd)
            {
                throw new ArgumentException($"Expected RunTerminalCommand, got {command.GetType().Name}");
            }

            try
            {
                Console.WriteLine($"\n[Run Terminal] Attempting to run: {terminalCmd.CommandLine}");
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
                        Console.WriteLine("--- Command Output ---");
                        if (!string.IsNullOrWhiteSpace(output)) Console.WriteLine(output);
                        if (!string.IsNullOrWhiteSpace(error)) Console.Error.WriteLine(error);
                        Console.WriteLine($"--- Command Finished (Exit Code: {process.ExitCode}) ---");
                        if (process.ExitCode != 0) throw new Exception($"Command failed with exit code {process.ExitCode}");
                    }
                    else
                    {
                        Console.WriteLine($"Command started in background (PID: {process.Id})");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to run command '{terminalCmd.CommandLine}': {ex.Message}");
                throw;
            }
        }
    }
}
