namespace VNZ.Test;

internal sealed class TestMediaService : VNZ.Service.Utils.MediaService.IService
{
    public Task<VNZ.Service.Utils.MediaService.Response.UploadImageResponse> UploadImageAsync(
        VNZ.Service.Utils.MediaService.Request.UploadImageRequest request)
    {
        return Task.FromResult(new VNZ.Service.Utils.MediaService.Response.UploadImageResponse
        {
            Url = "https://cdn.example.com/test-image.png",
            PublicId = "test-image",
            Format = "png"
        });
    }
}
