using System.ComponentModel.DataAnnotations;

namespace VNZ.Repository.Entity.Enum;

public enum ContactExpectedStart
{
    [Display(Name = "Càng sớm càng tốt")]
    AsSoonAsPossible = 1,

    [Display(Name = "Trong 1 - 3 tháng")]
    WithinOneToThreeMonths = 2,

    [Display(Name = "Trong 3 - 6 tháng")]
    WithinThreeToSixMonths = 3,

    [Display(Name = "Mới đang tìm hiểu")]
    Exploring = 4
}
