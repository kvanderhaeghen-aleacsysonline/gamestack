namespace Gamestack.Tests.Support;

/// <summary>A throwaway temp directory, deleted on dispose.</summary>
public sealed class TempDir : IDisposable
{
    /// <summary>Absolute path of the temp directory.</summary>
    public string Path { get; }

    public TempDir()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gamestack-tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>Combine a relative path onto the temp root.</summary>
    public string File(string relative) => System.IO.Path.Combine(Path, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));

    /// <summary>Write text to a file under the temp root, creating directories as needed.</summary>
    public string WriteText(string relative, string content)
    {
        var full = File(relative);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        System.IO.File.WriteAllText(full, content);
        return full;
    }

    public void Dispose()
    {
        try { Directory.Delete(Path, recursive: true); } catch (IOException) { /* best effort */ }
    }
}
