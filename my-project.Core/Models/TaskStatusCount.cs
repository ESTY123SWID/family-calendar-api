using my_project.Core.Enums;

namespace my_project.Core.Models;

// Number of tasks of an event per (category, status). Used for the progress view
public record TaskStatusCount(TaskCategory Category, FamilyTaskStatus Status, int Count);
