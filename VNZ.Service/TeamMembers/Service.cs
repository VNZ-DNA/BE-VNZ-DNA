using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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

    public async Task<Response.PagedTeamMemberListResponse> GetMemberListAsync(
        Request.GetTeamMemberListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateListRequest(request);

        var search = request.Search?.Trim();
        var status = ParseEmploymentStatus(request.Status);

        try
        {
            var query = _dbContext.Users
                .AsNoTracking()
                .Where(member => member.RoleId == null);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.ToLowerInvariant();
                query = query.Where(member =>
                    member.FullName.ToLower().Contains(normalizedSearch) ||
                    member.Email.ToLower().Contains(normalizedSearch) ||
                    (member.Position != null && member.Position.ToLower().Contains(normalizedSearch)));
            }

            if (status.HasValue)
            {
                query = query.Where(member => member.EmploymentStatus == status.Value);
            }

            var total = await query.CountAsync();

            var members = await query
                .OrderBy(member => member.DisplayOrder == null)
                .ThenBy(member => member.DisplayOrder)
                .ThenByDescending(member => member.CreateAt)
                .ThenByDescending(member => member.Id)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new Response.PagedTeamMemberListResponse
            {
                Items = members.Select(ToResponse).ToList(),
                Page = request.Page,
                PageSize = request.PageSize,
                Total = total,
                TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)request.PageSize)
            };
        }
        catch (Exception exception) when (exception is not TeamMemberException &&
                                          exception is not OperationCanceledException)
        {
            throw new TeamMemberException(
                "MEMBER_LIST_READ_FAILED",
                "Không thể đọc danh sách thành viên.",
                exception);
        }
    }

    public async Task<List<Response.OrderableTeamMemberResponse>> GetOrderableMembersAsync()
    {
        var orderableMembers = await _dbContext.Users
            .AsNoTracking()
            .Where(member => member.RoleId == null &&
                             member.EmploymentStatus == EmploymentStatus.Working &&
                             member.IsPublished &&
                             member.DisplayOrder != null)
            .OrderBy(member => member.DisplayOrder)
            .ThenBy(member => member.Id)
            .ToListAsync();

        return orderableMembers
            .Select(ToOrderableResponse)
            .ToList();
    }

    private static void ValidateListRequest(Request.GetTeamMemberListRequest request)
    {
        var fields = new List<string>();

        if (request.Page < 1)
        {
            fields.Add("page");
        }

        if (request.PageSize < 1 || request.PageSize > 100)
        {
            fields.Add("pageSize");
        }

        if (request.Search?.Trim().Length > 300)
        {
            fields.Add("search");
        }

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            !string.Equals(request.Status.Trim(), nameof(EmploymentStatus.Working), StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.Status.Trim(), nameof(EmploymentStatus.Resigned), StringComparison.OrdinalIgnoreCase))
        {
            fields.Add("status");
        }

        if (fields.Count > 0)
        {
            throw new TeamMemberException(
                "MEMBER_QUERY_INVALID",
                "Thông tin truy vấn danh sách thành viên không hợp lệ.",
                fields.ToArray());
        }
    }

    private static EmploymentStatus? ParseEmploymentStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        return string.Equals(status.Trim(), nameof(EmploymentStatus.Working), StringComparison.OrdinalIgnoreCase)
            ? EmploymentStatus.Working
            : EmploymentStatus.Resigned;
    }

    public async Task<List<Response.OrderableTeamMemberResponse>> ReorderMembersAsync(
        Request.ReorderTeamMembersRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var orderedMemberIds = request.OrderedMemberIds;
        if (orderedMemberIds is null || orderedMemberIds.Count == 0 ||
            orderedMemberIds.Distinct().Count() != orderedMemberIds.Count)
        {
            throw new TeamMemberException(
                "MEMBER_ORDER_INVALID", "Danh sách sắp xếp không hợp lệ.", "orderedMemberIds");
        }

        try
        {
            await using var transaction = await _dbContext.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);

            var allMemberIds = (await _dbContext.Users
                .Where(member => member.RoleId == null)
                .Select(member => member.Id)
                .ToListAsync())
                .ToHashSet();

            if (orderedMemberIds.Any(id => !allMemberIds.Contains(id)))
            {
                throw new TeamMemberException(
                    "MEMBER_ORDER_INVALID",
                    "Danh sách chứa ID thành viên không tồn tại.",
                    "orderedMemberIds");
            }

            var orderableMembers = await _dbContext.Users
                .Where(member => member.RoleId == null &&
                                 member.EmploymentStatus == EmploymentStatus.Working &&
                                 member.IsPublished &&
                                 member.DisplayOrder != null)
                .OrderBy(member => member.DisplayOrder)
                .ThenBy(member => member.Id)
                .ToListAsync();

            var currentIds = orderableMembers.Select(member => member.Id).ToHashSet();
            if (currentIds.Count != orderedMemberIds.Count ||
                orderedMemberIds.Any(id => !currentIds.Contains(id)))
            {
                throw new TeamMemberException(
                    "MEMBER_ORDER_CONFLICT",
                    "Danh sách thành viên có thể sắp xếp đã thay đổi. Vui lòng tải lại và thử lại.",
                    "orderedMemberIds");
            }

            // Tra cứu theo ID một lần để cập nhật đúng entity theo payload mà không phải quét lại danh sách.
            var membersById = orderableMembers.ToDictionary(member => member.Id);
            var now = DateTimeOffset.UtcNow;
            for (var index = 0; index < orderedMemberIds.Count; index++)
            {
                var member = membersById[orderedMemberIds[index]];
                var newDisplayOrder = index + 1;
                if (member.DisplayOrder == newDisplayOrder)
                {
                    continue;
                }

                member.DisplayOrder = newDisplayOrder;
                member.UpdatedAt = now;
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return orderedMemberIds
                .Select(id => ToOrderableResponse(membersById[id]))
                .ToList();
        }
        catch (Exception exception) when (
            FindPostgresException(exception)?.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            throw new TeamMemberException(
                "MEMBER_ORDER_CONFLICT",
                "Danh sách thành viên đang đăng đã thay đổi. Vui lòng tải lại và thử lại.",
                exception);
        }
        catch (Exception exception) when (exception is DbUpdateException or PostgresException)
        {
            throw new TeamMemberException(
                "MEMBER_ORDER_UPDATE_FAILED",
                "Không thể lưu thứ tự thành viên.",
                exception);
        }
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
            throw new TeamMemberException(
                "MEMBER_VALIDATION_ERROR",
                "Vui lòng nhập họ và tên.",
                "fullName");

        if (string.IsNullOrWhiteSpace(email))
            throw new TeamMemberException(
                "MEMBER_VALIDATION_ERROR",
                "Vui lòng nhập email.",
                "email");

        if (string.IsNullOrWhiteSpace(position))
            throw new TeamMemberException(
                "MEMBER_VALIDATION_ERROR",
                "Vui lòng nhập vị trí.",
                "position");

        if (string.IsNullOrWhiteSpace(jobLevel))
            throw new TeamMemberException(
                "MEMBER_VALIDATION_ERROR",
                "Vui lòng nhập cấp bậc.",
                "jobLevel");

        if (request.JoinedDate is null)
            throw new TeamMemberException(
                "MEMBER_VALIDATION_ERROR",
                "Vui lòng nhập ngày tham gia.",
                "joinedDate");

        if (fullName!.Length > 200)
            throw new TeamMemberException(
                "MEMBER_VALIDATION_ERROR",
                "Họ và tên không được vượt quá 200 ký tự.",
                "fullName");

        if (position!.Length > 200)
            throw new TeamMemberException(
                "MEMBER_VALIDATION_ERROR",
                "Vị trí không được vượt quá 200 ký tự.",
                "position");

        if (email!.Length > 320 || !new EmailAddressAttribute().IsValid(email))
            throw new TeamMemberException(
                "MEMBER_VALIDATION_ERROR",
                "Email không đúng định dạng.",
                "email");

        if (!TryParseEnumValue(jobLevel, out JobLevel parsedJobLevel))
        {
            throw new TeamMemberException(
                "MEMBER_VALIDATION_ERROR",
                "Cấp bậc không hợp lệ.",
                "jobLevel");
        }

        if (await _dbContext.Users.AnyAsync(x => x.Email.ToLower() == email))
        {
            throw new TeamMemberException(
                "MEMBER_EMAIL_EXISTS",
                "Email đã tồn tại trong hệ thống.",
                "email");
        }

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

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        var member = await FindMemberAsync(id);

        if (member.IsPublished)
        {
            if (request.IsPublished != false)
            {
                throw new ConflictException(
                    "Thành viên đang được đăng chỉ có thể chuyển sang trạng thái chưa đăng.");
            }

            var remainingPublishedMembers = await _dbContext.Users
                .Where(x => x.RoleId == null &&
                            x.IsPublished &&
                            x.EmploymentStatus == EmploymentStatus.Working &&
                            x.Id != member.Id)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

            var now = DateTimeOffset.UtcNow;
            for (var index = 0; index < remainingPublishedMembers.Count; index++)
            {
                var remainingMember = remainingPublishedMembers[index];
                var newDisplayOrder = index + 1;
                if (remainingMember.DisplayOrder == newDisplayOrder)
                {
                    continue;
                }

                remainingMember.DisplayOrder = newDisplayOrder;
                remainingMember.UpdatedAt = now;
            }

            member.IsPublished = false;
            member.DisplayOrder = null;
            member.UpdatedAt = now;

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return ToResponse(member);
        }

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

        if (!TryParseEnumValue(jobLevel, out JobLevel parsedJobLevel))
        {
            throw new ArgumentException("Cấp bậc không hợp lệ.");
        }

        if (!TryParseEnumValue(employmentStatus, out EmploymentStatus parsedEmploymentStatus))
        {
            throw new ArgumentException("Trạng thái làm việc không hợp lệ.");
        }

        var avatarUrl = NormalizeOptional(request.AvatarUrl);
        var animationUrl = NormalizeOptional(request.AnimationUrl);
        var audioUrl = NormalizeOptional(request.AudioUrl);
        var hometown = NormalizeOptional(request.Hometown);
        var hobbies = NormalizeOptional(request.Hobbies);
        var personalQuote = NormalizeOptional(request.PersonalQuote);

        if (!member.IsPublished && request.IsPublished == true &&
            parsedEmploymentStatus != EmploymentStatus.Working)
        {
            throw new ConflictException(
                "Thành viên đã nghỉ việc không thể đăng.");
        }

        var emailUsedByAnotherMember = await _dbContext.Users
            .AnyAsync(x => x.Id != id && x.Email.ToLower() == email);

        if (emailUsedByAnotherMember)
        {
            throw new ConflictException(
                "Email đã tồn tại trong hệ thống.");
        }

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

            if (request.IsPublished.Value)
            {
                var publishedMembers = await _dbContext.Users
                    .Where(x => x.RoleId == null &&
                                x.IsPublished &&
                                x.EmploymentStatus == EmploymentStatus.Working &&
                                x.Id != member.Id)
                    .OrderBy(x => x.DisplayOrder)
                    .ThenBy(x => x.Id)
                    .ToListAsync();

                var now = DateTimeOffset.UtcNow;
                for (var index = 0; index < publishedMembers.Count; index++)
                {
                    var publishedMember = publishedMembers[index];
                    var normalizedDisplayOrder = index + 1;
                    if (publishedMember.DisplayOrder == normalizedDisplayOrder)
                    {
                        continue;
                    }

                    publishedMember.DisplayOrder = normalizedDisplayOrder;
                    publishedMember.UpdatedAt = now;
                }

                member.DisplayOrder = publishedMembers.Count + 1;
            }
            else
            {
                member.DisplayOrder = null;
            }
        }
        member.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return ToResponse(member);
    }

    private async Task<User> FindMemberAsync(Guid id)
    {
        var member = await _dbContext.Users.SingleOrDefaultAsync(x => x.Id == id);
        if (member is null)
            throw new NotFoundException("Không tìm thấy thành viên.");

        return member;
    }

    private static Response.OrderableTeamMemberResponse ToOrderableResponse(User member)
    {
        return new Response.OrderableTeamMemberResponse
        {
            Id = member.Id,
            FullName = member.FullName,
            Position = member.Position,
            AvatarUrl = member.AvatarUrl,
            DisplayOrder = member.DisplayOrder!.Value
        };
    }

    private static Response.TeamMemberResponse ToResponse(User member)
    {
        return new Response.TeamMemberResponse
        {
            Id = member.Id,
            FullName = member.FullName,
            Email = member.Email,
            Position = member.Position,
            JobLevel = member.JobLevel.HasValue
                ? GetDisplayName(member.JobLevel.Value)
                : null,
            AvatarUrl = member.AvatarUrl,
            AnimationUrl = member.AnimationUrl,
            AudioUrl = member.AudioUrl,
            Hometown = member.Hometown,
            Hobbies = member.Hobbies,
            PersonalQuote = member.PersonalQuote,
            JoinedDate = member.JoinedDate,
            EmploymentStatus = GetDisplayName(member.EmploymentStatus),
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

    private static PostgresException? FindPostgresException(Exception exception)
    {
        while (exception is not null)
        {
            if (exception is PostgresException postgresException)
            {
                return postgresException;
            }

            exception = exception.InnerException!;
        }

        return null;
    }

    private static string GetDisplayName<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        var member = typeof(TEnum).GetMember(value.ToString()).Single();

        return member.GetCustomAttributes(typeof(DisplayAttribute), inherit: false)
            .OfType<DisplayAttribute>()
            .SingleOrDefault()?
            .GetName() ?? value.ToString();
    }

    private static bool TryParseEnumValue<TEnum>(string value, out TEnum result)
        where TEnum : struct, Enum
    {
        if (Enum.TryParse(value, true, out result) && Enum.IsDefined(result))
        {
            return true;
        }

        foreach (var enumValue in Enum.GetValues<TEnum>())
        {
            if (string.Equals(GetDisplayName(enumValue), value, StringComparison.OrdinalIgnoreCase))
            {
                result = enumValue;
                return true;
            }
        }

        result = default;
        return false;
    }
}
