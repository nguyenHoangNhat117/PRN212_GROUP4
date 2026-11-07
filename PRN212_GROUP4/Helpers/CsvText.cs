namespace PRN212_GROUP4.Helpers;

public static class CsvText
{
    public static string Cell(string? value)
    {
        string text = value ?? "";
        string trimmed = text.TrimStart();

        if (trimmed.StartsWith('=')
            || trimmed.StartsWith('+')
            || trimmed.StartsWith('-')
            || trimmed.StartsWith('@')
            || text.StartsWith('\t')
            || text.StartsWith('\r')
            || text.StartsWith('\n'))
        {
            text = "'" + text;
        }

        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }
}