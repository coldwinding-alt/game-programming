using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BasketballLegends2020
{
    public static class BLJson
    {
        public static object Parse(string json)
        {
            return Parser.Parse(json);
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

            return value is bool b ? b : fallback;
        }

        private sealed class Parser
        {
            private const string WordBreak = "{}[],:\"";
            private readonly string json;
            private int index;

            private Parser(string json)
            {
                this.json = json;
            }

            public static object Parse(string json)
            {
                if (string.IsNullOrEmpty(json))
                {
                    return null;
                }

                return new Parser(json).ParseValue();
            }

            private Dictionary<string, object> ParseObject()
            {
                var table = new Dictionary<string, object>();
                Read();

                while (true)
                {
                    switch (NextToken)
                    {
                        case Token.None:
                            return null;
                        case Token.CurlyClose:
                            Read();
                            return table;
                        case Token.Comma:
                            Read();
                            continue;
                    }

                    var name = ParseString();
                    if (name == null)
                    {
                        return null;
                    }

                    if (NextToken != Token.Colon)
                    {
                        return null;
                    }

                    Read();
                    table[name] = ParseValue();
                }
            }

            private List<object> ParseArray()
            {
                var array = new List<object>();
                Read();

                var parsing = true;
                while (parsing)
                {
                    var token = NextToken;
                    switch (token)
                    {
                        case Token.None:
                            return null;
                        case Token.SquareClose:
                            Read();
                            parsing = false;
                            break;
                        case Token.Comma:
                            Read();
                            break;
                        default:
                            array.Add(ParseValue());
                            break;
                    }
                }

                return array;
            }

            private object ParseValue()
            {
                switch (NextToken)
                {
                    case Token.String:
                        return ParseString();
                    case Token.Number:
                        return ParseNumber();
                    case Token.CurlyOpen:
                        return ParseObject();
                    case Token.SquareOpen:
                        return ParseArray();
                    case Token.True:
                        ReadWord("true");
                        return true;
                    case Token.False:
                        ReadWord("false");
                        return false;
                    case Token.Null:
                        ReadWord("null");
                        return null;
                    default:
                        return null;
                }
            }

            private string ParseString()
            {
                var builder = new StringBuilder();
                Read();

                var parsing = true;
                while (parsing && index < json.Length)
                {
                    var c = Read();
                    switch (c)
                    {
                        case '"':
                            parsing = false;
                            break;
                        case '\\':
                            if (index == json.Length)
                            {
                                break;
                            }

                            c = Read();
                            switch (c)
                            {
                                case '"':
                                case '\\':
                                case '/':
                                    builder.Append(c);
                                    break;
                                case 'b':
                                    builder.Append('\b');
                                    break;
                                case 'f':
                                    builder.Append('\f');
                                    break;
                                case 'n':
                                    builder.Append('\n');
                                    break;
                                case 'r':
                                    builder.Append('\r');
                                    break;
                                case 't':
                                    builder.Append('\t');
                                    break;
                                case 'u':
                                    if (index + 4 <= json.Length)
                                    {
                                        var hex = json.Substring(index, 4);
                                        builder.Append((char)Convert.ToInt32(hex, 16));
                                        index += 4;
                                    }
                                    break;
                            }
                            break;
                        default:
                            builder.Append(c);
                            break;
                    }
                }

                return builder.ToString();
            }

            private object ParseNumber()
            {
                var word = NextWord;
                if (word.IndexOf('.') < 0 && word.IndexOf('e') < 0 && word.IndexOf('E') < 0)
                {
                    if (long.TryParse(word, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
                    {
                        return integer;
                    }
                }

                double.TryParse(word, NumberStyles.Float, CultureInfo.InvariantCulture, out var number);
                return number;
            }

            private void EatWhitespace()
            {
                while (index < json.Length && char.IsWhiteSpace(json[index]))
                {
                    index++;
                }
            }

            private char Read()
            {
                return json[index++];
            }

            private void ReadWord(string word)
            {
                index += word.Length;
            }

            private string NextWord
            {
                get
                {
                    EatWhitespace();
                    var builder = new StringBuilder();
                    while (index < json.Length && WordBreak.IndexOf(json[index]) == -1 && !char.IsWhiteSpace(json[index]))
                    {
                        builder.Append(json[index++]);
                    }

                    return builder.ToString();
                }
            }

            private Token NextToken
            {
                get
                {
                    EatWhitespace();
                    if (index == json.Length)
                    {
                        return Token.None;
                    }

                    switch (json[index])
                    {
                        case '{':
                            return Token.CurlyOpen;
                        case '}':
                            return Token.CurlyClose;
                        case '[':
                            return Token.SquareOpen;
                        case ']':
                            return Token.SquareClose;
                        case ',':
                            return Token.Comma;
                        case '"':
                            return Token.String;
                        case ':':
                            return Token.Colon;
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                        case '-':
                            return Token.Number;
                    }

                    var word = NextWord;
                    index -= word.Length;
                    switch (word)
                    {
                        case "true":
                            return Token.True;
                        case "false":
                            return Token.False;
                        case "null":
                            return Token.Null;
                        default:
                            return Token.None;
                    }
                }
            }

            private enum Token
            {
                None,
                CurlyOpen,
                CurlyClose,
                SquareOpen,
                SquareClose,
                Colon,
                Comma,
                String,
                Number,
                True,
                False,
                Null
            }
        }
    }
}
