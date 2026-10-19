using System.ComponentModel.DataAnnotations;
using my_project.Core.Common;
using my_project.Core.Enums;

namespace my_project.Core.DTOs.Tasks;

public class CreateTaskRequest
{
    [Display(Name = "כותרת")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(150, ErrorMessage = ValidationMessages.MaxLength)]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "תיאור")]
    [StringLength(1000, ErrorMessage = ValidationMessages.MaxLength)]
    public string? Description { get; set; }

    [Display(Name = "קטגוריה")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [EnumDataType(typeof(TaskCategory), ErrorMessage = ValidationMessages.InvalidValue)]
    public TaskCategory? Category { get; set; }

    [Display(Name = "עדיפות")]
    [EnumDataType(typeof(TaskPriority), ErrorMessage = ValidationMessages.InvalidValue)]
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    // UTC
    public DateTime? DueDate { get; set; }
}
