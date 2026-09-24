using System.ComponentModel.DataAnnotations;

namespace VNZ.Repository.Entity.Enum;

public enum ContactBudgetRange
{
    [Display(Name = "Chưa xác định")]
    Undetermined = 1,

    [Display(Name = "Dưới 50 triệu")]
    Under50Million = 2,

    [Display(Name = "50 – 200 triệu")]
    From50To200Million = 3,

    [Display(Name = "200 – 500 triệu")]
    From200To500Million = 4,

    [Display(Name = "Trên 500 triệu")]
    Over500Million = 5
}
