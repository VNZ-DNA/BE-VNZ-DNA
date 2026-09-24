using System.ComponentModel.DataAnnotations;

namespace VNZ.Repository.Entity.Enum;

public enum ContactSource
{
    [Display(Name = "Tìm kiếm trên Google")]
    GoogleSearch = 1,

    [Display(Name = "Mạng xã hội")]
    SocialMedia = 2,

    [Display(Name = "Bạn bè / đối tác giới thiệu")]
    Referral = 3,

    [Display(Name = "Sự kiện · hội thảo")]
    EventSeminar = 4,

    [Display(Name = "Khác")]
    Other = 5
}
