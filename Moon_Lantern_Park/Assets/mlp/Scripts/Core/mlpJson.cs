// Simple JSON parser
// A self-written JSON parsing tool that does not rely on external libraries. It can parse JSON strings into dictionaries and lists, and convert dictionaries and lists back to JSON strings. Used to save and read game data.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace mlp
{
    /// <summary>
    /// Simple JSON parser: JSON tool written by myself, does not rely on external libraries. It can parse JSON strings into dictionaries and lists, and also convert data back to JSON strings. Used to save and read game data.
    /// </summary>
    public static class mlpJson
    {
        /// <summary>
        /// Parse a JSON string into a dictionary, list, string, number, boolean, or null.
        /// If the input is empty or contains illegal JSON format, null is returned.
        /// </summary>
        /// <param name="json">The raw JSON text to be parsed. </param>
        /// <returns>The parsed object, returns null if parsing fails. </returns>
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
        /// Converts a parsed JSON object into a dictionary keyed by strings. If the object is not of type dictionary, null is returned.
        /// </summary>
        /// <param name="value">The parsed JSON object. </param>
        /// <returns>The converted dictionary, if the object is not a dictionary type, null is returned. </returns>
        public static Dictionary<string, object> AsDict(object value)
        {
            return value as Dictionary<string, object>;
        }

        /// <summary>
        /// Convert a parsed JSON object to a list. If the object is not of type list, null is returned.
        /// </summary>
        /// <param name="value">The parsed JSON object. </param>
        /// <returns>The converted list, or null if the object is not a list type. </returns>
        public static List<object> AsList(object value)
        {
            return value as List<object>;
        }

        /// <summary>
        /// Finds a nested dictionary value based on key from the parent dictionary. If the key does not exist or the value is not of dictionary type, null is returned.
        /// </summary>
        /// <param name="dict">The parent dictionary to search for. </param>
        /// <param name="key">The key to look for. </param>
        /// <returns>Nested dictionary, returns null if not found. </returns>
        public static Dictionary<string, object> Dict(Dictionary<string, object> dict, string key)
        {
            return dict != null && dict.TryGetValue(key, out var value) ? AsDict(value) : null;
        }

        /// <summary>
        /// Find a list value from a dictionary based on a key. If the key does not exist or the value is not a list type, null is returned.
        /// </summary>
        /// <param name="dict">Dictionary to search. </param>
        /// <param name="key">The key to look for. </param>
        /// <returns>List value, returns null if not found. </returns>
        public static List<object> List(Dictionary<string, object> dict, string key)
        {
            return dict != null && dict.TryGetValue(key, out var value) ? AsList(value) : null;
        }

        /// <summary>
        /// Read a string value from a dictionary based on a key. If the key does not exist or the value is null, the default value is returned.
        /// </summary>
        /// <param name="dict">The dictionary to read. </param>
        /// <param name="key">The key to read. </param>
        /// <param name="fallback">The default value returned when the key does not exist or the value is null. </param>
        /// <returns>The read string value, or the default value. </returns>
        public static string String(Dictionary<string, object> dict, string key, string fallback = "")
        {
            if (dict == null || !dict.TryGetValue(key, out var value) || value == null)
            {
                return fallback;
            }

            return value.ToString();
        }

        /// <summary>
        /// Reads a floating point value from a dictionary based on a key. Other numeric types are automatically converted to floating point numbers. Returns the default value if the key does not exist or the value is null.
        /// </summary>
        /// <param name="dict">The dictionary to read. </param>
        /// <param name="key">The key to read. </param>
        /// <param name="fallback">The default value returned when the key does not exist or the value is null. </param>
        /// <returns>The floating point value read, or the default value. </returns>
        public static float Float(Dictionary<string, object> dict, string key, float fallback = 0f)
        {
            if (dict == null || !dict.TryGetValue(key, out var value) || value == null)
            {
                return fallback;
            }

            return Convert.ToSingle(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Read an integer value from a dictionary based on key. Other numeric types are automatically converted to integers. Returns the default value if the key does not exist or the value is null.
        /// </summary>
        /// <param name="dict">The dictionary to read. </param>
        /// <param name="key">The key to read. </param>
        /// <param name="fallback">The default value returned when the key does not exist or the value is null. </param>
        /// <returns>The integer value read, or the default value. </returns>
        public static int Int(Dictionary<string, object> dict, string key, int fallback = 0)
        {
            if (dict == null || !dict.TryGetValue(key, out var value) || value == null)
            {
                return fallback;
            }

            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Read a boolean value from a dictionary based on a key. If the key does not exist, the value is null, or is not of type boolean, the default value is returned.
        /// </summary>
        /// <param name="dict">The dictionary to read. </param>
        /// <param name="key">The key to read. </param>
        /// <param name="fallback">The default value returned when the key does not exist or the value is not of boolean type. </param>
        /// <returns>The Boolean value read, or the default value. </returns>
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
            /// Creates a reader that parses the given JSON string character by character.
            /// </summary>
            /// <param name="source">The raw JSON text to be parsed. </param>
            public mlpJsonReader(string source)
            {
                this.source = source;
            }

            /// <summary>
            /// Read a complete JSON document. Returns true when the entire input is correctly parsed as valid JSON.
            /// </summary>
            /// <param name="value">Parse result (dict, list, string, number, boolean, or null). </param>
            /// <returns>Returns true when parsing is successful and all input has been consumed. </returns>
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
            /// Reads the next JSON value (object, array, string, number, boolean, or null) from the current position.
            /// </summary>
            /// <param name="value">The value obtained by parsing. </param>
            /// <returns>Returns true when a valid JSON value is successfully read. </returns>
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
            /// Read a JSON object starting with '{' and parse it into a dictionary with strings as keys.
            /// </summary>
            /// <param name="value">The dictionary obtained by parsing, null if failed. </param>
            /// <returns>Returns true when the object is parsed successfully. </returns>
            private bool TryReadObject(out object value)
            {
                // 1. Create an empty dictionary and read the left curly brace
                var map = new Dictionary<string, object>();
                value = map;

                if (!TryConsume('{'))
                {
                    return false;
                }

                // 2. Empty objects are returned directly

                SkipWhitespace();
                if (TryConsume('}'))
                {
                    return true;
                }

                // 3. Loop to read key-value pairs: key -> colon -> value

                while (true)
                {
                    // 4. Read the key name (string)

                    SkipWhitespace();
                    if (!TryReadStringValue(out var propertyName))
                    {
                        return false;
                    }

                    // 5. Read colon delimiter

                    SkipWhitespace();
                    if (!TryConsume(':'))
                    {
                        return false;
                    }

                    // 6. Read the value (can be any JSON type)

                    if (!TryReadValue(out var propertyValue))
                    {
                        return false;
                    }

                    // 7. Save to dictionary
                    map[propertyName] = propertyValue;

                    // 8. End when encountering the right curly brace, continue reading the next pair when encountering the comma
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
            /// Read a JSON array starting with '[' and parse it into a list of objects.
            /// </summary>
            /// <param name="value">The parsed list, null if failed. </param>
            /// <returns>Returns true when array parsing is successful. </returns>
            private bool TryReadArray(out object value)
            {
                // 1. Create an empty list and read the left square bracket

                var items = new List<object>();
                value = items;

                if (!TryConsume('['))
                {
                    return false;
                }

                // 2. Empty array is returned directly
                SkipWhitespace();
                if (TryConsume(']'))
                {
                    return true;
                }

                // 3. Loop to read each element

                while (true)
                {
                    // 4. Read a value

                    if (!TryReadValue(out var itemValue))
                    {
                        return false;
                    }

                    // 5. Add to list
                    items.Add(itemValue);

                    // 6. End when encountering the right square bracket, continue reading the next one when encountering the comma
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
            /// Reads a JSON string wrapped in double quotes and wraps it as an object.
            /// </summary>
            /// <param name="value">String value wrapped in an object. </param>
            /// <returns>Returns true when string parsing is successful. </returns>
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
            /// Read JSON strings and handle escape sequences (such as \n, \t, \uXXXX, etc.).
            /// </summary>
            /// <param name="value">The decoded string, or null on failure. </param>
            /// <returns>Returns true when string parsing is successful. </returns>
            private bool TryReadStringValue(out string value)
            {
                // 1. Read the opening quote

                value = null;
                if (!TryConsume('"'))
                {
                    return false;
                }

                // 2. Read character by character until the closing quotation mark is encountered

                var builder = new StringBuilder();
                while (!IsAtEnd)
                {
                    var current = ReadChar();
                    // 3. Closing quote: end of string
                    if (current == '"')
                    {
                        value = builder.ToString();
                        return true;
                    }

                    // 4. Backslash: Handling escape sequences

                    if (current == '\\')
                    {
                        if (!TryAppendEscapeSequence(builder))
                        {
                            return false;
                        }

                        continue;
                    }

                    // 5. Illegal control character

                    if (current < ' ')
                    {
                        return false;
                    }

                    // 6. Ordinary characters are appended directly
                    builder.Append(current);
                }

                return false;
            }

            /// <summary>
            /// Reads an escape character after the backslash and appends the decoded result to a StringBuilder.
            /// Escape sequences such as \", \\, \/, \b, \f, \n, \r, \t and \uXXXX are supported.
            /// </summary>
            /// <param name="builder">StringBuilder for appending decoded characters. </param>
            /// <returns>Returns true if the escape sequence is legal. </returns>
            private bool TryAppendEscapeSequence(StringBuilder builder)
            {
                // 1. Make sure there are characters after the backslash

                if (IsAtEnd)
                {
                    return false;
                }

                // 2. Append the corresponding actual characters according to the escape character type
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
                    // 3. \u represents Unicode escape and is handled by specialized methods.

                    case 'u':
                        return TryAppendUnicode(builder);
                    default:
                        return false;
                }
            }

            /// <summary>
            /// Reads a \uXXXX escape sequence and appends the decoded Unicode characters to a StringBuilder.

            /// Surrogate pairs (two consecutive \u sequences representing characters above U+FFFF) are supported.
            /// </summary>
            /// <param name="builder">StringBuilder for appending decoded characters. </param>
            /// <returns>Returns true if the hexadecimal number is legal. </returns>
            private bool TryAppendUnicode(StringBuilder builder)
            {
                // 1. Read the first four-digit hexadecimal number

                if (!TryReadHexQuad(out var firstUnit))
                {
                    return false;
                }

                // 2. If it is not a high surrogate (BMP character), append it directly
                var firstChar = (char)firstUnit;
                if (!char.IsHighSurrogate(firstChar))
                {
                    builder.Append(firstChar);
                    return true;
                }

                // 3. is a high surrogate, try to read the second \uXXXX (low surrogate)

                var rewind = index;
                if (!TryConsume('\\') || !TryConsume('u') || !TryReadHexQuad(out var secondUnit))
                {
                    // 4. Unable to find matching low surrogate, only append high surrogate

                    index = rewind;
                    builder.Append(firstChar);
                    return true;
                }

                // 5. The verification is a low surrogate item. If the pairing is successful, both will be appended.
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
            /// Read exactly four hexadecimal characters and convert them to an integer value.
            /// </summary>
            /// <param name="value">Decoded 16-bit integer. </param>
            /// <returns>Returns true when four legal hexadecimal digits are successfully read. </returns>
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
            /// Try to match a literal keyword (such as "true", "false" or "null") and return the corresponding value if the match is successful.
            /// </summary>
            /// <param name="keyword">Keyword text to match (such as "true"). </param>
            /// <param name="replacement">The C# value returned when the match is successful (such as "true" for true, "null" for null). </param>
            /// <param name="value">The replacement value if the match is successful, otherwise it is null. </param>
            /// <returns>Returns true when the current position matches the keyword. </returns>
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
            /// Read a JSON number (integer or decimal, supports optional sign and exponent parts).
            /// Integers are returned as long, and decimals are returned as double.
            /// </summary>
            /// <param name="value">The parsed number, long or double type. </param>
            /// <returns>Returns true when a legal number is successfully read. </returns>
            private bool TryReadNumber(out object value)
            {
                // 1. Record the starting position of the number and read the optional negative sign

                value = null;
                var start = index;
                var isWholeNumber = true;

                if (TryConsume('-') && IsAtEnd)
                {
                    return false;
                }

                // 2. Read the integer part

                if (!TryReadIntegerDigits())
                {
                    return false;
                }

                // 3. If there is a decimal point, read the decimal part

                if (TryConsume('.'))
                {
                    isWholeNumber = false;
                    if (!TryReadDecimalDigits())
                    {
                        return false;
                    }
                }

                // 4. If e/E is present, read the exponent part

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

                // 5. Use long for integers and double for decimals.

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
            /// Reads the integer part of a number (one or more digits, leading zeros are not allowed except "0" itself).
            /// </summary>
            /// <returns>Returns true if at least one digit is read. </returns>
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
            /// Reads one or more digits of the decimal or exponent part of a number.

            /// </summary>
            /// <returns>Returns true if at least one digit is read. </returns>
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
            /// Checks whether the given keyword appears at the current read position without moving the read position.
            /// </summary>
            /// <param name="keyword">Text to match. </param>
            /// <returns>Returns true when the current position matches the keyword. </returns>
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
            /// Skips all spaces, tabs, and newlines until a non-whitespace character is encountered.

            /// </summary>
            private void SkipWhitespace()
            {
                while (!IsAtEnd && char.IsWhiteSpace(source[index]))
                {
                    index++;
                }
            }

            /// <summary>
            /// Returns the character at the current position and moves the reading position one position backward.
            /// </summary>
            /// <returns>The characters read. </returns>
            private char ReadChar()
            {
                return source[index++];
            }

            /// <summary>
            /// If the current character matches the expected character, skip the character and return true, otherwise return false.
            /// </summary>
            /// <param name="expected">Characters expected to match. </param>
            /// <returns>Returns true when the character is successfully matched and consumed. </returns>
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
            /// Determines whether a character may be the starting character of a JSON number (a digit or a minus sign).
            /// </summary>
            /// <param name="value">The character to test. </param>
            /// <returns> Returns true if the character is '-' or a number. </returns>
            private static bool IsNumberStart(char value)
            {
                return value == '-' || char.IsDigit(value);
            }

            /// <summary>
            /// Determines whether the character is a number between 1 and 9 (excluding zero).
            /// </summary>
            /// <param name="value">The character to test. </param>
            /// <returns> Returns true if the character is between '1' and '9'. </returns>
            private static bool IsNonZeroDigit(char value)
            {
                return value >= '1' && value <= '9';
            }

            /// <summary>
            /// Converts single hexadecimal characters (0-9, a-f, A-F) to corresponding integer values.
            /// </summary>
            /// <param name="value">The hexadecimal character to decode. </param>
            /// <returns>The corresponding integer value (0-15), -1 is returned when the character is not a legal hexadecimal number. </returns>
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
