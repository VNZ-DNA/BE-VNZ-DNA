using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MediaService = VNZ.Service.Utils.MediaService;
using VNZ.Service.Exceptions;

namespace VNZ.Service.Utils.CloudinaryService;

public class Service : MediaService.IService
{
    private const long MaxFileBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "jpg",
        [".jpeg"] = "jpg",
        [".png"] = "png",
        [".gif"] = "gif",
        [".webp"] = "webp"
    };

    private static readonly Dictionary<string, string> AllowedAudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        [".mp3"] = "mp3",
        [".wav"] = "wav",
        [".m4a"] = "m4a",
        [".ogg"] = "ogg",
        [".flac"] = "flac"
    };

    private readonly Cloudinary _cloudinary;
    private readonly ILogger<Service> _logger;

    public Service(IConfiguration configuration, ILogger<Service> logger)
    {
        _logger = logger;

        var options = new CloudinaryOptions();
        configuration.GetSection(nameof(CloudinaryOptions)).Bind(options);

        var cloudName = options.CloudName.Trim();
        var apiKey = options.ApiKey.Trim();
        var apiSecret = options.ApiSecret.Trim();

        if (string.IsNullOrWhiteSpace(cloudName)
            || string.IsNullOrWhiteSpace(apiKey)
            || string.IsNullOrWhiteSpace(apiSecret))
        {
            throw new MediaException(
                "MEDIA_CONFIGURATION_INVALID",
                "Cấu hình Cloudinary chưa đầy đủ.");
        }

        _cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));
    }

    public async Task<MediaService.Response.UploadImageResponse> UploadImageAsync(
        MediaService.Request.UploadImageRequest request)
    {
        if (request is null)
        {
            throw new MediaException(
                "MEDIA_FILE_REQUIRED",
                "Vui lòng chọn hình ảnh cần tải lên.",
                "file");
        }

        var folder = ResolveFolder(request.Purpose);
        var file = request.File;
        var detectedFormat = await ValidateImageAsync(file);

        if (file is null)
        {
            throw new MediaException(
                "MEDIA_FILE_REQUIRED",
                "Vui lòng chọn hình ảnh cần tải lên.",
                "file");
        }

        await using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream)
        };

        if (folder is not null)
        {
            uploadParams.Folder = folder;
        }

        ImageUploadResult uploadResult;

        try
        {
            uploadResult = await _cloudinary.UploadAsync(uploadParams);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                "Cloudinary upload failed. ExceptionType: {ExceptionType}",
                exception.GetType().Name);

            throw new MediaException(
                "MEDIA_UPLOAD_FAILED",
                "Không thể tải hình ảnh lên Cloudinary.",
                exception);
        }

        if (uploadResult.Error is not null)
        {
            _logger.LogError(
                "Cloudinary rejected upload. ErrorType: {ErrorType}",
                uploadResult.Error.GetType().Name);

            throw new MediaException(
                "MEDIA_UPLOAD_FAILED",
                "Không thể tải hình ảnh lên Cloudinary.");
        }

        var secureUrl = uploadResult.SecureUrl?.ToString();
        var publicId = uploadResult.PublicId;

        if (string.IsNullOrWhiteSpace(secureUrl) || string.IsNullOrWhiteSpace(publicId))
        {
            throw new MediaException(
                "MEDIA_UPLOAD_FAILED",
                "Cloudinary không trả về đầy đủ thông tin hình ảnh.");
        }

        return new MediaService.Response.UploadImageResponse
        {
            Url = secureUrl,
            PublicId = publicId,
            Format = string.IsNullOrWhiteSpace(uploadResult.Format)
                ? detectedFormat
                : uploadResult.Format,
            Bytes = uploadResult.Bytes,
            Width = uploadResult.Width,
            Height = uploadResult.Height
        };
    }

    public async Task<MediaService.Response.UploadAudioResponse> UploadAudioAsync(
        MediaService.Request.UploadAudioRequest request)
    {
        if (request is null)
        {
            throw new MediaException(
                "MEDIA_FILE_REQUIRED",
                "Vui lòng chọn file nhạc cần tải lên.",
                "file");
        }

        var folder = ResolveFolder(request.Purpose);
        var file = request.File;
        var detectedFormat = await ValidateAudioAsync(file);

        if (file is null)
        {
            throw new MediaException(
                "MEDIA_FILE_REQUIRED",
                "Vui lòng chọn file nhạc cần tải lên.",
                "file");
        }

        await using var stream = file.OpenReadStream();
        var uploadParams = new VideoUploadParams
        {
            File = new FileDescription(file.FileName, stream)
        };

        if (folder is not null)
        {
            uploadParams.Folder = folder;
        }

        VideoUploadResult uploadResult;

        try
        {
            uploadResult = await _cloudinary.UploadAsync(uploadParams);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                "Cloudinary audio upload failed. ExceptionType: {ExceptionType}",
                exception.GetType().Name);

            throw new MediaException(
                "MEDIA_UPLOAD_FAILED",
                "Không thể tải file nhạc lên Cloudinary.",
                exception);
        }

        if (uploadResult.Error is not null)
        {
            _logger.LogError(
                "Cloudinary rejected audio upload. ErrorType: {ErrorType}",
                uploadResult.Error.GetType().Name);

            throw new MediaException(
                "MEDIA_UPLOAD_FAILED",
                "Không thể tải file nhạc lên Cloudinary.");
        }

        var secureUrl = uploadResult.SecureUrl?.ToString();
        var publicId = uploadResult.PublicId;

        if (string.IsNullOrWhiteSpace(secureUrl) || string.IsNullOrWhiteSpace(publicId))
        {
            throw new MediaException(
                "MEDIA_UPLOAD_FAILED",
                "Cloudinary không trả về đầy đủ thông tin file nhạc.");
        }

        return new MediaService.Response.UploadAudioResponse
        {
            Url = secureUrl,
            PublicId = publicId,
            Format = string.IsNullOrWhiteSpace(uploadResult.Format)
                ? detectedFormat
                : uploadResult.Format,
            Bytes = uploadResult.Bytes
        };
    }

    private static async Task<string> ValidateImageAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            throw new MediaException(
                "MEDIA_FILE_REQUIRED",
                "Vui lòng chọn hình ảnh cần tải lên.",
                "file");
        }

        if (file.Length > MaxFileBytes)
        {
            throw new MediaException(
                "MEDIA_FILE_TOO_LARGE",
                "Kích thước hình ảnh không được vượt quá 5 MB.",
                "file");
        }

        var extension = Path.GetExtension(file.FileName);

        if (!AllowedExtensions.TryGetValue(extension, out var expectedFormat))
        {
            throw new MediaException(
                "MEDIA_FILE_TYPE_UNSUPPORTED",
                "Chỉ hỗ trợ hình ảnh JPG, JPEG, PNG, GIF hoặc WebP.",
                "file");
        }

        await using var stream = file.OpenReadStream();
        var header = new byte[12];
        var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length));
        var detectedFormat = DetectFormat(header, bytesRead);

        if (detectedFormat is null || !string.Equals(detectedFormat, expectedFormat, StringComparison.OrdinalIgnoreCase))
        {
            throw new MediaException(
                "MEDIA_FILE_TYPE_UNSUPPORTED",
                "Định dạng thực tế của hình ảnh không khớp với phần mở rộng.",
                "file");
        }

        return detectedFormat;
    }

    private static async Task<string> ValidateAudioAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            throw new MediaException(
                "MEDIA_FILE_REQUIRED",
                "Vui lòng chọn file nhạc cần tải lên.",
                "file");
        }

        if (file.Length > MaxFileBytes)
        {
            throw new MediaException(
                "MEDIA_FILE_TOO_LARGE",
                "Kích thước file nhạc không được vượt quá 5 MB.",
                "file");
        }

        var extension = Path.GetExtension(file.FileName);

        if (!AllowedAudioExtensions.TryGetValue(extension, out var expectedFormat))
        {
            throw new MediaException(
                "MEDIA_FILE_TYPE_UNSUPPORTED",
                "Chỉ hỗ trợ file nhạc MP3, WAV, M4A, OGG hoặc FLAC.",
                "file");
        }

        await using var stream = file.OpenReadStream();
        var header = new byte[12];
        var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length));
        var detectedFormat = DetectAudioFormat(header, bytesRead);

        if (detectedFormat is null || !string.Equals(detectedFormat, expectedFormat, StringComparison.OrdinalIgnoreCase))
        {
            throw new MediaException(
                "MEDIA_FILE_TYPE_UNSUPPORTED",
                "Định dạng thực tế của file nhạc không khớp với phần mở rộng.",
                "file");
        }

        return detectedFormat;
    }

    private static string? DetectFormat(byte[] header, int bytesRead)
    {
        if (bytesRead >= 3
            && header[0] == 0xFF
            && header[1] == 0xD8
            && header[2] == 0xFF)
        {
            return "jpg";
        }

        if (bytesRead >= 8
            && header[0] == 0x89
            && header[1] == 0x50
            && header[2] == 0x4E
            && header[3] == 0x47
            && header[4] == 0x0D
            && header[5] == 0x0A
            && header[6] == 0x1A
            && header[7] == 0x0A)
        {
            return "png";
        }

        if (bytesRead >= 6
            && header[0] == 'G'
            && header[1] == 'I'
            && header[2] == 'F'
            && header[3] == '8'
            && (header[4] == '7' || header[4] == '9')
            && header[5] == 'a')
        {
            return "gif";
        }

        if (bytesRead >= 12
            && header[0] == 'R'
            && header[1] == 'I'
            && header[2] == 'F'
            && header[3] == 'F'
            && header[8] == 'W'
            && header[9] == 'E'
            && header[10] == 'B'
            && header[11] == 'P')
        {
            return "webp";
        }

        return null;
    }

    private static string? DetectAudioFormat(byte[] header, int bytesRead)
    {
        if (bytesRead >= 3
            && header[0] == 'I'
            && header[1] == 'D'
            && header[2] == '3')
        {
            return "mp3";
        }

        if (bytesRead >= 2
            && header[0] == 0xFF
            && (header[1] & 0xE0) == 0xE0)
        {
            return "mp3";
        }

        if (bytesRead >= 12
            && header[0] == 'R'
            && header[1] == 'I'
            && header[2] == 'F'
            && header[3] == 'F'
            && header[8] == 'W'
            && header[9] == 'A'
            && header[10] == 'V'
            && header[11] == 'E')
        {
            return "wav";
        }

        if (bytesRead >= 4
            && header[0] == 'O'
            && header[1] == 'g'
            && header[2] == 'g'
            && header[3] == 'S')
        {
            return "ogg";
        }

        if (bytesRead >= 4
            && header[0] == 'f'
            && header[1] == 'L'
            && header[2] == 'a'
            && header[3] == 'C')
        {
            return "flac";
        }

        if (bytesRead >= 8
            && header[4] == 'f'
            && header[5] == 't'
            && header[6] == 'y'
            && header[7] == 'p')
        {
            return "m4a";
        }

        return null;
    }

    private static string? ResolveFolder(string? purpose)
    {
        if (string.IsNullOrWhiteSpace(purpose))
        {
            return null;
        }

        var normalizedPurpose = purpose.Trim();

        if (string.Equals(normalizedPurpose, "ProductLogo", StringComparison.Ordinal))
        {
            return "vnz/products";
        }

        if (string.Equals(normalizedPurpose, "NewsImage", StringComparison.Ordinal))
        {
            return "vnz/news";
        }

        if (string.Equals(normalizedPurpose, "TeamMemberAvatar", StringComparison.Ordinal))
        {
            return "vnz/team-members";
        }

        if (string.Equals(normalizedPurpose, "TeamMemberBackground", StringComparison.Ordinal))
        {
            return "vnz/team-members/backgrounds";
        }

        if (string.Equals(normalizedPurpose, "TeamMemberAudio", StringComparison.Ordinal))
        {
            return "vnz/team-members/audio";
        }

        if (string.Equals(normalizedPurpose, "PartnerLogo", StringComparison.Ordinal))
        {
            return "vnz/partners";
        }

        throw new MediaException(
            "MEDIA_PURPOSE_UNSUPPORTED",
            "Mục đích tải media không được hỗ trợ.",
            "purpose");
    }
}
