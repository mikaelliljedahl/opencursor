# OpenCursor Project Analysis

## Overview
OpenCursor is a modern Blazor Server-based developer assistant tool that integrates Large Language Models (LLMs) with a Model Context Protocol (MCP) server to enable AI-assisted software development. The project provides a cross-platform web interface for intelligent code assistance through structured command execution and file system operations.

## Architecture

### Core Components

#### 1. OpenCursor.Host (Blazor Server Application)
- **Technology**: C# Blazor Server (.NET 9.0)
- **Purpose**: Web-based user interface for LLM interaction
- **Key Features**:
  - Real-time chat interface with SignalR
  - Multi-provider LLM support (OpenRouter, Google Gemini)
  - Dynamic client switching via web settings
  - System prompt loading from `systemprompt.md`
  - MCP client integration using ModelContextProtocol SDK
  - Web-based tool discovery and registration
  - Cross-platform compatibility (no longer Windows-only)

#### 2. OpenCursor.MCPServer (Console Application)
- **Technology**: C# Console (.NET 9.0)
- **Purpose**: MCP server providing file system and development tools
- **Key Features**:
  - Comprehensive command handler system
  - File operations (create, read, update, delete, edit)
  - Search capabilities (semantic, regex, fuzzy file search)
  - Terminal command execution
  - Directory browsing and listing

## Technical Stack

### Dependencies
- **.NET 9.0**: Core framework
- **Microsoft.AspNetCore.Components.Web**: Blazor Server components
- **Microsoft.Extensions.AI.Abstractions**: AI framework abstractions
- **Microsoft.Extensions.AI.OpenAI**: OpenAI/OpenRouter integration
- **ModelContextProtocol**: MCP client/server communication
- **ModelContextProtocol.AspNetCore**: ASP.NET Core MCP integration
- **Mscc.GenerativeAI.Microsoft**: Google Gemini integration
- **Serilog**: Logging framework

### Communication Architecture
- **MCP Protocol**: In-process communication between Blazor host and MCP tools
- **SignalR**: Real-time web interface updates
- **Service-Oriented**: Dependency injection with scoped services
- **Tool-based Interaction**: LLM communicates through standardized MCP tools converted to AI tools

## MCP Command System

### Available Commands
1. **File Operations**:
   - `read_file`: Read file contents with line range support
   - `edit_file`: Complete file replacement with backup
   - `create_file`: Create new files with directory structure
   - `update_file`: Update existing files
   - `delete_file`: Remove files from filesystem

2. **Search and Discovery**:
   - `codebase_search`: Semantic text search across directories
   - `file_search`: Fuzzy filename matching
   - `grep_search`: Regex-based content search with patterns
   - `list_dir`: Directory listing and exploration

3. **Advanced Operations**:
   - `run_terminal_cmd`: Execute PowerShell commands
   - `parallel_apply`: Batch editing across multiple files
   - `reapply`: Reapply previous operations

### Safety Features
- Automatic backup creation (`.bak` files) before modifications
- Path validation and security through workspace-relative operations
- Comprehensive error handling and logging
- File existence validation before operations

## Configuration

### API Configuration
- Multi-provider API key configuration via `appsettings.json`:
  - Google Gemini API key
  - OpenRouter API key for multiple LLM access
- Web-based settings interface for runtime configuration
- System prompt customization through `systemprompt.md`
- Tool temperature and response format settings

### System Prompt Integration
The system uses a sophisticated prompt construction that:
- Loads base instructions from `systemprompt.md`
- Dynamically includes available MCP tools with JSON schemas
- Provides comprehensive guidelines for tool usage
- Ensures proper JSON formatting for tool calls
- Integrates with Microsoft.Extensions.AI tool framework

## Current State and Limitations

### Implemented Features
- ✅ Blazor Server real-time chat interface with conversation history
- ✅ Multi-provider LLM support (Google Gemini, OpenRouter)
- ✅ Web-based settings management with dynamic client switching
- ✅ MCP server with comprehensive tool set integrated as AI tools
- ✅ File system operations with safety features
- ✅ Search capabilities (semantic, regex, fuzzy)
- ✅ Terminal command execution (PowerShell)
- ✅ Dynamic tool discovery and registration
- ✅ Cross-platform web interface (no longer Windows-only)
- ✅ In-process MCP tool integration

### Known Limitations
- Windows PowerShell-specific terminal commands (could be made cross-platform)
- Some preview features (parallel_apply handler)
- No user approval workflow for file modifications
- Web-only interface (no offline desktop app)

### Future Enhancements (from Requirements.txt)
- Multi-role LLM sessions (Requirements Analyst, Developer, Code Verifier)
- Plugin system for extensibility
- LLM multi-agent collaboration
- Code refactoring assistance
- Automated debugging and error detection
- Interactive validation before applying modifications

## Development Workflow

### Target Use Case
OpenCursor is designed as an auxiliary web-based tool to complement traditional IDEs:
1. Developer accesses the web interface from any browser
2. Configures preferred LLM provider (OpenRouter or Gemini) via settings
3. Communicates with LLM about development tasks through real-time chat
4. LLM suggests code changes via structured MCP tool calls
5. Commands are executed safely with automatic backups
6. Results are displayed in real-time through the web interface
7. Can be accessed remotely or shared across development teams

### Self-Improving Goal
The long-term vision includes the tool being capable of extending itself - using its own MCP commands to modify and enhance its capabilities, creating a self-improving development assistant.

## Security Considerations
- API key management through configuration files
- Workspace-relative path enforcement
- File backup creation before modifications
- Comprehensive input validation
- Secure command execution patterns

This analysis reflects the current state of a sophisticated AI-assisted development tool that bridges LLM capabilities with practical file system operations through a well-structured MCP architecture. The migration from WPF to Blazor Server has transformed it into a modern, cross-platform web application with real-time capabilities and multi-provider LLM support.