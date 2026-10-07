using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.Exceptions;
using MediaService = VNZ.Service.Utils.MediaService;

namespace VNZ.Service.TeamMembers;

public sealed class Service : IService
{
    private const string DefaultMemberPassword = "Vnz@123456";

    private readonly AppDbContext _dbContext;
    private readonly MediaService.IService _mediaService;

    public Service(
        AppDbContext dbContext,
        MediaService.IService mediaService)
    {
        _dbContext = dbContext;
        _mediaService = mediaService;
    }

    public async Task<Response.DeleteTeamMemberResponse> DeleteTeamMemberAsync(Guid id, Guid currentUserId)
    {
        if (id == currentUserId)
        {
            throw new TeamMemberException(
                "TEAM_MEMBER_DELETE_FORBIDDEN",
                "Không thể xóa chính tài khoản đang đăng nhập.");
        }

        var affectedRows = await _dbContext.Users
            .Where(member => member.Id == id &&
                             member.RoleId == null &&
                             !member.IsDelete &&
                             !(member.EmploymentStatus == EmploymentStatus.Working &&
                               member.IsPublished))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(member => member.IsDelete, true));

        if (affectedRows == 1)
        {
            return new Response.DeleteTeamMemberResponse { Id = id };
        }

        var memberExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(member => member.Id == id && member.RoleId == null && !member.IsDelete);

        if (!memberExists)
        {
            throw new TeamMemberException("MEMBER_NOT_FOUND", "Không tìm thấy thành viên.");
        }

        throw new TeamMemberException(
            "TEAM_MEMBER_DELETE_FORBIDDEN",
            "Không thể xóa thành viên đang hiển thị trên website.");
    }

    public async Task<Response.FeaturedTeamMembersResponse> GetFeaturedMembersAsync()
    {
        var query = _dbContext.Users
            .AsNoTracking()
            .Where(member => member.RoleId == null &&
                             member.EmploymentStatus == EmploymentStatus.Working &&
                             member.IsPublished);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(member => member.DisplayOrder == null)
            .ThenBy(member => member.DisplayOrder)
            .ThenByDescending(member => member.CreateAt)
            .ThenByDescending(member => member.Id)
            .Take(6)
            .Select(member => new Response.FeaturedTeamMemberResponse
            {
                Id = member.Id,
                DisplayName = member.DisplayName,
                FullName = member.FullName,
                Position = member.Position,
                JobLevel = member.JobLevel.HasValue
                    ? GetDisplayName(member.JobLevel.Value)
                    : null,
                AvatarUrl = member.AvatarUrl,
                Hometown = member.Hometown,
                BackgroundUrl = member.BackgroundUrl
            })
            .ToListAsync();

        foreach (var item in items)
        {
            item.DisplayName = RemoveDisplayNameDiacritics(item.DisplayName);
        }

        return new Response.FeaturedTeamMembersResponse
        {
            Total = total,
            Items = items
        };
    }

    public async Task<List<Response.PublicTeamMemberResponse>> GetPublicMemberListAsync()
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Where(member => member.RoleId == null &&
                             member.EmploymentStatus == EmploymentStatus.Working &&
                             member.IsPublished)
            .OrderBy(member => member.DisplayOrder == null)
            .ThenBy(member => member.DisplayOrder)
            .ThenByDescending(member => member.CreateAt)
            .ThenByDescending(member => member.Id)
            .Select(member => new Response.PublicTeamMemberResponse
            {
                Id = member.Id,
                FullName = member.FullName,
                Position = member.Position,
                AvatarUrl = member.AvatarUrl
            })
            .ToListAsync();
    }

    public async Task<Response.PublicTeamMemberDetailResponse> GetPublicMemberByIdAsync(Guid id)
    {
        var member = await _dbContext.Users
            .AsNoTracking()
            .Where(member => member.Id == id &&
                             member.RoleId == null &&
                             member.EmploymentStatus == EmploymentStatus.Working &&
                             member.IsPublished)
            .Select(member => new Response.PublicTeamMemberDetailResponse
            {
                Id = member.Id,
                FullName = member.FullName,
                Position = member.Position,
                JobLevel = member.JobLevel.HasValue
                    ? GetDisplayName(member.JobLevel.Value)
                    : null,
                AvatarUrl = member.AvatarUrl,
                Hometown = member.Hometown,
                BackgroundUrl = member.BackgroundUrl,
                Hobbies = member.Hobbies,
                JoinedDate = member.JoinedDate,
                PersonalQuote = member.PersonalQuote,
                AnimationUrl = member.AnimationUrl,
                AudioUrl = member.AudioUrl
            })
            .SingleOrDefaultAsync();

        if (member is null)
            throw new NotFoundException("Không tìm thấy thành viên.");

        return member;
    }

    public async Task<Response.PagedTeamMemberListResponse> GetMemberListAsync(
        Request.GetTeamMemberListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateListRequest(request);

        var search = request.Search?.Trim();
        var statuses = ParseEmploymentStatuses(request.Status);
        var positions = NormalizePositions(request.Position);
        var jobLevels = ParseJobLevels(request.JobLevel);

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
                    member.Email.ToLower().Contains(normalizedSearch));
            }

            if (statuses.Count > 0)
            {
                query = query.Where(member => statuses.Contains(member.EmploymentStatus));
            }

            if (positions.Count > 0)
            {
                query = query.Where(member =>
                    member.Position != null &&
                    positions.Contains(member.Position.Trim().ToLower()));
            }

            if (jobLevels.Count > 0)
            {
                query = query.Where(member =>
                    member.JobLevel.HasValue &&
                    jobLevels.Contains(member.JobLevel.Value));
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

    public async Task<Response.TeamMemberFilterOptionsResponse> GetFilterOptionsAsync()
    {
        try
        {
            var rawPositions = await _dbContext.Users
                .AsNoTracking()
                .Where(member => member.RoleId == null && member.Position != null)
                .Select(member => member.Position!)
                .ToListAsync();

            var positions = rawPositions
                .Select(position => position.Trim())
                .Where(position => position.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(position => position, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var jobLevels = Enum.GetValues<JobLevel>()
                .Select(GetDisplayName)
                .ToList();

            return new Response.TeamMemberFilterOptionsResponse
            {
                Positions = positions,
                JobLevels = jobLevels
            };
        }
        catch (Exception exception) when (exception is not TeamMemberException &&
                                          exception is not OperationCanceledException)
        {
            throw new TeamMemberException(
                "MEMBER_FILTER_OPTIONS_READ_FAILED",
                "Không thể đọc options bộ lọc thành viên.",
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
            .ThenByDescending(member => member.CreateAt)
            .ThenByDescending(member => member.Id)
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

        if (request.PageSize is not (10 or 20 or 50))
        {
            fields.Add("pageSize");
        }

        if (request.Search?.Trim().Length > 300)
        {
            fields.Add("search");
        }

        if (request.Position is not null && request.Position.Any(position =>
                !string.IsNullOrWhiteSpace(position) && position.Trim().Length > 200))
        {
            fields.Add("position");
        }

        if (request.Status is not null && request.Status.Any(status =>
                !string.IsNullOrWhiteSpace(status) &&
                !string.Equals(status.Trim(), nameof(EmploymentStatus.Working), StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(status.Trim(), nameof(EmploymentStatus.Resigned), StringComparison.OrdinalIgnoreCase)))
        {
            fields.Add("status");
        }

        if (request.JobLevel is not null && request.JobLevel.Any(jobLevel =>
                !string.IsNullOrWhiteSpace(jobLevel) &&
                !TryParseEnumValue<JobLevel>(jobLevel.Trim(), out _)))
        {
            fields.Add("jobLevel");
        }

        if (fields.Count > 0)
        {
            throw new TeamMemberException(
                "MEMBER_QUERY_INVALID",
                "Thông tin truy vấn danh sách thành viên không hợp lệ.",
                fields.ToArray());
        }
    }

    private static List<EmploymentStatus> ParseEmploymentStatuses(List<string>? statuses)
    {
        if (statuses is null)
        {
            return [];
        }

        return statuses
            .Where(status => !string.IsNullOrWhiteSpace(status))
            .Select(status => string.Equals(
                status.Trim(),
                nameof(EmploymentStatus.Working),
                StringComparison.OrdinalIgnoreCase)
                ? EmploymentStatus.Working
                : EmploymentStatus.Resigned)
            .Distinct()
            .ToList();
    }

    private static List<string> NormalizePositions(List<string>? positions)
    {
        if (positions is null)
        {
            return [];
        }

        return positions
            .Where(position => !string.IsNullOrWhiteSpace(position))
            .Select(position => position.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();
    }

    private static List<JobLevel> ParseJobLevels(List<string>? jobLevels)
    {
        if (jobLevels is null)
        {
            return [];
        }

        var parsedJobLevels = new List<JobLevel>();
        foreach (var jobLevel in jobLevels)
        {
            if (string.IsNullOrWhiteSpace(jobLevel) ||
                !TryParseEnumValue<JobLevel>(jobLevel.Trim(), out var parsedJobLevel))
            {
                continue;
            }

            if (!parsedJobLevels.Contains(parsedJobLevel))
            {
                parsedJobLevels.Add(parsedJobLevel);
            }
        }

        return parsedJobLevels;
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
                .ThenByDescending(member => member.CreateAt)
                .ThenByDescending(member => member.Id)
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
        var displayName = NormalizeOptional(request.DisplayName);
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

        if (displayName is { Length: > 100 })
            throw new TeamMemberException(
                "MEMBER_VALIDATION_ERROR",
                "Tên hiển thị không được vượt quá 100 ký tự.",
                "displayName");

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

        var avatarUrl = await UploadTeamMemberImageAsync(request.Avatar, "TeamMemberAvatar");
        var backgroundUrl = await UploadTeamMemberImageAsync(request.Background, "TeamMemberBackground");
        var audioUrl = await UploadTeamMemberAudioAsync(request.Audio, "TeamMemberAudio");

        var now = DateTimeOffset.UtcNow;
        var member = new User
        {
            Id = Guid.NewGuid(),
            CreatedBy = createdBy,
            FullName = fullName,
            DisplayName = displayName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultMemberPassword),
            Position = position,
            JobLevel = parsedJobLevel,
            AvatarUrl = avatarUrl,
            AnimationUrl = NormalizeOptional(request.AnimationUrl),
            AudioUrl = audioUrl,
            Hometown = NormalizeOptional(request.Hometown),
            BackgroundUrl = backgroundUrl,
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

        if (request.IsPublished.HasValue)
        {
            ValidateStatusRequest(request);
            return await UpdatePublishStateAsync(id, request.IsPublished.Value);
        }

        return await UpdateProfileAsync(id, request);
    }

    private async Task<Response.TeamMemberResponse> UpdatePublishStateAsync(
        Guid id,
        bool isPublished)
    {
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var member = await FindMemberAsync(id);

        if (isPublished)
        {
            ValidatePublishableMember(member);
        }

        if (member.IsPublished == isPublished)
        {
            await transaction.CommitAsync();
            return ToResponse(member);
        }

        var now = DateTimeOffset.UtcNow;
        var publishedMembers = await GetPublishedMembersAsync(member.Id);
        NormalizePublishedDisplayOrders(publishedMembers, now);

        if (isPublished)
        {
            member.IsPublished = true;
            member.DisplayOrder = publishedMembers.Count + 1;
        }
        else
        {
            member.IsPublished = false;
            member.DisplayOrder = null;
        }

        member.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return ToResponse(member);
    }

    private async Task<Response.TeamMemberResponse> UpdateProfileAsync(
        Guid id,
        Request.UpdateTeamMemberRequest request)
    {
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var member = await FindMemberAsync(id);

        if (member.IsPublished)
        {
            throw new TeamMemberException(
                "RESOURCE_CONFLICT",
                "Phải gỡ đăng thành viên trước khi chỉnh sửa hoặc chuyển sang Đã nghỉ.",
                "isPublished");
        }

        var fullName = NormalizeRequired(request.FullName, "fullName", "Vui lòng nhập họ và tên.");
        var email = NormalizeRequired(request.Email, "email", "Vui lòng nhập email.")
            .ToLowerInvariant();
        var position = NormalizeRequired(request.Position, "position", "Vui lòng nhập vị trí.");
        var jobLevel = NormalizeRequired(request.JobLevel, "jobLevel", "Vui lòng nhập cấp bậc.");
        var employmentStatus = NormalizeRequired(
            request.EmploymentStatus,
            "employmentStatus",
            "Vui lòng nhập trạng thái làm việc.");
        var displayName = NormalizeOptional(request.DisplayName);

        if (fullName.Length > 200)
        {
            throw new TeamMemberException(
                "RESOURCE_VALIDATION_FAILED",
                "Họ và tên không được vượt quá 200 ký tự.",
                "fullName");
        }

        if (position.Length > 200)
        {
            throw new TeamMemberException(
                "RESOURCE_VALIDATION_FAILED",
                "Vị trí không được vượt quá 200 ký tự.",
                "position");
        }

        if (displayName is { Length: > 100 })
        {
            throw new TeamMemberException(
                "RESOURCE_VALIDATION_FAILED",
                "Tên hiển thị không được vượt quá 100 ký tự.",
                "displayName");
        }

        if (email.Length > 320 || !new EmailAddressAttribute().IsValid(email))
        {
            throw new TeamMemberException(
                "RESOURCE_VALIDATION_FAILED",
                "Email không đúng định dạng.",
                "email");
        }

        if (request.JoinedDate is null)
        {
            throw new TeamMemberException(
                "RESOURCE_VALIDATION_FAILED",
                "Vui lòng nhập ngày tham gia.",
                "joinedDate");
        }

        if (!TryParseEnumValue(jobLevel, out JobLevel parsedJobLevel))
        {
            throw new TeamMemberException(
                "RESOURCE_VALIDATION_FAILED",
                "Cấp bậc không hợp lệ.",
                "jobLevel");
        }

        if (!TryParseEnumValue(employmentStatus, out EmploymentStatus parsedEmploymentStatus))
        {
            throw new TeamMemberException(
                "RESOURCE_VALIDATION_FAILED",
                "Trạng thái làm việc không hợp lệ.",
                "employmentStatus");
        }

        var emailUsedByAnotherMember = await _dbContext.Users
            .AnyAsync(x => x.Id != id && x.Email.ToLower() == email);

        if (emailUsedByAnotherMember)
        {
            throw new TeamMemberException(
                "RESOURCE_CONFLICT",
                "Email đã tồn tại trong hệ thống.",
                "email");
        }

        var avatarAction = NormalizeTeamMemberMediaAction(request.AvatarAction);
        var backgroundAction = NormalizeTeamMemberMediaAction(request.BackgroundAction);
        var audioAction = NormalizeTeamMemberMediaAction(request.AudioAction);
        ValidateTeamMemberMediaActions(avatarAction, backgroundAction, audioAction, request);

        var removeAvatar = string.Equals(avatarAction, "remove", StringComparison.Ordinal);
        var removeBackground = string.Equals(backgroundAction, "remove", StringComparison.Ordinal);
        var removeAudio = string.Equals(audioAction, "remove", StringComparison.Ordinal);

        var avatarUrl = removeAvatar
            ? null
            : await UploadTeamMemberImageAsync(
                request.Avatar,
                "TeamMemberAvatar",
                member.AvatarUrl);
        var backgroundUrl = removeBackground
            ? null
            : await UploadTeamMemberImageAsync(
                request.Background,
                "TeamMemberBackground",
                member.BackgroundUrl);
        var audioUrl = removeAudio
            ? null
            : await UploadTeamMemberAudioAsync(
                request.Audio,
                "TeamMemberAudio",
                member.AudioUrl);

        member.FullName = fullName;
        member.DisplayName = displayName;
        member.Email = email;
        member.Position = position;
        member.JobLevel = parsedJobLevel;
        member.JoinedDate = request.JoinedDate;
        member.AvatarUrl = avatarUrl;
        member.AnimationUrl = NormalizeOptional(request.AnimationUrl);
        member.AudioUrl = audioUrl;
        member.Hometown = NormalizeOptional(request.Hometown);
        member.BackgroundUrl = backgroundUrl;
        member.Hobbies = NormalizeOptional(request.Hobbies);
        member.PersonalQuote = NormalizeOptional(request.PersonalQuote);
        member.EmploymentStatus = parsedEmploymentStatus;
        member.IsPublished = false;
        member.DisplayOrder = null;
        member.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return ToResponse(member);
    }

    private static void ValidateStatusRequest(Request.UpdateTeamMemberRequest request)
    {
        if (HasProfilePayload(request))
        {
            throw new TeamMemberException(
                "RESOURCE_VALIDATION_FAILED",
                "Request Đăng/Gỡ đăng không được kèm dữ liệu hồ sơ.",
                "isPublished");
        }
    }

    private static bool HasProfilePayload(Request.UpdateTeamMemberRequest request)
    {
        return request.FullName is not null ||
            request.DisplayName is not null ||
            request.Email is not null ||
            request.Position is not null ||
            request.JobLevel is not null ||
            request.JoinedDate.HasValue ||
            request.Avatar is not null ||
            request.AvatarAction is not null ||
            request.AnimationUrl is not null ||
            request.Audio is not null ||
            request.AudioAction is not null ||
            request.Hometown is not null ||
            request.Background is not null ||
            request.BackgroundAction is not null ||
            request.Hobbies is not null ||
            request.PersonalQuote is not null ||
            request.EmploymentStatus is not null;
    }

    private static string? NormalizeTeamMemberMediaAction(string? action)
    {
        return string.IsNullOrWhiteSpace(action)
            ? null
            : action.Trim();
    }

    private static void ValidateTeamMemberMediaActions(
        string? avatarAction,
        string? backgroundAction,
        string? audioAction,
        Request.UpdateTeamMemberRequest request)
    {
        ValidateTeamMemberMediaAction(avatarAction, request.Avatar, "avatarAction", "avatar", "Avatar");
        ValidateTeamMemberMediaAction(
            backgroundAction,
            request.Background,
            "backgroundAction",
            "background",
            "Background");
        ValidateTeamMemberMediaAction(audioAction, request.Audio, "audioAction", "audio", "Audio");
    }

    private static void ValidateTeamMemberMediaAction(
        string? action,
        IFormFile? file,
        string actionField,
        string fileField,
        string mediaName)
    {
        if (action is not null && !string.Equals(action, "remove", StringComparison.Ordinal))
        {
            throw new TeamMemberException(
                "MEMBER_MEDIA_ACTION_INVALID",
                $"Thao tác {mediaName} của thành viên không hợp lệ.",
                actionField);
        }

        if (string.Equals(action, "remove", StringComparison.Ordinal) && file is not null)
        {
            throw new TeamMemberException(
                "MEMBER_MEDIA_ACTION_INVALID",
                $"Không thể vừa gỡ {mediaName} vừa gửi file mới.",
                actionField,
                fileField);
        }
    }

    private async Task<string?> UploadTeamMemberImageAsync(
        IFormFile? file,
        string purpose,
        string? currentUrl = null)
    {
        if (file is null)
        {
            return currentUrl;
        }

        var uploadResult = await _mediaService.UploadImageAsync(
            new MediaService.Request.UploadImageRequest
            {
                File = file,
                Purpose = purpose
            });

        return uploadResult.Url;
    }

    private async Task<string?> UploadTeamMemberAudioAsync(
        IFormFile? file,
        string purpose,
        string? currentUrl = null)
    {
        if (file is null)
        {
            return currentUrl;
        }

        var uploadResult = await _mediaService.UploadAudioAsync(
            new MediaService.Request.UploadAudioRequest
            {
                File = file,
                Purpose = purpose
            });

        return uploadResult.Url;
    }

    private static string NormalizeRequired(string? value, string field, string message)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new TeamMemberException(
                "RESOURCE_VALIDATION_FAILED",
                message,
                field);
        }

        return normalized;
    }

    private async Task<List<User>> GetPublishedMembersAsync(Guid excludedMemberId)
    {
        return await _dbContext.Users
            .Where(member => member.RoleId == null &&
                             !member.IsDelete &&
                             member.IsPublished &&
                             member.EmploymentStatus == EmploymentStatus.Working &&
                             member.Id != excludedMemberId)
            .OrderBy(member => member.DisplayOrder == null)
            .ThenBy(member => member.DisplayOrder)
            .ThenByDescending(member => member.CreateAt)
            .ThenByDescending(member => member.Id)
            .ToListAsync();
    }

    private static void NormalizePublishedDisplayOrders(
        List<User> publishedMembers,
        DateTimeOffset now)
    {
        for (var index = 0; index < publishedMembers.Count; index++)
        {
            var member = publishedMembers[index];
            var displayOrder = index + 1;

            if (member.DisplayOrder == displayOrder)
            {
                continue;
            }

            member.DisplayOrder = displayOrder;
            member.UpdatedAt = now;
        }
    }

    private static void ValidatePublishableMember(User member)
    {
        var displayName = member.DisplayName?.Trim();

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new TeamMemberException(
                "RESOURCE_VALIDATION_FAILED",
                "Tên hiển thị là bắt buộc khi đăng thành viên.",
                "displayName");
        }

        if (displayName.Length > 100)
        {
            throw new TeamMemberException(
                "RESOURCE_VALIDATION_FAILED",
                "Tên hiển thị không được vượt quá 100 ký tự.",
                "displayName");
        }

        if (member.EmploymentStatus != EmploymentStatus.Working)
        {
            throw new TeamMemberException(
                "RESOURCE_CONFLICT",
                "Thành viên đã nghỉ việc không thể đăng.",
                "employmentStatus");
        }
    }

    private async Task<User> FindMemberAsync(Guid id)
    {
        var member = await _dbContext.Users.SingleOrDefaultAsync(x =>
            x.Id == id &&
            x.RoleId == null &&
            !x.IsDelete);
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
            DisplayName = member.DisplayName,
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
            BackgroundUrl = member.BackgroundUrl,
            Hobbies = member.Hobbies,
            PersonalQuote = member.PersonalQuote,
            JoinedDate = member.JoinedDate,
            EmploymentStatus = GetDisplayName(member.EmploymentStatus),
            IsActive = member.IsActive,
            IsPublished = member.IsPublished,
            DisplayOrder = member.DisplayOrder,
            CreatedAt = member.CreateAt,
            UpdatedAt = member.UpdatedAt,
            CanDelete = !(member.EmploymentStatus == EmploymentStatus.Working && member.IsPublished),
            DeleteBlockedReason = member.EmploymentStatus == EmploymentStatus.Working && member.IsPublished
                ? "PUBLIC_VISIBLE"
                : null
        };
    }

    private static string? RemoveDisplayNameDiacritics(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var result = new StringBuilder(value.Length);
        foreach (var character in value.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            // Vietnamese đ/Đ do not decompose into d/D plus a combining mark.
            if (character == 'đ')
                result.Append('d');
            else if (character == 'Đ')
                result.Append('D');
            else
                result.Append(character);
        }

        return result.ToString().Normalize(NormalizationForm.FormC);
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
