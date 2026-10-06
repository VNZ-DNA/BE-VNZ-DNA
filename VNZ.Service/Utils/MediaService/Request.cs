using Microsoft.AspNetCore.Http;

namespace VNZ.Service.Utils.MediaService;

public class Request
{
    public class UploadImageRequest
    {
        public IFormFile? File { get; set; }
        public string? Purpose { get; set; }
    }

    public class UploadAudioRequest
    {
        public IFormFile? File { get; set; }
        public string? Purpose { get; set; }
    }
}
