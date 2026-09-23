using System.ComponentModel.DataAnnotations;

namespace VNZ.Repository.Entity.Enum;

public enum JobLevel
{
    [Display(Name = "Intern")]
    Intern = 1,
    [Display(Name = "Fresher")]
    Fresher = 2,
    [Display(Name = "Junior")]
    Junior = 3,
    [Display(Name = "Middle")]
    Middle = 4,
    [Display(Name = "Senior")]
    Senior = 5,
    [Display(Name = "Lead")]
    Lead = 6
}
