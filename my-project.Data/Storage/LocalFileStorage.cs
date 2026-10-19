using my_project.Core.Interfaces.Infrastructure;

namespace my_project.Data.Storage;

// Stores uploaded files in a local folder (uploads/). Only the stored name is kept in the database
public class LocalFileStorage : IFileStorage
{
    private const int BufferSize = 81920;

    private readonly string _rootPath;

    public LocalFileStorage(string rootPath)
    {
        _rootPath = rootPath;
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken cancellationToken = default)
    {
        var storedFileName = Guid.NewGuid().ToString("N") + Path.GetExtension(originalFileName).ToLowerInvariant();
        var path = GetPath(storedFileName);

        try
        {
            await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);
            await content.CopyToAsync(file, cancellationToken);
        }
        catch
        {
            // Do not leave a half-written file behind
            File.Delete(path);
            throw;
        }

        return storedFileName;
    }

    // Throws FileNotFoundException if the file does not exist
    public Task<Stream> OpenReadAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        Stream stream = new FileStream(GetPath(storedFileName), FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        // File.Delete does nothing if the file does not exist
        File.Delete(GetPath(storedFileName));
        return Task.CompletedTask;
    }

    // The stored name must be a plain file name – never a path that leaves the uploads folder
    private string GetPath(string storedFileName)
    {
        if (string.IsNullOrWhiteSpace(storedFileName) || Path.GetFileName(storedFileName) != storedFileName)
        {
            throw new ArgumentException("Invalid stored file name.", nameof(storedFileName));
        }

        return Path.Combine(_rootPath, storedFileName);
    }
}
