using VNZ.Service.Exceptions;

namespace VNZ.Service.Localization;

public static class LocaleResolver
{
    public const string Vietnamese = "vi";
    public const string English = "en";

    public static string Resolve(string? value)
    {
        if (value is null)
        {
            return Vietnamese;
        }

        if (value == Vietnamese || value == English)
        {
            return value;
        }

        throw InvalidLocale();
    }

    public static string Resolve(IEnumerable<string?> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var valuesList = values.ToList();

        if (valuesList.Count == 0)
        {
            return Vietnamese;
        }

        if (valuesList.Count != 1)
        {
            throw InvalidLocale();
        }

        return Resolve(valuesList[0]);
    }

    private static LocalizationException InvalidLocale()
    {
        return new LocalizationException(
            "LANGUAGE_NOT_SUPPORTED",
            "Chỉ hỗ trợ ngôn ngữ vi hoặc en.",
            "lang");
    }
}
