namespace OpenCursor.Client.Handlers
{
    public interface IMcpCommandHandler
    {
        string CommandName { get; }
        Task<string> HandleCommand(IMcpCommand command, string workspaceRoot);
        bool CanHandle(IMcpCommand command);

        // Default implementation for GetFullPath
        internal static string GetFullPath(string relativePath, string workspaceRoot)
        {
            string combinedPath = Path.Combine(workspaceRoot, relativePath);
            return Path.GetFullPath(combinedPath);
        }
    }
}
