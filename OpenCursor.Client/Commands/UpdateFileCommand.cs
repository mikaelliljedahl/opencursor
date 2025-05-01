namespace OpenCursor.Client.Commands
{
    internal class UpdateFileCommand : IMcpCommand
    {
        public string RelativePath { get; set; }
        public string? Content { get; set; }

    }
}