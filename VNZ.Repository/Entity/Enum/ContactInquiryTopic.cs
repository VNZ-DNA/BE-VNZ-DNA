using System.ComponentModel.DataAnnotations;

namespace VNZ.Repository.Entity.Enum;

public enum ContactInquiryTopic
{
    [Display(Name = "Hợp tác / dự án")]
    PartnershipProject = 1,

    [Display(Name = "Tư vấn giải pháp công nghệ")]
    TechnologyConsulting = 2,

    [Display(Name = "Sản phẩm của VNZ")]
    VnzProducts = 3,

    [Display(Name = "Tuyển dụng / ứng tuyển")]
    RecruitmentApplication = 4,

    [Display(Name = "Báo chí · truyền thông")]
    MediaPress = 5,

    [Display(Name = "Nội dung khác")]
    Other = 6
}
