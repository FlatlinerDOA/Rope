namespace Rope.IO;

// StartsWith - Equals | StartsWith
// Contains - StartsWith, Equals, EndsWith, Contains

[Flags]
public enum SupportedOperationFlags
{
 	None = 0,
    
    /// <summary>
    /// Indexes each character combined with it's position in the text (offset by 1).
    /// </summary>
    StartsWith = 1 << 0,
    
    /// <summary>
    /// Equals implies StartsWith but adds a string terminator for the character position after the text (offset by 1) for a lower false positive rate.
    /// </summary>
    Equals = StartsWith | 1 << 2,

    /// <summary>
    /// EndsWith stands alone as it stores negative indexes for each character working backwards from the end of the string to the start, it is only supported if explictly set or if Contains is specified.
    /// </summary>    
    EndsWith = 1 << 3,

    /// <summary>
    /// Contains implies StartsWith and EndsWith, and is the weakest guarantee from a statistical standpoint, characters are just stored with an int.MaxValue index so has the highest false positive rate.
    /// Equals is supported by this operation but not included with it.
    /// </summary>
    Contains = StartsWith | EndsWith | 1 << 4,
}
