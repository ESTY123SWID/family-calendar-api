namespace my_project.Core.Common;

// Hebrew messages for the Data Annotations on request DTOs.
// {0} = the field name from [Display(Name = ...)], {1} / {2} = the attribute limits
public static class ValidationMessages
{
    public const string Required = "השדה {0} הוא שדה חובה.";

    // StringLength without a minimum: {1} = maximum
    public const string MaxLength = "השדה {0} יכול להכיל עד {1} תווים.";

    // StringLength with a minimum: {1} = maximum, {2} = minimum
    public const string LengthRange = "השדה {0} חייב להכיל בין {2} ל-{1} תווים.";

    public const string Email = "כתובת האימייל אינה תקינה.";

    // Range: {1} = minimum, {2} = maximum
    public const string Range = "השדה {0} חייב להיות בין {1} ל-{2}.";

    public const string InvalidValue = "הערך בשדה {0} אינו חוקי.";

    public const string MinItems = "יש לבחור לפחות פריט אחד בשדה {0}.";
}
