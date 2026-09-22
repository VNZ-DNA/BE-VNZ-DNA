using System.ComponentModel.DataAnnotations;

namespace VNZ.Repository.Entity.Enum;

public enum NewsStatus
{
    [Display(Name = "Bản nháp")]
    Draft = 1,
    [Display(Name = "Đã đăng")]
    Published = 2,
    [Display(Name = "Đã đóng")]
    Closed = 3
}
