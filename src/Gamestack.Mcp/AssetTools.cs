using System.ComponentModel;
using System.Text.Json;
using Gamestack.Core.Models;
using Gamestack.Core.Organization;
using Gamestack.Core.Versioning;
using ModelContextProtocol.Server;

namespace Gamestack.Mcp;

/// <summary>
/// Read-only Model Context Protocol tools that let an AI agent (Claude, Cursor, VS Code) discover
/// Gamestack assets and their metadata. All tools read the shared workspace manifest; none mutate it.
/// </summary>
[McpServerToolType]
public static class AssetTools
{
    private static string Serialize(object value) => JsonSerializer.Serialize(value, ManifestService.JsonOptions);

    [McpServerTool(Name = "search_assets")]
    [Description("Search tracked assets by name substring, tags, file type, game id, and/or a custom " +
                 "attribute. All supplied filters must match. Returns matching assets with their path, " +
                 "current version, and tags.")]
    public static async Task<string> SearchAssets(
        WorkspaceManifestAccessor workspace,
        CancellationToken ct,
        [Description("Case-insensitive substring matched against the asset's path.")] string? text = null,
        [Description("Comma-separated tags that must ALL be present (case-insensitive).")] string? tags = null,
        [Description("File extension to match, with or without a dot (e.g. 'png' or '.psd').")] string? extension = null,
        [Description("Game id to match against the workspace's linked game.")] string? gameId = null,
        [Description("Custom attribute key to filter on (use together with attributeValue).")] string? attributeKey = null,
        [Description("Custom attribute value to match (use together with attributeKey).")] string? attributeValue = null)
    {
        var manifest = await workspace.LoadManifestAsync(ct).ConfigureAwait(false);

        var tagList = (tags ?? "")
            .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var attributes = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(attributeKey) && !string.IsNullOrWhiteSpace(attributeValue))
            attributes[attributeKey.Trim()] = attributeValue.Trim();

        var query = new SearchQuery
        {
            Text = string.IsNullOrWhiteSpace(text) ? null : text.Trim(),
            Tags = tagList,
            Extension = string.IsNullOrWhiteSpace(extension) ? null : extension.Trim(),
            GameId = string.IsNullOrWhiteSpace(gameId) ? null : gameId.Trim(),
            Attributes = attributes,
        };

        var results = AssetSearch.Search(manifest, query)
            .Select(r => new
            {
                path = r.Path,
                currentVersion = r.File.CurrentVersion,
                tags = r.File.Tags,
                attributes = r.File.Attributes,
            });
        return Serialize(new { count = results.Count(), results });
    }

    [McpServerTool(Name = "get_asset")]
    [Description("Get full metadata for one asset by its workspace-relative path: version history " +
                 "(author, time, description, size, review status), tags, custom attributes, and the " +
                 "feedback comment thread.")]
    public static async Task<string> GetAsset(
        WorkspaceManifestAccessor workspace,
        CancellationToken ct,
        [Description("Workspace-relative path of the asset (as returned by search_assets/list_assets).")] string path)
    {
        var manifest = await workspace.LoadManifestAsync(ct).ConfigureAwait(false);
        if (!manifest.Files.TryGetValue(path, out var file))
            return Serialize(new { error = $"No tracked asset at '{path}'." });

        return Serialize(new
        {
            path,
            currentVersion = file.CurrentVersion,
            tags = file.Tags,
            attributes = file.Attributes,
            versions = file.Versions.Select(v => new
            {
                v.Version,
                pushedBy = v.PushedBy.DisplayName,
                v.PushedAtUtc,
                v.Description,
                v.Size,
                review = v.Review is { } r ? $"{r.Status} (reviewer {r.Reviewer.DisplayName})" : null,
            }),
            comments = file.Comments.Select(c => new
            {
                author = c.Author.DisplayName,
                c.AtUtc,
                c.Version,
                kind = c.Kind.ToString(),
                c.Text,
            }),
        });
    }

    [McpServerTool(Name = "list_assets")]
    [Description("List every tracked asset in the workspace with its current version and tags.")]
    public static async Task<string> ListAssets(WorkspaceManifestAccessor workspace, CancellationToken ct)
    {
        var manifest = await workspace.LoadManifestAsync(ct).ConfigureAwait(false);
        var assets = manifest.Files
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => new { path = kv.Key, currentVersion = kv.Value.CurrentVersion, tags = kv.Value.Tags });
        return Serialize(new { count = manifest.Files.Count, assets });
    }

    [McpServerTool(Name = "list_tags")]
    [Description("List the workspace-wide tag vocabulary available for tagging and searching assets.")]
    public static async Task<string> ListTags(WorkspaceManifestAccessor workspace, CancellationToken ct)
    {
        var manifest = await workspace.LoadManifestAsync(ct).ConfigureAwait(false);
        return Serialize(new { tags = manifest.Tags });
    }

    [McpServerTool(Name = "get_workspace_info")]
    [Description("Get workspace summary: project name, linked game (id/slug), tracked-file count, " +
                 "tag vocabulary size, and the defined custom-attribute fields.")]
    public static async Task<string> GetWorkspaceInfo(WorkspaceManifestAccessor workspace, CancellationToken ct)
    {
        var manifest = await workspace.LoadManifestAsync(ct).ConfigureAwait(false);
        return Serialize(new
        {
            name = manifest.Name,
            gameId = manifest.GameId,
            gameSlug = manifest.GameSlug,
            fileCount = manifest.Files.Count,
            tagCount = manifest.Tags.Count,
            attributeDefinitions = manifest.AttributeDefinitions.Select(d => new { d.Key, type = d.Type.ToString() }),
        });
    }
}
