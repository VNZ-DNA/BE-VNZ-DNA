using System.ComponentModel.DataAnnotations;

namespace VNZ.Repository.Entity.Enum;

public enum ContactStatus
{
    [Display(Name = "Chưa liên hệ")]
    NotContacted = 1,
    [Display(Name = "Đã liên hệ")]
    Contacted = 2
}
