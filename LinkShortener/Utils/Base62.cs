using System;

namespace LinkShortener.Utils
{
    internal static class Base62
    {
        private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public static string Encode(long value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (value == 0) return "0";

            var baseSize = Alphabet.Length;
            var chars = new System.Text.StringBuilder();
            while (value > 0)
            {
                var rem = (int)(value % baseSize);
                chars.Insert(0, Alphabet[rem]);
                value /= baseSize;
            }

            return chars.ToString();
        }
    }
}
