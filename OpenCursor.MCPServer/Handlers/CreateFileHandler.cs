using OpenCursor.Client.Commands;
using System.IO;
using System.Threading.Tasks;

namespace OpenCursor.Client.Handlers
{
    public class CreateFileHandler : IMcpCommandHandler
    {
        public bool CanHandle(IMcpCommand command) => command is CreateFileCommand;

        public async Task HandleCommand(IMcpCommand command, string workspaceRoot)
        {
            var createCommand = (CreateFileCommand)command;
            string fullPath = Path.Combine(workspaceRoot, createCommand.RelativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllTextAsync(fullPath, createCommand.Content ?? string.Empty);
            Console.WriteLine($"File created: {fullPath}");
        }
    }
}
