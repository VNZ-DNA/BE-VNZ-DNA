using Microsoft.AspNetCore.Http;

namespace VNZ.Service.TeamMembers;

public static class Request
{
    public class CreateTeamMemberRequest
    {
        public string? FullName { get; set; }
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
        public string? Position { get; set; }
        public string? JobLevel { get; set; }
        public DateTimeOffset? JoinedDate { get; set; }
        public IFormFile? Avatar { get; set; }
        public string? AvatarUrl { get; set; }
        public string? AnimationUrl { get; set; }
        public string? AudioUrl { get; set; }
        public string? Hometown { get; set; }
        public string? BackgroundUrl { get; set; }
        public string? Hobbies { get; set; }
        public string? PersonalQuote { get; set; }

    }

    public class UpdateTeamMemberRequest
    {
        public string? FullName { get; set; }
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
        public string? Position { get; set; }
        public string? JobLevel { get; set; }
        public DateTimeOffset? JoinedDate { get; set; }
        public IFormFile? Avatar { get; set; }
        public string? AvatarUrl { get; set; }
        public string? AnimationUrl { get; set; }
        public string? AudioUrl { get; set; }
        public string? Hometown { get; set; }
        public string? BackgroundUrl { get; set; }
        public string? Hobbies { get; set; }
        public string? PersonalQuote { get; set; }
        public string? EmploymentStatus { get; set; }
        public bool? IsPublished { get; set; }

    }

    public class ReorderTeamMembersRequest
    {
        public List<Guid>? OrderedMemberIds { get; set; }
    }

    public class GetTeamMemberListRequest
    {
        public string? Search { get; set; }
        public List<string>? Status { get; set; }
        public List<string>? Position { get; set; }
        public List<string>? JobLevel { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
