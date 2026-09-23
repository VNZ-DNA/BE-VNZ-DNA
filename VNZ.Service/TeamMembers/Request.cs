namespace VNZ.Service.TeamMembers;

public static class Request
{
    public class CreateTeamMemberRequest
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Position { get; set; }
        public string? JobLevel { get; set; }
        public DateTimeOffset? JoinedDate { get; set; }
        public string? AvatarUrl { get; set; }
        public string? AnimationUrl { get; set; }
        public string? AudioUrl { get; set; }
        public string? Hometown { get; set; }
        public string? Hobbies { get; set; }
        public string? PersonalQuote { get; set; }

    }

    public class UpdateTeamMemberRequest
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Position { get; set; }
        public string? JobLevel { get; set; }
        public DateTimeOffset? JoinedDate { get; set; }
        public string? AvatarUrl { get; set; }
        public string? AnimationUrl { get; set; }
        public string? AudioUrl { get; set; }
        public string? Hometown { get; set; }
        public string? Hobbies { get; set; }
        public string? PersonalQuote { get; set; }
        public string? EmploymentStatus { get; set; }
        public bool? IsPublished { get; set; }

    }
}
