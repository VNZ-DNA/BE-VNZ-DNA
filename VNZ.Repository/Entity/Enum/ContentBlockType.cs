using System.ComponentModel.DataAnnotations;

namespace VNZ.Repository.Entity.Enum;

public enum ContentBlockType
{
    [Display(Name = "Tiêu đề")]
    Title = 1,
    [Display(Name = "Mô tả")]
    Description = 2,
    [Display(Name = "Danh mục")]
    Category = 3,
    [Display(Name = "Tính năng")]
    Feature = 4
}
