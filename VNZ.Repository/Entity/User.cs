namespace VNZ.Repository.Entity;

using VNZ.Repository.Abstraction;
using VNZ.Repository.Entity.Enum;

public class User : BaseEntity
{
    public Guid? RoleId { get; set; }
    public Guid? CreatedBy { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? Position { get; set; }
    public JobLevel? JobLevel { get; set; }
    public string? AvatarUrl { get; set; }
    public string? AnimationUrl { get; set; }
    public string? AudioUrl { get; set; }
    public string? Hometown { get; set; }
    public string? Hobbies { get; set; }
    public string? PersonalQuote { get; set; }
    public DateTime? JoinedDate { get; set; }
    public bool IsActive { get; set; }
    public EmploymentStatus EmploymentStatus { get; set; }
    public bool IsPublished { get; set; }
    public int? DisplayOrder { get; set; }
    public DateTime CreateAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Role? Role { get; set; }
    public User? Creator { get; set; }
    public ICollection<User> CreatedUsers { get; set; } = new List<User>();
    public ICollection<NewsArticle> NewsArticles { get; set; } = new List<NewsArticle>();
    public ICollection<JobPost> JobPosts { get; set; } = new List<JobPost>();
    public ICollection<JobApplication> ReviewedApplications { get; set; } = new List<JobApplication>();
    public ICollection<ContactInquiry> ContactInquiries { get; set; } = new List<ContactInquiry>();
    public ICollection<Partner> Partners { get; set; } = new List<Partner>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
