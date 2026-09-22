using System.ComponentModel.DataAnnotations;

namespace VNZ.Repository.Entity.Enum;

public enum EmploymentType
{
    [Display(Name = "Thực tập")]
    Internship = 1,
    [Display(Name = "Toàn thời gian")]
    FullTime = 2,
    [Display(Name = "Bán thời gian")]
    PartTime = 3,
    [Display(Name = "Hợp đồng")]
    Contract = 4
}
