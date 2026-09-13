using System;
using System.Text;

/// <summary>
/// Utility to fix floating Thai vowels and tone marks (แก้ปัญหาสระลอย/วรรณยุกต์ลอย)
/// Converts standard Thai Unicode characters to properly positioned glyphs.
/// </summary>
public static class ThaiFontAdjuster
{
    // Consonants with tall ascenders (หางยาว): ป, ผ, ฝ, ฟ
    private static bool IsAscender(char c)
    {
        return c == '\u0E1B' || c == '\u0E1C' || c == '\u0E1D' || c == '\u0E1F';
    }

    // Consonants with descenders (มีหางล่าง): ญ, ฐ
    private static bool IsDescender(char c)
    {
        return c == '\u0E0D' || c == '\u0E10';
    }

    // Upper vowels (สระบน): ั, ิ, ี, ึ, ื, ็, ์
    private static bool IsUpperVowel(char c)
    {
        return c == '\u0E31' || (c >= '\u0E34' && c <= '\u0E37') || c == '\u0E47' || c == '\u0E4C';
    }

    // Lower vowels (สระล่าง): ุ, ู, ฺ
    private static bool IsLowerVowel(char c)
    {
        return c >= '\u0E38' && c <= '\u0E3A';
    }

    // Tone marks (วรรณยุกต์): ่, ้, ๊, ๋, ์, ํ, ๎
    private static bool IsToneMark(char c)
    {
        return (c >= '\u0E48' && c <= '\u0E4C') || c == '\u0E4D' || c == '\u0E4E';
    }

    /// <summary>
    /// Adjusts Thai text string so vowels and tone marks do not float or overlap.
    /// </summary>
    public static string Adjust(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        StringBuilder sb = new StringBuilder(text.Length);
        int len = text.Length;

        for (int i = 0; i < len; i++)
        {
            char current = text[i];
            char prev = (i > 0) ? text[i - 1] : '\0';
            char prev2 = (i > 1) ? text[i - 2] : '\0';

            // Check if current is tone mark
            if (IsToneMark(current))
            {
                // Case 1: Consonant with ascender (ป, ผ, ฝ, ฟ) followed directly by tone mark
                if (IsAscender(prev))
                {
                    // Shift tone mark left/down into PUA position
                    sb.Append(ShiftToneMarkLeft(current));
                    continue;
                }
                // Case 2: Consonant with ascender followed by upper vowel, followed by tone mark
                else if (IsUpperVowel(prev) && IsAscender(prev2))
                {
                    // Shift upper tone mark left
                    sb.Append(ShiftToneMarkUpperLeft(current));
                    continue;
                }
                // Case 3: Normal consonant followed by upper vowel, followed by tone mark
                else if (IsUpperVowel(prev))
                {
                    // Standard tone mark above vowel (PUA upper level)
                    sb.Append(ShiftToneMarkUpper(current));
                    continue;
                }
            }
            // Check if current is upper vowel following an ascender
            else if (IsUpperVowel(current))
            {
                if (IsAscender(prev))
                {
                    // Shift upper vowel left
                    sb.Append(ShiftUpperVowelLeft(current));
                    continue;
                }
            }
            // Check if current is lower vowel under descender (ญ, ฐ)
            else if (IsLowerVowel(current))
            {
                if (IsDescender(prev))
                {
                    // Cut descender or shift lower vowel
                    sb.Append(ShiftLowerVowel(current));
                    continue;
                }
            }

            sb.Append(current);
        }

        return sb.ToString();
    }

    private static char ShiftToneMarkLeft(char c)
    {
        switch (c)
        {
            case '\u0E48': return '\uF70A'; // ไม้เอก
            case '\u0E49': return '\uF70B'; // ไม้โท
            case '\u0E4A': return '\uF70C'; // ไม้ตรี
            case '\u0E4B': return '\uF70D'; // ไม้จัตวา
            case '\u0E4C': return '\uF70E'; // การันต์
            default: return c;
        }
    }

    private static char ShiftToneMarkUpperLeft(char c)
    {
        switch (c)
        {
            case '\u0E48': return '\uF713';
            case '\u0E49': return '\uF714';
            case '\u0E4A': return '\uF715';
            case '\u0E4B': return '\uF716';
            case '\u0E4C': return '\uF717';
            default: return c;
        }
    }

    private static char ShiftToneMarkUpper(char c)
    {
        switch (c)
        {
            case '\u0E48': return '\uF705';
            case '\u0E49': return '\uF706';
            case '\u0E4A': return '\uF707';
            case '\u0E4B': return '\uF708';
            case '\u0E4C': return '\uF709';
            default: return c;
        }
    }

    private static char ShiftUpperVowelLeft(char c)
    {
        switch (c)
        {
            case '\u0E31': return '\uF710'; // ไม้หันอากาศ
            case '\u0E34': return '\uF701'; // สระอิ
            case '\u0E35': return '\uF702'; // สระอี
            case '\u0E36': return '\uF703'; // สระอึ
            case '\u0E37': return '\uF704'; // สระอื
            case '\u0E47': return '\uF712'; // ไม้ไต่คู้
            default: return c;
        }
    }

    private static char ShiftLowerVowel(char c)
    {
        switch (c)
        {
            case '\u0E38': return '\uF718'; // สระอุ
            case '\u0E39': return '\uF719'; // สระอู
            default: return c;
        }
    }
}
