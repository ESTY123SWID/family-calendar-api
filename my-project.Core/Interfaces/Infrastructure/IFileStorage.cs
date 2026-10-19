namespace my_project.Core.Interfaces.Infrastructure;

// Stores uploaded files outside the database
public interface IFileStorage
{
    // Saves the content under a new unique name (GUID + the extension of originalFileName) and returns that name
    Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string storedFileName, CancellationToken cancellationToken = default);

    // Does nothing if the file does not exist
    Task DeleteAsync(string storedFileName, CancellationToken cancellationToken = default);
}
