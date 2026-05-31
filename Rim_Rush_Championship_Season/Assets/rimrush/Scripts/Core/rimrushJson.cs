// 文件作用：这个脚本负责本模块的核心逻辑与协作调度。
// 概括：rimrushJson 用来处理对应子系统的关键流程，先看这里能快速定位功能入口。

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace rimrush
{
    public static class rimrushJson
    {
        /// <summary>
        /// Executes Parse for the rimrushJson workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="json">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static object Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var reader = new rimrushJsonReader(json);
            return reader.TryReadDocument(out var value) ? value : null;
        }

        /// <summary>
        /// Executes As Dict for the rimrushJson workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="value">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static Dictionary<string, object> AsDict(object value)
        {
            return value as Dictionary<string, object>;
        }

        /// <summary>
        /// Executes As List for the rimrushJson workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="value">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static List<object> AsList(object value)
        {
            return value as List<object>;
        }

        /// <summary>
        /// Executes Dict for the rimrushJson workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dict">Input value used by this step of the workflow.</param>
        /// <param name="key">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static Dictionary<string, object> Dict(Dictionary<string, object> dict, string key)
        {
            return dict != null && dict.TryGetValue(key, out var value) ? AsDict(value) : null;
        }

        /// <summary>
        /// Executes List for the rimrushJson workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dict">Input value used by this step of the workflow.</param>
        /// <param name="key">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static List<object> List(Dictionary<string, object> dict, string key)
        {
            return dict != null && dict.TryGetValue(key, out var value) ? AsList(value) : null;
        }

        /// <summary>
        /// Executes String for the rimrushJson workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dict">Input value used by this step of the workflow.</param>
        /// <param name="key">Input value used by this step of the workflow.</param>
        /// <param name="fallback">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static string String(Dictionary<string, object> dict, string key, string fallback = "")
        {
            if (dict == null || !dict.TryGetValue(key, out var value) || value == null)
            {
                return fallback;
            }

            return value.ToString();
        }

        /// <summary>
        /// Executes Float for the rimrushJson workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dict">Input value used by this step of the workflow.</param>
        /// <param name="key">Input value used by this step of the workflow.</param>
        /// <param name="fallback">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static float Float(Dictionary<string, object> dict, string key, float fallback = 0f)
        {
            if (dict == null || !dict.TryGetValue(key, out var value) || value == null)
            {
                return fallback;
            }

            return Convert.ToSingle(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Executes Int for the rimrushJson workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dict">Input value used by this step of the workflow.</param>
        /// <param name="key">Input value used by this step of the workflow.</param>
        /// <param name="fallback">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static int Int(Dictionary<string, object> dict, string key, int fallback = 0)
        {
            if (dict == null || !dict.TryGetValue(key, out var value) || value == null)
            {
                return fallback;
            }

            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Executes Bool for the rimrushJson workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dict">Input value used by this step of the workflow.</param>
        /// <param name="key">Input value used by this step of the workflow.</param>
        /// <param name="fallback">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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

            /// <summary>
            /// Executes rimrush Json Reader for the rimrushJsonReader workflow.
            /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
            /// </summary>
            /// <param name="source">Input value used by this step of the workflow.</param>
            public rimrushJsonReader(string source)
            {
                this.source = source;
            }

            /// <summary>
            /// Executes Try Read Document for the rimrushJsonReader workflow.
            /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
            /// </summary>
            /// <param name="value">Input value used by this step of the workflow.</param>
            /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
            /// Executes Try Read Value for the rimrushJsonReader workflow.
            /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
            /// </summary>
            /// <param name="value">Input value used by this step of the workflow.</param>
            /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
            /// Executes Try Read Object for the rimrushJsonReader workflow.
            /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
            /// </summary>
            /// <param name="value">Input value used by this step of the workflow.</param>
            /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
            /// Executes Try Read Array for the rimrushJsonReader workflow.
            /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
            /// </summary>
            /// <param name="value">Input value used by this step of the workflow.</param>
            /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
            /// Executes Try Read String for the rimrushJsonReader workflow.
            /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
            /// </summary>
            /// <param name="value">Input value used by this step of the workflow.</param>
            /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
            /// Executes Try Read String Value for the rimrushJsonReader workflow.
            /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
            /// </summary>
            /// <param name="value">Input value used by this step of the workflow.</param>
            /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
            /// Executes Try Append Escape Sequence for the rimrushJsonReader workflow.
            /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
            /// </summary>
            /// <param name="builder">Input value used by this step of the workflow.</param>
            /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
            /// Executes Try Append Unicode for the rimrushJsonReader workflow.
            /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
            /// </summary>
            /// <param name="builder">Input value used by this step of the workflow.</param>
            /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
            /// Executes Try Read Hex Quad for the rimrushJsonReader workflow.
            /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
            /// </summary>
            /// <param name="value">Input value used by this step of the workflow.</param>
            /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
            /// Executes Try Read Keyword for the rimrushJsonReader workflow.
            /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
            /// </summary>
            /// <param name="keyword">Input value used by this step of the workflow.</param>
            /// <param name="replacement">Input value used by this step of the workflow.</param>
            /// <param name="value">Input value used by this step of the workflow.</param>
            /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
            /// Executes Try Read Number for the rimrushJsonReader workflow.
            /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
            /// </summary>
            /// <param name="value">Input value used by this step of the workflow.</param>
            /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
            /// Executes Try Read Integer Digits for the rimrushJsonReader workflow.
            /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
            /// </summary>
            /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
            /// Executes Try Read Decimal Digits for the rimrushJsonReader workflow.
            /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
            /// </summary>
            /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
            /// Executes Matches for the rimrushJsonReader workflow.
            /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
            /// </summary>
            /// <param name="keyword">Input value used by this step of the workflow.</param>
            /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
            /// Executes Skip Whitespace for the rimrushJsonReader workflow.
            /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
            /// </summary>
            private void SkipWhitespace()
            {
                while (!IsAtEnd && char.IsWhiteSpace(source[index]))
                {
                    index++;
                }
            }

            /// <summary>
            /// Executes Read Char for the rimrushJsonReader workflow.
            /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
            /// </summary>
            /// <returns>Result produced for downstream logic in the current frame.</returns>
            private char ReadChar()
            {
                return source[index++];
            }

            /// <summary>
            /// Executes Try Consume for the rimrushJsonReader workflow.
            /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
            /// </summary>
            /// <param name="expected">Input value used by this step of the workflow.</param>
            /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
            /// Executes Is Number Start for the rimrushJsonReader workflow.
            /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
            /// </summary>
            /// <param name="value">Input value used by this step of the workflow.</param>
            /// <returns>True when the requested operation succeeds; otherwise false.</returns>
            private static bool IsNumberStart(char value)
            {
                return value == '-' || char.IsDigit(value);
            }

            /// <summary>
            /// Executes Is Non Zero Digit for the rimrushJsonReader workflow.
            /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
            /// </summary>
            /// <param name="value">Input value used by this step of the workflow.</param>
            /// <returns>True when the requested operation succeeds; otherwise false.</returns>
            private static bool IsNonZeroDigit(char value)
            {
                return value >= '1' && value <= '9';
            }

            /// <summary>
            /// Executes Decode Hex Digit for the rimrushJsonReader workflow.
            /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
            /// </summary>
            /// <param name="value">Input value used by this step of the workflow.</param>
            /// <returns>Result produced for downstream logic in the current frame.</returns>
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
