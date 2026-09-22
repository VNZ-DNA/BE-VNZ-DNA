using System.ComponentModel.DataAnnotations;

namespace VNZ.Repository.Entity.Enum;

public enum JobPostStatus
{
    [Display(Name = "Bản nháp")]
    Draft = 1,
    [Display(Name = "Đang tuyển")]
    Open = 2,
    [Display(Name = "Đã đóng")]
    Closed = 3,
    [Display(Name = "Đã hết hạn")]
    Expired = 4
}
