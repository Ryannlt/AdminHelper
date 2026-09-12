using System;
using System.Globalization;
using System.Text;

namespace AdminHelper
{
    internal static class TextSearch
    {
        private static readonly StringBuilder Builder = new StringBuilder(48);

        public static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            string source = Decompose(text);

            Builder.Length = 0;
            for (int i = 0; i < source.Length; i++)
            {
                char character = source[i];

                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;

                string folded = Fold(character);
                if (folded != null)
                {
                    Builder.Append(folded);
                    continue;
                }

                if (char.IsLetterOrDigit(character)) Builder.Append(char.ToLowerInvariant(character));
            }

            return Builder.ToString();
        }

        public static bool Contains(string haystack, string normalizedNeedle)
        {
            if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(normalizedNeedle)) return false;

            return Normalize(haystack).IndexOf(normalizedNeedle, StringComparison.Ordinal) >= 0;
        }

        private static string Fold(char character)
        {
            switch (character)
            {
                case 'ß':
                case 'ẞ': return "ss";
                case 'æ':
                case 'Æ': return "ae";
                case 'œ':
                case 'Œ': return "oe";
                case 'ø':
                case 'Ø': return "o";
                case 'ł':
                case 'Ł': return "l";
                case 'đ':
                case 'Đ':
                case 'ð':
                case 'Ð': return "d";
                case 'þ':
                case 'Þ': return "th";
                case 'ı': return "i";
                default: return null;
            }
        }

        private static string Decompose(string text)
        {
            try
            {
                return text.Normalize(NormalizationForm.FormD);
            }
            catch (Exception)
            {
                return text;
            }
        }
    }
}
