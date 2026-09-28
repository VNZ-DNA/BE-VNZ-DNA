namespace VNZ.Service.Utils.MediaService;

public class Response
{
    public class UploadImageResponse
    {
        public required string Url { get; set; }
        public required string PublicId { get; set; }
        public required string Format { get; set; }
        public long Bytes { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
    }
}
