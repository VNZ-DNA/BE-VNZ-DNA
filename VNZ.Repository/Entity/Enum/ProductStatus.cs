using System.ComponentModel.DataAnnotations;

namespace VNZ.Repository.Entity.Enum;

public enum ProductStatus
{
    [Display(Name = "Đang thực hiện")]
    InProgress = 1,
    [Display(Name = "Đã hoàn thành")]
    Completed = 2
}
