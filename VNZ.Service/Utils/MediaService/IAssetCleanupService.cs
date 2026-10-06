namespace VNZ.Service.Utils.MediaService;

/// <summary>
/// Optional cleanup capability for media providers. Uploads are deliberately
/// performed outside the database transaction; this capability lets a caller
/// remove assets when the subsequent database write fails.
/// </summary>
public interface IAssetCleanupService
{
    Task DeleteImageAsync(string publicId);
}
