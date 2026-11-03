using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace OhtaTool.Plugin.Core
{
    /// <summary>Đối tượng JSON giữ nguyên thứ tự khoá (để ghi lại module.json không xáo trộn, không mất trường lạ).</summary>
    public sealed class JsonObject
    {
        private readonly List<KeyValuePair<string, object>> _items = new List<KeyValuePair<string, object>>();

        public IReadOnlyList<KeyValuePair<string, object>> Items => _items;

        public object Get(string key)
        {
            int i = IndexOf(key);
            return i >= 0 ? _items[i].Value : null;
        }

        public string GetString(string key) => Get(key) as string;

        /// <summary>Gán giá trị: khoá có sẵn giữ nguyên vị trí, khoá mới thêm vào cuối.</summary>
        public void Set(string key, object value)
        {
            int i = IndexOf(key);
            if (i >= 0)
                _items[i] = new KeyValuePair<string, object>(key, value);
            else
                _items.Add(new KeyValuePair<string, object>(key, value));
        }

        /// <summary>Gán giá trị; nếu khoá chưa có thì chèn ngay sau khoá afterKey (nếu tìm thấy).</summary>
        public void SetAfter(string key, object value, string afterKey)
        {
            if (IndexOf(key) >= 0)
            {
                Set(key, value);
                return;
            }
            int after = IndexOf(afterKey);
            var kv = new KeyValuePair<string, object>(key, value);
            if (after >= 0)
                _items.Insert(after + 1, kv);
            else
                _items.Add(kv);
        }

        public bool Remove(string key)
        {
            int i = IndexOf(key);
            if (i < 0)
                return false;
            _items.RemoveAt(i);
            return true;
        }

        public bool ContainsKey(string key) => IndexOf(key) >= 0;

        private int IndexOf(string key)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (string.Equals(_items[i].Key, key, StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }
    }

    /// <summary>Số JSON giữ nguyên chuỗi gốc (không đổi 90 thành 90.0).</summary>
    public sealed class JsonNumber
    {
        public JsonNumber(string raw) { Raw = raw; }
        public string Raw { get; }
        public override string ToString() => Raw;
    }

    /// <summary>
    /// Bộ đọc/ghi JSON tối giản, chạy cả net48 và net10 không cần thư viện ngoài.
    /// Giá trị: JsonObject, List&lt;object&gt;, string, bool, JsonNumber, null.
    /// </summary>
    public static class JsonLite
    {
        public static object Parse(string text)
        {
            var p = new Parser(text ?? string.Empty);
            p.SkipWs();
            object v = p.ReadValue();
            p.SkipWs();
            if (!p.AtEnd)
                throw new FormatException("JSON thừa ký tự ở vị trí " + p.Pos);
            return v;
        }

        public static string Serialize(object value)
        {
            var sb = new StringBuilder();
            Write(sb, value, 0);
            sb.Append('\n');
            return sb.ToString();
        }

        private static void Write(StringBuilder sb, object v, int indent)
        {
            switch (v)
            {
                case null:
                    sb.Append("null");
                    break;
                case string s:
                    WriteString(sb, s);
                    break;
                case bool b:
                    sb.Append(b ? "true" : "false");
                    break;
                case JsonNumber n:
                    sb.Append(n.Raw);
                    break;
                case int i:
                    sb.Append(i.ToString(CultureInfo.InvariantCulture));
                    break;
                case double d:
                    sb.Append(d.ToString("R", CultureInfo.InvariantCulture));
                    break;
                case JsonObject o:
                    if (o.Items.Count == 0)
                    {
                        sb.Append("{}");
                        break;
                    }
                    sb.Append("{\n");
                    for (int k = 0; k < o.Items.Count; k++)
                    {
                        Indent(sb, indent + 1);
                        WriteString(sb, o.Items[k].Key);
                        sb.Append(": ");
                        Write(sb, o.Items[k].Value, indent + 1);
                        if (k < o.Items.Count - 1)
                            sb.Append(',');
                        sb.Append('\n');
                    }
                    Indent(sb, indent);
                    sb.Append('}');
                    break;
                case List<object> list:
                    if (list.Count == 0)
                    {
                        sb.Append("[]");
                        break;
                    }
                    sb.Append("[\n");
                    for (int k = 0; k < list.Count; k++)
                    {
                        Indent(sb, indent + 1);
                        Write(sb, list[k], indent + 1);
                        if (k < list.Count - 1)
                            sb.Append(',');
                        sb.Append('\n');
                    }
                    Indent(sb, indent);
                    sb.Append(']');
                    break;
                default:
                    WriteString(sb, Convert.ToString(v, CultureInfo.InvariantCulture));
                    break;
            }
        }

        private static void Indent(StringBuilder sb, int level) => sb.Append(' ', level * 2);

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        private sealed class Parser
        {
            private readonly string _s;
            public int Pos;

            public Parser(string s) { _s = s; }

            public bool AtEnd => Pos >= _s.Length;

            public void SkipWs()
            {
                while (Pos < _s.Length && char.IsWhiteSpace(_s[Pos]))
                    Pos++;
            }

            public object ReadValue()
            {
                if (AtEnd)
                    throw new FormatException("JSON kết thúc đột ngột");
                char c = _s[Pos];
                if (c == '{') return ReadObject();
                if (c == '[') return ReadArray();
                if (c == '"') return ReadString();
                if (Match("true")) return true;
                if (Match("false")) return false;
                if (Match("null")) return null;
                return ReadNumber();
            }

            private bool Match(string word)
            {
                if (string.CompareOrdinal(_s, Pos, word, 0, word.Length) == 0)
                {
                    Pos += word.Length;
                    return true;
                }
                return false;
            }

            private JsonObject ReadObject()
            {
                var o = new JsonObject();
                Pos++; // {
                SkipWs();
                if (Peek('}')) { Pos++; return o; }
                while (true)
                {
                    SkipWs();
                    if (AtEnd || _s[Pos] != '"')
                        throw new FormatException("Thiếu tên khoá ở vị trí " + Pos);
                    string key = ReadString();
                    SkipWs();
                    Expect(':');
                    SkipWs();
                    o.Set(key, ReadValue());
                    SkipWs();
                    if (Peek(',')) { Pos++; continue; }
                    Expect('}');
                    return o;
                }
            }

            private List<object> ReadArray()
            {
                var list = new List<object>();
                Pos++; // [
                SkipWs();
                if (Peek(']')) { Pos++; return list; }
                while (true)
                {
                    SkipWs();
                    list.Add(ReadValue());
                    SkipWs();
                    if (Peek(',')) { Pos++; continue; }
                    Expect(']');
                    return list;
                }
            }

            private string ReadString()
            {
                Pos++; // "
                var sb = new StringBuilder();
                while (true)
                {
                    if (AtEnd)
                        throw new FormatException("Chuỗi JSON chưa đóng");
                    char c = _s[Pos++];
                    if (c == '"')
                        return sb.ToString();
                    if (c != '\\')
                    {
                        sb.Append(c);
                        continue;
                    }
                    if (AtEnd)
                        throw new FormatException("Chuỗi JSON chưa đóng");
                    char e = _s[Pos++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (Pos + 4 > _s.Length)
                                throw new FormatException("Mã \\u không đủ 4 ký tự");
                            sb.Append((char)int.Parse(_s.Substring(Pos, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            Pos += 4;
                            break;
                        default:
                            throw new FormatException("Ký tự thoát lạ \\" + e);
                    }
                }
            }

            private JsonNumber ReadNumber()
            {
                int start = Pos;
                while (Pos < _s.Length && "+-0123456789.eE".IndexOf(_s[Pos]) >= 0)
                    Pos++;
                if (Pos == start)
                    throw new FormatException("Giá trị JSON lạ ở vị trí " + start);
                return new JsonNumber(_s.Substring(start, Pos - start));
            }

            private bool Peek(char c) => Pos < _s.Length && _s[Pos] == c;

            private void Expect(char c)
            {
                if (!Peek(c))
                    throw new FormatException("Thiếu '" + c + "' ở vị trí " + Pos);
                Pos++;
            }
        }
    }
}
