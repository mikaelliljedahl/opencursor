using OpenCursor.Client.Commands;
using System.IO;
using System.Threading.Tasks;

namespace OpenCursor.Client.Handlers
{
    public class UpdateFileHandler : IMcpCommandHandler
    {
        public string CommandName => "update_file";

        public bool CanHandle(IMcpCommand command) => command is UpdateFileCommand;

        public async Task<string> HandleCommand(IMcpCommand command, string workspaceRoot)
        {
            var updateCommand = (UpdateFileCommand)command;
            string fullPath = Path.Combine(workspaceRoot, updateCommand.RelativePath);

            // Create backup
            string backupPath = fullPath + ".bak";

            File.Copy(fullPath, backupPath, true);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("File not found for update.", fullPath);

            await File.WriteAllTextAsync(fullPath, updateCommand.Content ?? string.Empty);
            return $"File updated: {updateCommand.RelativePath}";
        }
    }
}
