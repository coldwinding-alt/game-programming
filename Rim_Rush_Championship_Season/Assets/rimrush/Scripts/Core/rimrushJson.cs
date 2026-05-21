using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace rimrush
{
    public static class rimrushJson
    {
        public static object Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var reader = new rimrushJsonReader(json);
            return reader.TryReadDocument(out var value) ? value : null;
        }

        public static Dictionary<string, object> AsDict(object value)
        {
            return value as Dictionary<string, object>;
        }

        public static List<object> AsList(object value)
        {
            return value as List<object>;
        }

        public static Dictionary<string, object> Dict(Dictionary<string, object> dict, string key)
        {
            return dict != null && dict.TryGetValue(key, out var value) ? AsDict(value) : null;
        }

        public static List<object> List(Dictionary<string, object> dict, string key)
        {
            return dict != null && dict.TryGetValue(key, out var value) ? AsList(value) : null;
        }

        public static string String(Dictionary<string, object> dict, string key, string fallback = "")
        {
            if (dict == null || !dict.TryGetValue(key, out var value) || value == null)
            {
                return fallback;
            }

            return value.ToString();
        }

        public static float Float(Dictionary<string, object> dict, string key, float fallback = 0f)
        {
            if (dict == null || !dict.TryGetValue(key, out var value) || value == null)
            {
                return fallback;
            }

            return Convert.ToSingle(value, CultureInfo.InvariantCulture);
        }

        public static int Int(Dictionary<string, object> dict, string key, int fallback = 0)
        {
            if (dict == null || !dict.TryGetValue(key, out var value) || value == null)
            {
                return fallback;
            }

            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        public static bool Bool(Dictionary<string, object> dict, string key, bool fallback = false)
        {
            if (dict == null || !dict.TryGetValue(key, out var value) || value == null)
            {
                return fallback;
            }

            return value is bool boolValue ? boolValue : fallback;
        }

        private sealed class rimrushJsonReader
        {
            private readonly string source;
            private int index;

            public rimrushJsonReader(string source)
            {
                this.source = source;
            }

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

            private bool TryReadDecimalDigits()
            {
                var start = index;
                while (!IsAtEnd && char.IsDigit(source[index]))
                {
                    index++;
                }

                return index > start;
            }

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

            private void SkipWhitespace()
            {
                while (!IsAtEnd && char.IsWhiteSpace(source[index]))
                {
                    index++;
                }
            }

            private char ReadChar()
            {
                return source[index++];
            }

            private bool TryConsume(char expected)
            {
                if (IsAtEnd || source[index] != expected)
                {
                    return false;
                }

                index++;
                return true;
            }

            private static bool IsNumberStart(char value)
            {
                return value == '-' || char.IsDigit(value);
            }

            private static bool IsNonZeroDigit(char value)
            {
                return value >= '1' && value <= '9';
            }

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
