namespace VNZ.Repository.Abstraction;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public bool IsDelete { get; set; }
}
