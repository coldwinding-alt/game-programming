// 简易 JSON 解析器
// 自己写的 JSON 解析工具，不依赖外部库。能把 JSON 字符串解析成字典和列表，也能把字典和列表转回 JSON 字符串。用于保存和读取游戏数据。

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace mlp
{
    public static class mlpJson
    {
        /// <summary>
        /// Parses a JSON string into a dictionary, list, string, number, bool, or null.
        /// Returns null if the input is empty or contains invalid JSON.
        /// </summary>
        /// <param name="json">The raw JSON text to parse.</param>
        /// <returns>The parsed object, or null on failure.</returns>
        public static object Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var reader = new mlpJsonReader(json);
            return reader.TryReadDocument(out var value) ? value : null;
        }

        /// <summary>
        /// Casts a parsed JSON object to a string-keyed dictionary, or returns null if it is not a dictionary.
        /// </summary>
        /// <param name="value">A parsed JSON object.</param>
        /// <returns>The dictionary, or null if the value is not a dictionary.</returns>
        public static Dictionary<string, object> AsDict(object value)
        {
            return value as Dictionary<string, object>;
        }

        /// <summary>
        /// Casts a parsed JSON object to a list, or returns null if it is not a list.
        /// </summary>
        /// <param name="value">A parsed JSON object.</param>
        /// <returns>The list, or null if the value is not a list.</returns>
        public static List<object> AsList(object value)
        {
            return value as List<object>;
        }

        /// <summary>
        /// Looks up a nested dictionary value by key from a parent dictionary. Returns null if the key is missing or the value is not a dictionary.
        /// </summary>
        /// <param name="dict">The parent dictionary to search in.</param>
        /// <param name="key">The key to look up.</param>
        /// <returns>The nested dictionary, or null.</returns>
        public static Dictionary<string, object> Dict(Dictionary<string, object> dict, string key)
        {
            return dict != null && dict.TryGetValue(key, out var value) ? AsDict(value) : null;
        }

        /// <summary>
        /// Looks up a list value by key from a dictionary. Returns null if the key is missing or the value is not a list.
        /// </summary>
        /// <param name="dict">The dictionary to search in.</param>
        /// <param name="key">The key to look up.</param>
        /// <returns>The list, or null.</returns>
        public static List<object> List(Dictionary<string, object> dict, string key)
        {
            return dict != null && dict.TryGetValue(key, out var value) ? AsList(value) : null;
        }

        /// <summary>
        /// Reads a string value from a dictionary by key. Returns the fallback if the key is missing or the value is null.
        /// </summary>
        /// <param name="dict">The dictionary to read from.</param>
        /// <param name="key">The key whose string value to return.</param>
        /// <param name="fallback">Value returned when the key is missing or null.</param>
        /// <returns>The string value, or the fallback.</returns>
        public static string String(Dictionary<string, object> dict, string key, string fallback = "")
        {
            if (dict == null || !dict.TryGetValue(key, out var value) || value == null)
            {
                return fallback;
            }

            return value.ToString();
        }

        /// <summary>
        /// Reads a float value from a dictionary by key. Converts number types automatically. Returns the fallback if missing or null.
        /// </summary>
        /// <param name="dict">The dictionary to read from.</param>
        /// <param name="key">The key whose float value to return.</param>
        /// <param name="fallback">Value returned when the key is missing or null.</param>
        /// <returns>The float value, or the fallback.</returns>
        public static float Float(Dictionary<string, object> dict, string key, float fallback = 0f)
        {
            if (dict == null || !dict.TryGetValue(key, out var value) || value == null)
            {
                return fallback;
            }

            return Convert.ToSingle(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Reads an integer value from a dictionary by key. Converts number types automatically. Returns the fallback if missing or null.
        /// </summary>
        /// <param name="dict">The dictionary to read from.</param>
        /// <param name="key">The key whose integer value to return.</param>
        /// <param name="fallback">Value returned when the key is missing or null.</param>
        /// <returns>The integer value, or the fallback.</returns>
        public static int Int(Dictionary<string, object> dict, string key, int fallback = 0)
        {
            if (dict == null || !dict.TryGetValue(key, out var value) || value == null)
            {
                return fallback;
            }

            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Reads a boolean value from a dictionary by key. Returns the fallback if the key is missing, null, or not a boolean.
        /// </summary>
        /// <param name="dict">The dictionary to read from.</param>
        /// <param name="key">The key whose boolean value to return.</param>
        /// <param name="fallback">Value returned when the key is missing or not a boolean.</param>
        /// <returns>The boolean value, or the fallback.</returns>
        public static bool Bool(Dictionary<string, object> dict, string key, bool fallback = false)
        {
            if (dict == null || !dict.TryGetValue(key, out var value) || value == null)
            {
                return fallback;
            }

            return value is bool boolValue ? boolValue : fallback;
        }

        private sealed class mlpJsonReader
        {
            private readonly string source;
            private int index;

            /// <summary>
            /// Creates a reader that will parse the given JSON string character by character.
            /// </summary>
            /// <param name="source">The raw JSON text to read.</param>
            public mlpJsonReader(string source)
            {
                this.source = source;
            }

            /// <summary>
            /// Reads a complete JSON document. Returns true if the entire input was consumed as valid JSON.
            /// </summary>
            /// <param name="value">The parsed result (dictionary, list, string, number, bool, or null).</param>
            /// <returns>True if parsing succeeded and the entire input was used.</returns>
            public bool TryReadDocument(out object value)
            {
                value = null;
                SkipWhitespace();
                if (!TryReadValue(out value))
                {
                    return false;
                }

                SkipWhitespace();
                return index == source.Length;
            }

            /// <summary>
            /// Reads the next JSON value (object, array, string, number, bool, or null) from the current position.
            /// </summary>
            /// <param name="value">The parsed value.</param>
            /// <returns>True if a valid JSON value was read.</returns>
            private bool TryReadValue(out object value)
            {
                value = null;
                SkipWhitespace();
                if (IsAtEnd)
                {
                    return false;
                }

                switch (source[index])
                {
                    case '{':
                        return TryReadObject(out value);
                    case '[':
                        return TryReadArray(out value);
                    case '"':
                        return TryReadString(out value);
                    case 't':
                        return TryReadKeyword("true", true, out value);
                    case 'f':
                        return TryReadKeyword("false", false, out value);
                    case 'n':
                        return TryReadKeyword("null", null, out value);
                    default:
                        return IsNumberStart(source[index]) && TryReadNumber(out value);
                }
            }

            /// <summary>
            /// Reads a JSON object (starting with '{') into a string-keyed dictionary.
            /// </summary>
            /// <param name="value">The resulting dictionary, or null on failure.</param>
            /// <returns>True if the object was parsed successfully.</returns>
            private bool TryReadObject(out object value)
            {
                var map = new Dictionary<string, object>();
                value = map;

                if (!TryConsume('{'))
                {
                    return false;
                }

                SkipWhitespace();
                if (TryConsume('}'))
                {
                    return true;
                }

                while (true)
                {
                    SkipWhitespace();
                    if (!TryReadStringValue(out var propertyName))
                    {
                        return false;
                    }

                    SkipWhitespace();
                    if (!TryConsume(':'))
                    {
                        return false;
                    }

                    if (!TryReadValue(out var propertyValue))
                    {
                        return false;
                    }

                    map[propertyName] = propertyValue;

                    SkipWhitespace();
                    if (TryConsume('}'))
                    {
                        return true;
                    }

                    if (!TryConsume(','))
                    {
                        return false;
                    }
                }
            }

            /// <summary>
            /// Reads a JSON array (starting with '[') into a list of objects.
            /// </summary>
            /// <param name="value">The resulting list, or null on failure.</param>
            /// <returns>True if the array was parsed successfully.</returns>
            private bool TryReadArray(out object value)
            {
                var items = new List<object>();
                value = items;

                if (!TryConsume('['))
                {
                    return false;
                }

                SkipWhitespace();
                if (TryConsume(']'))
                {
                    return true;
                }

                while (true)
                {
                    if (!TryReadValue(out var itemValue))
                    {
                        return false;
                    }

                    items.Add(itemValue);

                    SkipWhitespace();
                    if (TryConsume(']'))
                    {
                        return true;
                    }

                    if (!TryConsume(','))
                    {
                        return false;
                    }
                }
            }

            /// <summary>
            /// Reads a JSON string (wrapped in double quotes) as a boxed object.
            /// </summary>
            /// <param name="value">The string value wrapped in an object.</param>
            /// <returns>True if the string was parsed successfully.</returns>
            private bool TryReadString(out object value)
            {
                value = null;
                if (!TryReadStringValue(out var text))
                {
                    return false;
                }

                value = text;
                return true;
            }

            /// <summary>
            /// Reads a JSON string and handles escape sequences (like \n, \t, \uXXXX).
            /// </summary>
            /// <param name="value">The decoded string, or null on failure.</param>
            /// <returns>True if the string was parsed successfully.</returns>
            private bool TryReadStringValue(out string value)
            {
                value = null;
                if (!TryConsume('"'))
                {
                    return false;
                }

                var builder = new StringBuilder();
                while (!IsAtEnd)
                {
                    var current = ReadChar();
                    if (current == '"')
                    {
                        value = builder.ToString();
                        return true;
                    }

                    if (current == '\\')
                    {
                        if (!TryAppendEscapeSequence(builder))
                        {
                            return false;
                        }

                        continue;
                    }

                    if (current < ' ')
                    {
                        return false;
                    }

                    builder.Append(current);
                }

                return false;
            }

            /// <summary>
            /// Reads one escape character after a backslash and appends the decoded character to the builder.
            /// Supports \", \\, \/, \b, \f, \n, \r, \t, and \uXXXX.
            /// </summary>
            /// <param name="builder">The StringBuilder to append the decoded character to.</param>
            /// <returns>True if the escape sequence was valid.</returns>
            private bool TryAppendEscapeSequence(StringBuilder builder)
            {
                if (IsAtEnd)
                {
                    return false;
                }

                switch (ReadChar())
                {
                    case '"':
                        builder.Append('"');
                        return true;
                    case '\\':
                        builder.Append('\\');
                        return true;
                    case '/':
                        builder.Append('/');
                        return true;
                    case 'b':
                        builder.Append('\b');
                        return true;
                    case 'f':
                        builder.Append('\f');
                        return true;
                    case 'n':
                        builder.Append('\n');
                        return true;
                    case 'r':
                        builder.Append('\r');
                        return true;
                    case 't':
                        builder.Append('\t');
                        return true;
                    case 'u':
                        return TryAppendUnicode(builder);
                    default:
                        return false;
                }
            }

            /// <summary>
            /// Reads a \uXXXX escape sequence and appends the decoded Unicode character(s) to the builder.
            /// Handles surrogate pairs (two consecutive \u sequences for characters above U+FFFF).
            /// </summary>
            /// <param name="builder">The StringBuilder to append the decoded character(s) to.</param>
            /// <returns>True if the hex digits were valid.</returns>
            private bool TryAppendUnicode(StringBuilder builder)
            {
                if (!TryReadHexQuad(out var firstUnit))
                {
                    return false;
                }

                var firstChar = (char)firstUnit;
                if (!char.IsHighSurrogate(firstChar))
                {
                    builder.Append(firstChar);
                    return true;
                }

                var rewind = index;
                if (!TryConsume('\\') || !TryConsume('u') || !TryReadHexQuad(out var secondUnit))
                {
                    index = rewind;
                    builder.Append(firstChar);
                    return true;
                }

                var secondChar = (char)secondUnit;
                if (!char.IsLowSurrogate(secondChar))
                {
                    return false;
                }

                builder.Append(firstChar);
                builder.Append(secondChar);
                return true;
            }

            /// <summary>
            /// Reads exactly four hexadecimal characters and converts them to an integer value.
            /// </summary>
            /// <param name="value">The decoded 16-bit integer.</param>
            /// <returns>True if four valid hex digits were read.</returns>
            private bool TryReadHexQuad(out int value)
            {
                value = 0;
                if (index + 4 > source.Length)
                {
                    return false;
                }

                for (var i = 0; i < 4; i++)
                {
                    var digit = DecodeHexDigit(source[index++]);
                    if (digit < 0)
                    {
                        return false;
                    }

                    value = (value << 4) | digit;
                }

                return true;
            }

            /// <summary>
            /// Attempts to match a literal keyword (like "true", "false", or "null") and returns the corresponding value.
            /// </summary>
            /// <param name="keyword">The keyword text to match (e.g. "true").</param>
            /// <param name="replacement">The C# value to return on a match (e.g. true for "true", null for "null").</param>
            /// <param name="value">The replacement value if matched, or null.</param>
            /// <returns>True if the keyword was matched at the current position.</returns>
            private bool TryReadKeyword(string keyword, object replacement, out object value)
            {
                value = null;
                if (!Matches(keyword))
                {
                    return false;
                }

                index += keyword.Length;
                value = replacement;
                return true;
            }

            /// <summary>
            /// Reads a JSON number (integer or decimal, with optional sign and exponent).
            /// Returns a long for whole numbers or a double for decimals.
            /// </summary>
            /// <param name="value">The parsed number as a long or double.</param>
            /// <returns>True if a valid number was read.</returns>
            private bool TryReadNumber(out object value)
            {
                value = null;
                var start = index;
                var isWholeNumber = true;

                if (TryConsume('-') && IsAtEnd)
                {
                    return false;
                }

                if (!TryReadIntegerDigits())
                {
                    return false;
                }

                if (TryConsume('.'))
                {
                    isWholeNumber = false;
                    if (!TryReadDecimalDigits())
                    {
                        return false;
                    }
                }

                if (!IsAtEnd && (source[index] == 'e' || source[index] == 'E'))
                {
                    isWholeNumber = false;
                    index++;
                    if (!IsAtEnd && (source[index] == '+' || source[index] == '-'))
                    {
                        index++;
                    }

                    if (!TryReadDecimalDigits())
                    {
                        return false;
                    }
                }

                var slice = source.Substring(start, index - start);
                if (isWholeNumber && long.TryParse(slice, NumberStyles.Integer, CultureInfo.InvariantCulture, out var wholeNumber))
                {
                    value = wholeNumber;
                    return true;
                }

                if (double.TryParse(slice, NumberStyles.Float, CultureInfo.InvariantCulture, out var decimalNumber))
                {
                    value = decimalNumber;
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Reads the integer part of a number (one or more digits, with no leading zeros except for "0" itself).
            /// </summary>
            /// <returns>True if at least one digit was read.</returns>
            private bool TryReadIntegerDigits()
            {
                if (IsAtEnd)
                {
                    return false;
                }

                if (source[index] == '0')
                {
                    index++;
                    return true;
                }

                if (!IsNonZeroDigit(source[index]))
                {
                    return false;
                }

                index++;
                while (!IsAtEnd && char.IsDigit(source[index]))
                {
                    index++;
                }

                return true;
            }

            /// <summary>
            /// Reads one or more digits for the fractional or exponent part of a number.
            /// </summary>
            /// <returns>True if at least one digit was read.</returns>
            private bool TryReadDecimalDigits()
            {
                var start = index;
                while (!IsAtEnd && char.IsDigit(source[index]))
                {
                    index++;
                }

                return index > start;
            }

            /// <summary>
            /// Checks whether the given keyword appears at the current read position without advancing.
            /// </summary>
            /// <param name="keyword">The text to match against.</param>
            /// <returns>True if the keyword is found at the current position.</returns>
            private bool Matches(string keyword)
            {
                if (index + keyword.Length > source.Length)
                {
                    return false;
                }

                for (var i = 0; i < keyword.Length; i++)
                {
                    if (source[index + i] != keyword[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            /// <summary>
            /// Advances past any spaces, tabs, and newlines until a non-whitespace character is reached.
            /// </summary>
            private void SkipWhitespace()
            {
                while (!IsAtEnd && char.IsWhiteSpace(source[index]))
                {
                    index++;
                }
            }

            /// <summary>
            /// Returns the character at the current position and advances to the next position.
            /// </summary>
            /// <returns>The character that was read.</returns>
            private char ReadChar()
            {
                return source[index++];
            }

            /// <summary>
            /// If the current character matches the expected one, advances past it and returns true. Otherwise returns false.
            /// </summary>
            /// <param name="expected">The character to match.</param>
            /// <returns>True if the character was found and consumed.</returns>
            private bool TryConsume(char expected)
            {
                if (IsAtEnd || source[index] != expected)
                {
                    return false;
                }

                index++;
                return true;
            }

            /// <summary>
            /// Returns true if the character could be the start of a JSON number (a digit or a minus sign).
            /// </summary>
            /// <param name="value">The character to test.</param>
            /// <returns>True if the character is '-' or a digit.</returns>
            private static bool IsNumberStart(char value)
            {
                return value == '-' || char.IsDigit(value);
            }

            /// <summary>
            /// Returns true if the character is a digit from 1 to 9 (not zero).
            /// </summary>
            /// <param name="value">The character to test.</param>
            /// <returns>True if the character is between '1' and '9'.</returns>
            private static bool IsNonZeroDigit(char value)
            {
                return value >= '1' && value <= '9';
            }

            /// <summary>
            /// Converts a single hexadecimal character (0-9, a-f, A-F) to its integer value.
            /// </summary>
            /// <param name="value">The hex character to decode.</param>
            /// <returns>The integer value (0-15), or -1 if the character is not a valid hex digit.</returns>
            private static int DecodeHexDigit(char value)
            {
                if (value >= '0' && value <= '9')
                {
                    return value - '0';
                }

                if (value >= 'a' && value <= 'f')
                {
                    return value - 'a' + 10;
                }

                if (value >= 'A' && value <= 'F')
                {
                    return value - 'A' + 10;
                }

                return -1;
            }

            private bool IsAtEnd
            {
                get
                {
                    return index >= source.Length;
                }
            }
        }
    }
}
