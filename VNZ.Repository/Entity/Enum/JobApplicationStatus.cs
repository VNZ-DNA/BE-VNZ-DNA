using System.ComponentModel.DataAnnotations;

namespace VNZ.Repository.Entity.Enum;

public enum JobApplicationStatus
{
    [Display(Name = "Chờ duyệt")]
    Pending = 1,
    [Display(Name = "Đã duyệt")]
    Accepted = 2,
    [Display(Name = "Không duyệt")]
    Rejected = 3,
    [Display(Name = "Đã gửi email phỏng vấn")]
    SendedEmail = 4
}
