namespace VNZ.Service.TeamMembers;

public static class Request
{
    public class CreateTeamMemberRequest
    {
        public string? FullName { get; init; }
        public string? Email { get; init; }
        public string? Position { get; init; }
        public string? JobLevel { get; init; }
        public DateTimeOffset? JoinedDate { get; init; }
        public string? AvatarUrl { get; init; }
        public string? AnimationUrl { get; init; }
        public string? AudioUrl { get; init; }
        public string? Hometown { get; init; }
        public string? Hobbies { get; init; }
        public string? PersonalQuote { get; init; }
    }

    public class UpdateTeamMemberRequest
    {
        public string? FullName { get; init; }
        public string? Email { get; init; }
        public string? Position { get; init; }
        public string? JobLevel { get; init; }
        public DateTimeOffset? JoinedDate { get; init; }
        public string? AvatarUrl { get; init; }
        public string? AnimationUrl { get; init; }
        public string? AudioUrl { get; init; }
        public string? Hometown { get; init; }
        public string? Hobbies { get; init; }
        public string? PersonalQuote { get; init; }
        public string? EmploymentStatus { get; init; }
    }
}
