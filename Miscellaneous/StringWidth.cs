using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Temac.Miscellaneous;

/// <summary>
/// String width manipulations
/// </summary>
internal static class StringWidth
{
    public static string Fixed(string text, int width)
    {
        if (text.Length <= width)
            return text.PadRight(width, ' ');

        return Max(text, width);
    }

    public static string Max(string text, int width)
    {
        if (text.Length <= width)
            return text;

        string leftText = text.Substring(0, width / 2);
        string rightText = text.Substring(text.Length - (width - leftText.Length - 1));
        return leftText + "\u2026" + rightText;
    }

    public static string Replaces(string text, int oldWidth)
    {
        if (text.Length >= oldWidth)
            return text;

        int diff = oldWidth - text.Length;

        return text + new String(' ', diff) + new String('\r', diff);
    }
}
