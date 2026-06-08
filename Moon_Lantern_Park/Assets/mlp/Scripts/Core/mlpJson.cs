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
        /// 将 JSON 字符串解析为字典、列表、字符串、数字、布尔值或 null。
        /// 如果输入为空或包含非法 JSON 格式，则返回 null。
        /// </summary>
        /// <param name="json">待解析的原始 JSON 文本。</param>
        /// <returns>解析后的对象，解析失败时返回 null。</returns>
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
        /// 将已解析的 JSON 对象转换为以字符串为键的字典。如果对象不是字典类型，则返回 null。
        /// </summary>
        /// <param name="value">已解析的 JSON 对象。</param>
        /// <returns>转换后的字典，若对象不是字典类型则返回 null。</returns>
        public static Dictionary<string, object> AsDict(object value)
        {
            return value as Dictionary<string, object>;
        }

        /// <summary>
        /// 将已解析的 JSON 对象转换为列表。如果对象不是列表类型，则返回 null。
        /// </summary>
        /// <param name="value">已解析的 JSON 对象。</param>
        /// <returns>转换后的列表，若对象不是列表类型则返回 null。</returns>
        public static List<object> AsList(object value)
        {
            return value as List<object>;
        }

        /// <summary>
        /// 从父字典中根据键查找嵌套的字典值。如果键不存在或值不是字典类型，则返回 null。
        /// </summary>
        /// <param name="dict">要搜索的父字典。</param>
        /// <param name="key">要查找的键。</param>
        /// <returns>嵌套的字典，未找到时返回 null。</returns>
        public static Dictionary<string, object> Dict(Dictionary<string, object> dict, string key)
        {
            return dict != null && dict.TryGetValue(key, out var value) ? AsDict(value) : null;
        }

        /// <summary>
        /// 从字典中根据键查找列表值。如果键不存在或值不是列表类型，则返回 null。
        /// </summary>
        /// <param name="dict">要搜索的字典。</param>
        /// <param name="key">要查找的键。</param>
        /// <returns>列表值，未找到时返回 null。</returns>
        public static List<object> List(Dictionary<string, object> dict, string key)
        {
            return dict != null && dict.TryGetValue(key, out var value) ? AsList(value) : null;
        }

        /// <summary>
        /// 从字典中根据键读取字符串值。如果键不存在或值为 null，则返回默认值。
        /// </summary>
        /// <param name="dict">要读取的字典。</param>
        /// <param name="key">要读取的键。</param>
        /// <param name="fallback">键不存在或值为 null 时返回的默认值。</param>
        /// <returns>读取到的字符串值，或默认值。</returns>
        public static string String(Dictionary<string, object> dict, string key, string fallback = "")
        {
            if (dict == null || !dict.TryGetValue(key, out var value) || value == null)
            {
                return fallback;
            }

            return value.ToString();
        }

        /// <summary>
        /// 从字典中根据键读取浮点数值。会自动将其他数值类型转换为浮点数。键不存在或值为 null 时返回默认值。
        /// </summary>
        /// <param name="dict">要读取的字典。</param>
        /// <param name="key">要读取的键。</param>
        /// <param name="fallback">键不存在或值为 null 时返回的默认值。</param>
        /// <returns>读取到的浮点数值，或默认值。</returns>
        public static float Float(Dictionary<string, object> dict, string key, float fallback = 0f)
        {
            if (dict == null || !dict.TryGetValue(key, out var value) || value == null)
            {
                return fallback;
            }

            return Convert.ToSingle(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 从字典中根据键读取整数值。会自动将其他数值类型转换为整数。键不存在或值为 null 时返回默认值。
        /// </summary>
        /// <param name="dict">要读取的字典。</param>
        /// <param name="key">要读取的键。</param>
        /// <param name="fallback">键不存在或值为 null 时返回的默认值。</param>
        /// <returns>读取到的整数值，或默认值。</returns>
        public static int Int(Dictionary<string, object> dict, string key, int fallback = 0)
        {
            if (dict == null || !dict.TryGetValue(key, out var value) || value == null)
            {
                return fallback;
            }

            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 从字典中根据键读取布尔值。如果键不存在、值为 null 或不是布尔类型，则返回默认值。
        /// </summary>
        /// <param name="dict">要读取的字典。</param>
        /// <param name="key">要读取的键。</param>
        /// <param name="fallback">键不存在或值不是布尔类型时返回的默认值。</param>
        /// <returns>读取到的布尔值，或默认值。</returns>
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
            /// 创建一个逐字符解析给定 JSON 字符串的读取器。
            /// </summary>
            /// <param name="source">待解析的原始 JSON 文本。</param>
            public mlpJsonReader(string source)
            {
                this.source = source;
            }

            /// <summary>
            /// 读取一个完整的 JSON 文档。当整个输入被正确解析为合法 JSON 时返回 true。
            /// </summary>
            /// <param name="value">解析结果（字典、列表、字符串、数字、布尔值或 null）。</param>
            /// <returns>解析成功且全部输入已消费时返回 true。</returns>
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
            /// 从当前位置读取下一个 JSON 值（对象、数组、字符串、数字、布尔值或 null）。
            /// </summary>
            /// <param name="value">解析得到的值。</param>
            /// <returns>成功读取到合法 JSON 值时返回 true。</returns>
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
            /// 读取一个以 '{' 开头的 JSON 对象，解析为以字符串为键的字典。
            /// </summary>
            /// <param name="value">解析得到的字典，失败时为 null。</param>
            /// <returns>对象解析成功时返回 true。</returns>
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
            /// 读取一个以 '[' 开头的 JSON 数组，解析为对象列表。
            /// </summary>
            /// <param name="value">解析得到的列表，失败时为 null。</param>
            /// <returns>数组解析成功时返回 true。</returns>
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
            /// 读取一个用双引号包裹的 JSON 字符串，并将其包装为对象。
            /// </summary>
            /// <param name="value">包装在对象中的字符串值。</param>
            /// <returns>字符串解析成功时返回 true。</returns>
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
            /// 读取 JSON 字符串并处理转义序列（如 \n、\t、\uXXXX 等）。
            /// </summary>
            /// <param name="value">解码后的字符串，失败时为 null。</param>
            /// <returns>字符串解析成功时返回 true。</returns>
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
            /// 读取反斜杠后面的一个转义字符，并将解码结果追加到 StringBuilder 中。
            /// 支持 \"、\\、\/、\b、\f、\n、\r、\t 和 \uXXXX 等转义序列。
            /// </summary>
            /// <param name="builder">用于追加解码字符的 StringBuilder。</param>
            /// <returns>转义序列合法时返回 true。</returns>
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
            /// 读取一个 \uXXXX 转义序列，将解码后的 Unicode 字符追加到 StringBuilder 中。
            /// 支持代理对（两个连续的 \u 序列，用于表示 U+FFFF 以上的字符）。
            /// </summary>
            /// <param name="builder">用于追加解码字符的 StringBuilder。</param>
            /// <returns>十六进制数字合法时返回 true。</returns>
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
            /// 读取恰好四个十六进制字符，并将其转换为整数值。
            /// </summary>
            /// <param name="value">解码后的 16 位整数。</param>
            /// <returns>成功读取四个合法十六进制数字时返回 true。</returns>
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
            /// 尝试匹配一个字面量关键字（如 "true"、"false" 或 "null"），匹配成功时返回对应的值。
            /// </summary>
            /// <param name="keyword">要匹配的关键字文本（如 "true"）。</param>
            /// <param name="replacement">匹配成功时返回的 C# 值（如 "true" 对应 true，"null" 对应 null）。</param>
            /// <param name="value">匹配成功时的替换值，否则为 null。</param>
            /// <returns>当前位置匹配到关键字时返回 true。</returns>
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
            /// 读取一个 JSON 数字（整数或小数，支持可选的正负号和指数部分）。
            /// 整数返回 long 类型，小数返回 double 类型。
            /// </summary>
            /// <param name="value">解析后的数字，long 或 double 类型。</param>
            /// <returns>成功读取到合法数字时返回 true。</returns>
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
            /// 读取数字的整数部分（一个或多个数字，除 "0" 本身外不允许前导零）。
            /// </summary>
            /// <returns>至少读取到一位数字时返回 true。</returns>
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
            /// 读取数字的小数部分或指数部分的一个或多个数字。
            /// </summary>
            /// <returns>至少读取到一位数字时返回 true。</returns>
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
            /// 检查给定关键字是否出现在当前读取位置，不移动读取位置。
            /// </summary>
            /// <param name="keyword">要匹配的文本。</param>
            /// <returns>当前位置匹配到关键字时返回 true。</returns>
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
            /// 跳过所有空格、制表符和换行符，直到遇到非空白字符为止。
            /// </summary>
            private void SkipWhitespace()
            {
                while (!IsAtEnd && char.IsWhiteSpace(source[index]))
                {
                    index++;
                }
            }

            /// <summary>
            /// 返回当前位置的字符，并将读取位置向后移动一位。
            /// </summary>
            /// <returns>读取到的字符。</returns>
            private char ReadChar()
            {
                return source[index++];
            }

            /// <summary>
            /// 如果当前字符与期望字符匹配，则跳过该字符并返回 true，否则返回 false。
            /// </summary>
            /// <param name="expected">期望匹配的字符。</param>
            /// <returns>成功匹配并消费该字符时返回 true。</returns>
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
            /// 判断字符是否可能是 JSON 数字的起始字符（数字或负号）。
            /// </summary>
            /// <param name="value">要测试的字符。</param>
            /// <returns>字符为 '-' 或数字时返回 true。</returns>
            private static bool IsNumberStart(char value)
            {
                return value == '-' || char.IsDigit(value);
            }

            /// <summary>
            /// 判断字符是否为 1 到 9 之间的数字（不包括零）。
            /// </summary>
            /// <param name="value">要测试的字符。</param>
            /// <returns>字符在 '1' 到 '9' 之间时返回 true。</returns>
            private static bool IsNonZeroDigit(char value)
            {
                return value >= '1' && value <= '9';
            }

            /// <summary>
            /// 将单个十六进制字符（0-9、a-f、A-F）转换为对应的整数值。
            /// </summary>
            /// <param name="value">要解码的十六进制字符。</param>
            /// <returns>对应的整数值（0-15），字符不是合法十六进制数字时返回 -1。</returns>
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
