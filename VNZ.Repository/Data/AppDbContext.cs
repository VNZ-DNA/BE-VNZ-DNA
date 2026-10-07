using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Repository.Entity.Json;

namespace VNZ.Repository;

public class AppDbContext : DbContext
{
    private static readonly JsonSerializerOptions TypedJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter() }
    };

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();
    public DbSet<NewsCategory> NewsCategories => Set<NewsCategory>();
    public DbSet<NewsArticleCategory> NewsArticleCategories => Set<NewsArticleCategory>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<JobPost> JobPosts => Set<JobPost>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<ContactInquiry> ContactInquiries => Set<ContactInquiry>();
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Role");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.IsDelete).HasDefaultValue(false);
            entity.Property(x => x.Type).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.Type).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.IsDelete).HasDefaultValue(false);
            entity.HasQueryFilter(x => !x.IsDelete);
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(100);
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.Property(x => x.PasswordHash)
                .HasMaxLength(500)
                .IsRequired();
            entity.Property(x => x.EmploymentStatus).HasConversion<string>().IsRequired();
            entity.Property(x => x.JobLevel).HasConversion<string>();
            entity.HasIndex(x => x.Email)
                .HasDatabaseName("IX_User_Email_Active")
                .IsUnique()
                .HasFilter("\"IsDelete\" = FALSE");
            entity.HasOne(x => x.Role).WithMany(x => x.Users).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Creator).WithMany(x => x.CreatedUsers).HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NewsArticle>(entity =>
        {
            entity.ToTable("News_Article");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.UpdatedAt).IsConcurrencyToken();
            entity.Property(x => x.Translations)
                .HasConversion(new ValueConverter<NewsTranslations?, string?>(
                    value => value == null ? null : JsonSerializer.Serialize(value, TypedJsonOptions),
                    value => value == null || value == ""
                        ? null
                        : JsonSerializer.Deserialize<NewsTranslations>(value, TypedJsonOptions)))
                .HasColumnType("jsonb");

            entity.Property(x => x.IsDelete).HasDefaultValue(false);
            entity.HasQueryFilter(x => !x.IsDelete);
            entity.Property(x => x.Title).HasMaxLength(300);

            entity.Property(x => x.Slug).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ReadingTimeMinutes).HasDefaultValue(1);
            entity.Property(x => x.Status).HasConversion<string>().IsRequired();
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => x.Slug)
                .HasDatabaseName("IX_News_Article_Slug")
                .IsUnique();
            entity.HasIndex(x => new { x.PublishAt, x.Id })
                .HasDatabaseName("IX_News_Article_Public_PublishAt_Id")
                .HasFilter("\"Status\" = 'Published' AND \"Published\" = TRUE AND \"PublishAt\" IS NOT NULL");
            entity.HasOne(x => x.Creator)
                .WithMany(x => x.NewsArticles)
                .HasForeignKey(x => x.CreatedBy)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NewsCategory>(entity =>
        {
            entity.ToTable("News_Category");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code).HasMaxLength(100).IsRequired();
            entity.Property(x => x.IsDelete).HasDefaultValue(false);
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<NewsArticleCategory>(entity =>
        {
            entity.ToTable("News_Article_Category");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.IsDelete).HasDefaultValue(false);
            entity.HasIndex(x => new { x.NewsArticleId, x.NewsCategoryId }).IsUnique();
            entity.HasOne(x => x.NewsArticle).WithMany(x => x.NewsArticleCategories).HasForeignKey(x => x.NewsArticleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.NewsCategory).WithMany(x => x.NewsArticleCategories).HasForeignKey(x => x.NewsCategoryId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("Department");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code).HasMaxLength(100).IsRequired();
            entity.Property(x => x.IsDelete).HasDefaultValue(false);

            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<JobPost>(entity =>
        {
            entity.ToTable("Job_Post");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UpdatedAt).IsConcurrencyToken();
            entity.Property(x => x.Translations)
                .HasConversion(new ValueConverter<JobPostTranslations?, string?>(
                    value => value == null ? null : JsonSerializer.Serialize(value, TypedJsonOptions),
                    value => value == null || value == ""
                        ? null
                        : JsonSerializer.Deserialize<JobPostTranslations>(value, TypedJsonOptions)))
                .HasColumnType("jsonb");
            entity.Property(x => x.IsDelete).HasDefaultValue(false);
            entity.HasQueryFilter(x => !x.IsDelete);
            entity.Property(x => x.Title).HasMaxLength(300);
            entity.Property(x => x.Slug).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().IsRequired();
            entity.Property(x => x.EmploymentType).HasConversion<string>();
            entity.Property(x => x.JobLevel).HasConversion<string>();
            entity.Property(x => x.Skills).HasColumnType("jsonb").IsRequired();
            entity.HasIndex(x => new { x.Status, x.ExpiredAt });
            entity.HasIndex(x => x.Slug)
                .HasDatabaseName("IX_Job_Post_Slug")
                .IsUnique();
            entity.HasOne(x => x.Department).WithMany(x => x.JobPosts).HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Creator).WithMany(x => x.JobPosts).HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<JobApplication>(entity =>
        {
            entity.ToTable("Job_Application");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.IsDelete).HasDefaultValue(false);
            entity.HasQueryFilter(x => !x.IsDelete);
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.Property(x => x.Availability).HasMaxLength(100);
            entity.Property(x => x.AvailableStartDate).HasMaxLength(100);
            entity.Property(x => x.ReferralSource).HasMaxLength(200);
            entity.Property(x => x.Status).HasConversion<string>().IsRequired();
            entity.Property(x => x.JobPostSnapshot).HasColumnType("jsonb");
            entity.HasIndex(x => x.Status);
            entity.HasOne(x => x.JobPost).WithMany(x => x.Applications).HasForeignKey(x => x.JobPostId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Reviewer).WithMany(x => x.ReviewedApplications).HasForeignKey(x => x.ReviewedBy).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ContactInquiry>(entity =>
        {
            entity.ToTable("Contact_Inquiry");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.IsDelete).HasDefaultValue(false);
            entity.HasQueryFilter(x => !x.IsDelete);
            entity.Property(x => x.InquiryTopic).HasConversion<string>().IsRequired();
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.Property(x => x.CompanyName).HasMaxLength(200);
            entity.Property(x => x.BudgetRange).HasConversion<string>();
            entity.Property(x => x.ExpectedStart).HasConversion<string>();
            entity.Property(x => x.Source).HasConversion<string>();
            entity.Property(x => x.ConsentToDataProcessing).IsRequired();
            entity.Property(x => x.ContactStatus)
                .HasConversion<string>()
                .IsRequired()
                .HasSentinel(ContactStatus.NotContacted)
                .HasDefaultValue(ContactStatus.NotContacted);
            entity.HasIndex(x => x.IsRead);
            entity.HasOne(x => x.ContactedByUser).WithMany(x => x.ContactInquiries).HasForeignKey(x => x.ContactedBy).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Partner>(entity =>
        {
            entity.ToTable("Partner");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.IsDelete).HasDefaultValue(false);
            entity.HasQueryFilter(x => !x.IsDelete);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.HasOne(x => x.Creator).WithMany(x => x.Partners).HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Product");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.IsDelete).HasDefaultValue(false);
            entity.HasQueryFilter(x => !x.IsDelete);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().IsRequired();
            entity.Property(x => x.WordmarkUrl).HasColumnType("text");
            entity.Property(x => x.UpdatedAt).IsConcurrencyToken();
            entity.Property(x => x.Content).HasColumnType("jsonb");
            entity.Property(x => x.Translations)
                .HasConversion(new ValueConverter<ProductTranslations?, string?>(
                    value => value == null ? null : JsonSerializer.Serialize(value, TypedJsonOptions),
                    value => value == null || value == ""
                        ? null
                        : JsonSerializer.Deserialize<ProductTranslations>(value, TypedJsonOptions)))
                .HasColumnType("jsonb");
            entity.Property(x => x.Images).HasColumnType("jsonb");
            entity.HasOne(x => x.Creator).WithMany(x => x.Products).HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable("UserSession");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.IsDelete).HasDefaultValue(false);
            entity.Property(x => x.RefreshToken).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.ExpiresAt).IsRequired();
            entity.HasIndex(x => x.RefreshToken).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.ExpiresAt });
            entity.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
