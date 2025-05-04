using OpenCursor.Client.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCursor.MCPServer.Handlers;

public static class McpCommandHandlerRegistry
{
    private static readonly Dictionary<string, Type> _handlerTypesByCommandName;

    static McpCommandHandlerRegistry()
    {
        _handlerTypesByCommandName = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IMcpCommandHandler).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .Select(t =>
            {
                var instance = (IMcpCommandHandler?)Activator.CreateInstance(t);
                return new { Type = t, Name = instance?.CommandName };
            })
            .Where(x => x.Name != null)
            .ToDictionary(x => x.Name!, x => x.Type, StringComparer.OrdinalIgnoreCase);
    }

    public static IMcpCommandHandler? CreateHandler(string commandName)
    {
        if (_handlerTypesByCommandName.TryGetValue(commandName, out var type))
        {
            return (IMcpCommandHandler?)Activator.CreateInstance(type);
        }

        return null;
    }

    public static Type? GetHandlerType(string commandName)
    {
        return _handlerTypesByCommandName.TryGetValue(commandName, out var type) ? type : null;
    }
}
