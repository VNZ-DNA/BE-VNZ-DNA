using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.Exceptions;

namespace VNZ.Service.TeamMembers;

public sealed class Service : IService
{
    private const string DefaultMemberPassword = "Vnz@123456";

    private readonly AppDbContext _dbContext;

    public Service(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Response.TeamMemberResponse> GetMemberByIdAsync(Guid id)
    {
        var member = await FindMemberAsync(id);
        return ToResponse(member);
    }

    public async Task<Response.TeamMemberResponse> CreateMemberAsync(Request.CreateTeamMemberRequest request, Guid createdBy)
    {
        ArgumentNullException.ThrowIfNull(request);

        var fullName = request.FullName?.Trim();
        var email = request.Email?.Trim().ToLowerInvariant();
        var position = request.Position?.Trim();
        var jobLevel = request.JobLevel?.Trim();

        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Vui lòng nhập họ và tên.");

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Vui lòng nhập email.");

        if (string.IsNullOrWhiteSpace(position))
            throw new ArgumentException("Vui lòng nhập vị trí.");

        if (string.IsNullOrWhiteSpace(jobLevel))
            throw new ArgumentException("Vui lòng nhập cấp bậc.");

        if (request.JoinedDate is null)
            throw new ArgumentException("Vui lòng nhập ngày tham gia.");

        if (fullName!.Length > 200)
            throw new ArgumentException("Họ và tên không được vượt quá 200 ký tự.");

        if (position!.Length > 200)
            throw new ArgumentException("Vị trí không được vượt quá 200 ký tự.");

        if (email!.Length > 320 || !new EmailAddressAttribute().IsValid(email))
            throw new ArgumentException("Email không đúng định dạng.");

        if (!Enum.TryParse<JobLevel>(jobLevel, true, out var parsedJobLevel) ||
            !Enum.IsDefined(typeof(JobLevel), parsedJobLevel))
        {
            throw new ArgumentException("Cấp bậc không hợp lệ.");
        }

        if (await _dbContext.Users.AnyAsync(x => x.Email.ToLower() == email))
            throw new ArgumentException("Email đã tồn tại trong hệ thống.");

        var now = DateTimeOffset.UtcNow;
        var member = new User
        {
            Id = Guid.NewGuid(),
            CreatedBy = createdBy,
            FullName = fullName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultMemberPassword),
            Position = position,
            JobLevel = parsedJobLevel,
            AvatarUrl = NormalizeOptional(request.AvatarUrl),
            AnimationUrl = NormalizeOptional(request.AnimationUrl),
            AudioUrl = NormalizeOptional(request.AudioUrl),
            Hometown = NormalizeOptional(request.Hometown),
            Hobbies = NormalizeOptional(request.Hobbies),
            PersonalQuote = NormalizeOptional(request.PersonalQuote),
            JoinedDate = request.JoinedDate,
            IsActive = true,
            EmploymentStatus = EmploymentStatus.Working,
            IsPublished = false,
            DisplayOrder = null,
            CreateAt = now,
            ResetPasswordCode = 0
        };

        _dbContext.Users.Add(member);
        await _dbContext.SaveChangesAsync();

        return ToResponse(member);
    }

    public async Task<Response.TeamMemberResponse> UpdateMemberAsync(
        Guid id,
        Request.UpdateTeamMemberRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var member = await FindMemberAsync(id);

        var fullName = request.FullName?.Trim();
        var email = request.Email?.Trim().ToLowerInvariant();
        var position = request.Position?.Trim();
        var jobLevel = request.JobLevel?.Trim();
        var employmentStatus = request.EmploymentStatus?.Trim();

        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Vui lòng nhập họ và tên.");

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Vui lòng nhập email.");

        if (string.IsNullOrWhiteSpace(position))
            throw new ArgumentException("Vui lòng nhập vị trí.");

        if (string.IsNullOrWhiteSpace(jobLevel))
            throw new ArgumentException("Vui lòng nhập cấp bậc.");

        if (request.JoinedDate is null)
            throw new ArgumentException("Vui lòng nhập ngày tham gia.");

        if (string.IsNullOrWhiteSpace(employmentStatus))
            throw new ArgumentException("Vui lòng nhập trạng thái làm việc.");

        if (fullName.Length > 200)
            throw new ArgumentException("Họ và tên không được vượt quá 200 ký tự.");

        if (position.Length > 200)
            throw new ArgumentException("Vị trí không được vượt quá 200 ký tự.");

        if (email.Length > 320 || !new EmailAddressAttribute().IsValid(email))
            throw new ArgumentException("Email không đúng định dạng.");

        if (!Enum.TryParse<JobLevel>(jobLevel, true, out var parsedJobLevel) ||
            !Enum.IsDefined(typeof(JobLevel), parsedJobLevel))
        {
            throw new ArgumentException("Cấp bậc không hợp lệ.");
        }

        if (!Enum.TryParse<EmploymentStatus>(employmentStatus, true, out var parsedEmploymentStatus) ||
            !Enum.IsDefined(typeof(EmploymentStatus), parsedEmploymentStatus))
        {
            throw new ArgumentException("Trạng thái làm việc không hợp lệ.");
        }

        var avatarUrl = NormalizeOptional(request.AvatarUrl);
        var animationUrl = NormalizeOptional(request.AnimationUrl);
        var audioUrl = NormalizeOptional(request.AudioUrl);
        var hometown = NormalizeOptional(request.Hometown);
        var hobbies = NormalizeOptional(request.Hobbies);
        var personalQuote = NormalizeOptional(request.PersonalQuote);

        var hasInformationOrEmploymentStatusChanges =
            member.FullName != fullName ||
            member.Email != email ||
            member.Position != position ||
            member.JobLevel != parsedJobLevel ||
            member.JoinedDate != request.JoinedDate ||
            member.AvatarUrl != avatarUrl ||
            member.AnimationUrl != animationUrl ||
            member.AudioUrl != audioUrl ||
            member.Hometown != hometown ||
            member.Hobbies != hobbies ||
            member.PersonalQuote != personalQuote ||
            member.EmploymentStatus != parsedEmploymentStatus;

        if (member.IsPublished && hasInformationOrEmploymentStatusChanges)
        {
            throw new ConflictException(
                "Không thể chỉnh sửa thông tin hoặc trạng thái làm việc của thành viên đang được đăng. Vui lòng gỡ đăng trước.");
        }

        var emailUsedByAnotherMember = await _dbContext.Users
            .AnyAsync(x => x.Id != id && x.Email.ToLower() == email);

        if (emailUsedByAnotherMember)
            throw new ArgumentException("Email đã tồn tại trong hệ thống.");

        member.FullName = fullName;
        member.Email = email;
        member.Position = position;
        member.JobLevel = parsedJobLevel;
        member.JoinedDate = request.JoinedDate;
        member.AvatarUrl = avatarUrl;
        member.AnimationUrl = animationUrl;
        member.AudioUrl = audioUrl;
        member.Hometown = hometown;
        member.Hobbies = hobbies;
        member.PersonalQuote = personalQuote;
        member.EmploymentStatus = parsedEmploymentStatus;
        if (request.IsPublished.HasValue)
        {
            member.IsPublished = request.IsPublished.Value;
        }
        member.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();
        return ToResponse(member);
    }

    private async Task<User> FindMemberAsync(Guid id)
    {
        var member = await _dbContext.Users.SingleOrDefaultAsync(x => x.Id == id);
        if (member is null)
            throw new NotFoundException("Không tìm thấy thành viên.");

        return member;
    }

    private static Response.TeamMemberResponse ToResponse(User member)
    {
        return new Response.TeamMemberResponse
        {
            Id = member.Id,
            RoleId = member.RoleId,
            CreatedBy = member.CreatedBy,
            FullName = member.FullName,
            Email = member.Email,
            Position = member.Position,
            JobLevel = member.JobLevel,
            AvatarUrl = member.AvatarUrl,
            AnimationUrl = member.AnimationUrl,
            AudioUrl = member.AudioUrl,
            Hometown = member.Hometown,
            Hobbies = member.Hobbies,
            PersonalQuote = member.PersonalQuote,
            JoinedDate = member.JoinedDate,
            EmploymentStatus = member.EmploymentStatus,
            IsActive = member.IsActive,
            IsPublished = member.IsPublished,
            DisplayOrder = member.DisplayOrder,
            CreatedAt = member.CreateAt,
            UpdatedAt = member.UpdatedAt
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
