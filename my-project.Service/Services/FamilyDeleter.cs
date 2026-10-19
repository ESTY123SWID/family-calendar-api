using Microsoft.Extensions.Logging;
using my_project.Core.Entities;
using my_project.Core.Interfaces;
using my_project.Core.Interfaces.Infrastructure;

namespace my_project.Service.Services;

// Deletes a family (the database cascades to members, events, tasks...) and then its files on disk.
// Shared by FamilyService (family manager) and AdminService (system admin)
public class FamilyDeleter
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<FamilyDeleter> _logger;

    public FamilyDeleter(IUnitOfWork unitOfWork, IFileStorage fileStorage, ILogger<FamilyDeleter> logger)
    {
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task DeleteAsync(Family family, CancellationToken cancellationToken)
    {
        // Read the file names before the rows are gone
        var storedFileNames = await _unitOfWork.EventAttachments
            .GetStoredFileNamesForFamilyAsync(family.Id, cancellationToken);

        _unitOfWork.Families.Remove(family);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Family {FamilyId} deleted", family.Id);

        // Files are removed only after the database delete succeeded.
        // A file that fails to delete is an orphan on disk, not a broken request
        foreach (var storedFileName in storedFileNames)
        {
            try
            {
                await _fileStorage.DeleteAsync(storedFileName, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(exception, "Failed to delete file {StoredFileName} of family {FamilyId}", storedFileName, family.Id);
            }
        }
    }
}
