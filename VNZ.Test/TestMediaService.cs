namespace VNZ.Test;

internal sealed class TestMediaService : VNZ.Service.Utils.MediaService.IService
{
    public Task<VNZ.Service.Utils.MediaService.Response.UploadImageResponse> UploadImageAsync(
        VNZ.Service.Utils.MediaService.Request.UploadImageRequest request)
    {
        var url = request.Purpose == "TeamMemberBackground"
            ? "https://cdn.example.com/test-background.png"
            : "https://cdn.example.com/test-image.png";

        return Task.FromResult(new VNZ.Service.Utils.MediaService.Response.UploadImageResponse
        {
            Url = url,
            PublicId = "test-image",
            Format = "png"
        });
    }

    public Task<VNZ.Service.Utils.MediaService.Response.UploadAudioResponse> UploadAudioAsync(
        VNZ.Service.Utils.MediaService.Request.UploadAudioRequest request)
    {
        return Task.FromResult(new VNZ.Service.Utils.MediaService.Response.UploadAudioResponse
        {
            Url = "https://cdn.example.com/test-audio.mp3",
            PublicId = "test-audio",
            Format = "mp3"
        });
    }
}
