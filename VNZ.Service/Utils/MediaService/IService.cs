namespace VNZ.Service.Utils.MediaService;

public interface IService
{
    Task<Response.UploadImageResponse> UploadImageAsync(Request.UploadImageRequest request);
    Task<Response.UploadAudioResponse> UploadAudioAsync(Request.UploadAudioRequest request);
}
