# Claude API Integration

## Overview
A new Claude API client has been added to OpenCursor, providing integration with Anthropic's Claude API alongside the existing OpenRouter and Gemini clients.

## Implementation Details

### 1. ClaudeChatClient (`/OpenCursor.Host/LlmClient/ClaudeChatClient.cs`)
- **Full IChatClient Implementation**: Implements the Microsoft.Extensions.AI.IChatClient interface
- **Official API Integration**: Uses Anthropic's official Claude API (not browser automation)
- **Model Support**: Supports Claude 3.5 Sonnet, Claude 3.5 Haiku, and Claude 3 Opus
- **Streaming Support**: Implements both regular and streaming chat responses
- **Tool Calling**: Basic support for tool/function calling (simplified implementation)
- **Error Handling**: Proper HTTP error handling and response parsing

### 2. Configuration Updates

#### Settings Service (`SettingsService.cs`)
- Added `ClaudeApiKey` property to `AppSettings`
- Added `ClaudeModel` property with default to "claude-3-5-sonnet-20241022"
- Updated `ChatClient` options to include "Claude"

#### Chat Client Selector (`ChatClientSelectorService.cs`)
- Added switch case for "Claude" client selection
- Proper configuration passing with API key and model settings
- Maintains existing OpenRouter and Gemini client support

#### Application Configuration (`appsettings.json`)
- Added `ClaudeApiKey` placeholder configuration
- Added `ClaudeModel` default configuration

### 3. UI Updates

#### Settings Page (`Pages/Settings.razor`)
- Added Claude API Key input field
- Added Claude model selection dropdown with available models:
  - Claude 3.5 Sonnet (default)
  - Claude 3.5 Haiku
  - Claude 3 Opus
- Updated Chat Client selector to include Claude option

## Key Features

### API Compliance
- Uses official Anthropic Claude API endpoints
- Proper authentication with API keys
- Standard HTTP client implementation
- Follows Claude API message format and conventions

### Message Conversion
- Converts Microsoft.Extensions.AI messages to Claude API format
- Handles system messages as separate system parameter
- Supports user, assistant, and tool message types
- Proper content extraction and formatting

### Streaming Support
- Implements Server-Sent Events (SSE) parsing
- Real-time response streaming
- Proper cancellation token support
- Error handling for malformed stream chunks

### Tool Integration
- Basic tool calling support structure
- Converts AI tools to Claude's function schema format
- Handles tool responses and function calls
- Extensible for future MCP tool integration

## Configuration Guide

### 1. Obtain Claude API Key
- Sign up for Anthropic Claude API access
- Generate an API key from the Anthropic Console
- Note: This requires separate API billing, not Claude Pro subscription

### 2. Configure OpenCursor
```json
{
  "ClaudeApiKey": "sk-ant-api03-your-actual-api-key-here",
  "ClaudeModel": "claude-3-5-sonnet-20241022"
}
```

### 3. Select Claude Client
- Navigate to Settings page in OpenCursor
- Enter your Claude API key
- Select "Claude" as the Chat Client
- Choose your preferred Claude model
- Save settings

## Technical Notes

### Why API Instead of Pro Subscription
The implementation uses Anthropic's official API rather than browser automation for Claude Pro because:

1. **Security**: API keys are more secure than session management
2. **Reliability**: Official API is more stable than browser automation
3. **Compliance**: Follows Anthropic's terms of service
4. **Maintainability**: Easier to maintain than browser automation code
5. **Performance**: Direct API calls are faster than browser automation

### Limitations
- Requires Claude API access (separate from Pro subscription)
- API usage incurs separate billing
- Tool calling implementation is basic (can be enhanced)
- No access to Claude Pro features like artifact generation

### Future Enhancements
- Enhanced tool schema conversion
- Better error handling and retry logic
- Support for Claude's computer use capabilities
- Integration with Claude's function calling improvements
- Caching and rate limiting

## Usage
Once configured, Claude will appear as a chat client option alongside OpenRouter and Gemini, providing access to Claude's advanced reasoning capabilities through OpenCursor's MCP tool ecosystem.