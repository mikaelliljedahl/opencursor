namespace OpenCursor.Client.Commands
{
    internal class DeleteFileCommand : IMcpCommand
    {
        public string RelativePath { get; set; }
    }
}