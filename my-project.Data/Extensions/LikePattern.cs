namespace my_project.Data.Extensions;

internal static class LikePattern
{
    private const string EscapeCharacter = "\\";

    // "%<value>%" for EF.Functions.ILike (case-insensitive "contains" in PostgreSQL).
    // %, _ and \ typed by the user are escaped, so they are searched as plain characters
    public static string Contains(string value)
    {
        var escaped = value
            .Replace(EscapeCharacter, EscapeCharacter + EscapeCharacter)
            .Replace("%", EscapeCharacter + "%")
            .Replace("_", EscapeCharacter + "_");

        return $"%{escaped}%";
    }
}
