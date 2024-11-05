namespace Rope.Compare;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

/// <summary>
/// Culture specific character comparer for performing find operations such as IndexOf etc.
/// </summary>
public sealed class CharComparer : IEqualityComparer<char>
{
    public static readonly CharComparer Ordinal = new CharComparer(CultureInfo.InvariantCulture, CompareOptions.Ordinal);

    public static readonly CharComparer OrdinalIgnoreCase = new CharComparer(CultureInfo.InvariantCulture, CompareOptions.OrdinalIgnoreCase);

    public static readonly CharComparer InvariantCulture = new CharComparer(CultureInfo.InvariantCulture, CompareOptions.None);

    public static readonly CharComparer InvariantCultureIgnoreCase = new CharComparer(CultureInfo.InvariantCulture, CompareOptions.IgnoreCase);

    public static readonly CharComparer CurrentCulture = new CharComparer(CultureInfo.CurrentCulture, CompareOptions.None);

    public static readonly CharComparer CurrentCultureIgnoreCase = new CharComparer(CultureInfo.CurrentCulture, CompareOptions.IgnoreCase);

    private readonly CultureInfo culture;

    private readonly CompareOptions options;

    public CharComparer(CultureInfo culture, CompareOptions options)
    {
        this.culture = culture;
        this.options = options;
    }

#if NET8_0_OR_GREATER
    public bool Equals(char x, char y) => culture.CompareInfo.Compare([x], [y], options) == 0;

    public int GetHashCode([DisallowNull] char obj) => culture.CompareInfo.GetHashCode([obj], options);
#else
    public bool Equals(char x, char y) => culture.CompareInfo.Compare(new string([x]), new string([y]), options) == 0;

    public int GetHashCode([DisallowNull] char obj) => culture.CompareInfo.GetHashCode(new string([obj]), options);
#endif
}