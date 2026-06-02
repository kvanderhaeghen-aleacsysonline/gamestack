using Gamestack.Core.Abstractions;
using Gamestack.Core.Versioning;
using Gamestack.Infrastructure;
using Gamestack.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Gamestack MCP server: exposes read-only asset discovery over stdio so AI agents
// (Claude, Cursor, VS Code) can query the workspace manifest. See AssetTools for the tools.
var builder = Host.CreateApplicationBuilder(args);

// stdio transport uses stdout for JSON-RPC, so ALL logging must go to stderr.
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<ManifestService>();
builder.Services.AddSingleton<ISettingsStore>(_ => new JsonSettingsStore());
builder.Services.AddSingleton<WorkspaceManifestAccessor>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
