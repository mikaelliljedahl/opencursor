namespace OpenCursor.Client.Commands
{
    internal class CreateFileCommand : IMcpCommand
    {
        public string RelativePath { get; set; }
        public string? Content { get; set; }
    }
}