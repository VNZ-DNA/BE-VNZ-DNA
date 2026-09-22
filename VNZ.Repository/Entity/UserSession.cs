namespace VNZ.Repository.Entity;

using VNZ.Repository.Abstraction;

public class UserSession : BaseEntity
{
    public User User { get; set; } = null!;
    public Guid UserId { get; set; }
    public required string RefreshToken { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
    public required bool IsRevoked { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    
}
