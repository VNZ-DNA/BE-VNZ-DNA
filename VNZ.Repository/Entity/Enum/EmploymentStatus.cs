using System.ComponentModel.DataAnnotations;

namespace VNZ.Repository.Entity.Enum;

public enum EmploymentStatus
{
    [Display(Name = "Đang làm việc")]
    Working = 1,
    [Display(Name = "Đã nghỉ việc")]
    Resigned = 2
}
