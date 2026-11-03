using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OhtaTool.Plugin.Core
{
    /// <summary>
    /// Sửa module.json từ giao diện (ADR 0002): luôn đọc lại file mới nhất, chỉ chèn phần thay đổi,
    /// giữ nguyên trường lạ, ghi qua file tạm và giữ module.json.bak.
    /// Các hàm thao tác trên JsonObject thuần, không đụng đĩa, để kiểm thử độc lập.
    /// </summary>
    public static class ModuleJsonEditor
    {
        public const string FileName = "module.json";

        public static string ManifestPath(string moduleDir) => Path.Combine(moduleDir, FileName);

        // ------------------------------------------------------------------
        // Đọc / ghi đĩa
        // ------------------------------------------------------------------

        public static JsonObject Load(string moduleDir)
        {
            string path = ManifestPath(moduleDir);
            if (!File.Exists(path))
                return new JsonObject();

            byte[] bytes = File.ReadAllBytes(path);
            int offset = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
            string text = Encoding.UTF8.GetString(bytes, offset, bytes.Length - offset);
            if (string.IsNullOrWhiteSpace(text))
                return new JsonObject();

            if (!(JsonLite.Parse(text) is JsonObject root))
                throw new FormatException("module.json không phải một đối tượng JSON { }.");
            return root;
        }

        public static void Save(string moduleDir, JsonObject root)
        {
            string path = ManifestPath(moduleDir);
            string tmp = path + ".tmp";
            string bak = path + ".bak";
            File.WriteAllText(tmp, JsonLite.Serialize(root), new UTF8Encoding(false));

            if (!File.Exists(path))
            {
                File.Move(tmp, path);
                return;
            }

            try
            {
                File.Replace(tmp, path, bak);
            }
            catch
            {
                // File.Replace có thể lỗi trên OneDrive/ổ mạng -> sao lưu rồi chép đè
                File.Copy(path, bak, true);
                File.Copy(tmp, path, true);
                File.Delete(tmp);
            }
        }

        /// <summary>Đọc lại module.json mới nhất, áp thay đổi, ghi lại. Trả về kết quả của mutate.</summary>
        public static T Update<T>(string moduleDir, Func<JsonObject, T> mutate)
        {
            JsonObject root = Load(moduleDir);
            T result = mutate(root);
            Save(moduleDir, root);
            return result;
        }

        // ------------------------------------------------------------------
        // Nút lệnh
        // ------------------------------------------------------------------

        public static List<object> GetList(JsonObject root, string key)
        {
            if (root.Get(key) is List<object> list)
                return list;
            var created = new List<object>();
            root.Set(key, created);
            return created;
        }

        public static IEnumerable<JsonObject> Tools(JsonObject root) => GetList(root, "tools").OfType<JsonObject>();

        public static JsonObject FindTool(JsonObject root, string toolId) =>
            Tools(root).FirstOrDefault(t => string.Equals(t.GetString("id"), toolId, StringComparison.OrdinalIgnoreCase));

        public static string MakeUniqueToolId(JsonObject root, string command)
        {
            string baseId = "u_" + Regex.Replace((command ?? "tool").ToLowerInvariant(), "[^a-z0-9_]", "_");
            var used = new HashSet<string>(Tools(root).Select(t => t.GetString("id") ?? string.Empty), StringComparer.OrdinalIgnoreCase);
            string id = baseId;
            for (int i = 2; used.Contains(id); i++)
                id = baseId + "_" + i;
            return id;
        }

        /// <summary>Thêm nút mới vào cuối danh sách; tự sinh id nếu trống. Trả về id.</summary>
        public static string AddTool(JsonObject root, ToolItem tool)
        {
            if (string.IsNullOrWhiteSpace(tool.Id))
                tool.Id = MakeUniqueToolId(root, tool.Command);

            var obj = new JsonObject();
            obj.Set("id", tool.Id);
            WriteToolFields(obj, tool);
            GetList(root, "tools").Add(obj);
            EnsureGroupListed(root, tool.Category);
            return tool.Id;
        }

        /// <summary>Sửa các trường của nút (giữ nguyên trường lạ và cờ locked).</summary>
        public static bool UpdateTool(JsonObject root, string toolId, ToolItem values)
        {
            JsonObject obj = FindTool(root, toolId);
            if (obj == null)
                return false;
            WriteToolFields(obj, values);
            EnsureGroupListed(root, values.Category);
            return true;
        }

        /// <summary>Gỡ nút khỏi module.json (không xoá file lisp). Nút locked không gỡ được.</summary>
        public static bool RemoveTool(JsonObject root, string toolId)
        {
            JsonObject obj = FindTool(root, toolId);
            if (obj == null || IsLocked(obj))
                return false;
            return GetList(root, "tools").Remove(obj);
        }

        public static bool SetToolCategory(JsonObject root, string toolId, string category)
        {
            JsonObject obj = FindTool(root, toolId);
            if (obj == null)
                return false;
            SetOptional(obj, "category", NormalizeCategory(category), "id");
            EnsureGroupListed(root, category);
            return true;
        }

        /// <summary>Đổi chỗ nút với nút liền trước (delta=-1) hoặc liền sau (+1) trong cùng nhóm.</summary>
        public static bool MoveToolInGroup(JsonObject root, string toolId, int delta)
        {
            List<object> list = GetList(root, "tools");
            JsonObject obj = FindTool(root, toolId);
            if (obj == null || delta == 0)
                return false;

            string group = NormalizeCategory(obj.GetString("category"));
            int i = list.IndexOf(obj);
            int step = delta < 0 ? -1 : 1;
            for (int j = i + step; j >= 0 && j < list.Count; j += step)
            {
                if (list[j] is JsonObject other &&
                    string.Equals(NormalizeCategory(other.GetString("category")), group, StringComparison.OrdinalIgnoreCase))
                {
                    list[i] = other;
                    list[j] = obj;
                    return true;
                }
            }
            return false;
        }

        public static void AddStartupLisp(JsonObject root, string relPath)
        {
            if (string.IsNullOrWhiteSpace(relPath))
                return;
            if (!root.ContainsKey("startupLisp"))
                root.SetAfter("startupLisp", new List<object>(), "accentColor");
            List<object> list = GetList(root, "startupLisp");
            if (!list.OfType<string>().Any(s => SamePath(s, relPath)))
                list.Add(relPath);
        }

        // ------------------------------------------------------------------
        // Nhóm & header
        // ------------------------------------------------------------------

        public static List<string> GetGroups(JsonObject root) =>
            root.Get("groups") is List<object> l ? l.OfType<string>().ToList() : new List<string>();

        public static void SetGroups(JsonObject root, IEnumerable<string> groups)
        {
            var list = new List<object>();
            foreach (string g in groups)
            {
                string name = NormalizeCategory(g);
                if (name.Length > 0 && !list.OfType<string>().Contains(name, StringComparer.OrdinalIgnoreCase))
                    list.Add(name);
            }
            root.SetAfter("groups", list, "startupLisp");
        }

        /// <summary>Đổi tên nhóm: sửa cả danh sách groups và category của các nút.</summary>
        public static void RenameGroup(JsonObject root, string oldName, string newName)
        {
            string from = NormalizeCategory(oldName);
            string to = NormalizeCategory(newName);
            if (from.Length == 0 || to.Length == 0)
                return;

            foreach (JsonObject t in Tools(root))
            {
                if (string.Equals(NormalizeCategory(t.GetString("category")), from, StringComparison.OrdinalIgnoreCase))
                    t.Set("category", to);
            }
            SetGroups(root, GetGroups(root).Select(g => string.Equals(g, from, StringComparison.OrdinalIgnoreCase) ? to : g));
        }

        /// <summary>Xoá nhóm: các nút trong nhóm chuyển về "Khác" (bỏ category).</summary>
        public static void DeleteGroup(JsonObject root, string name)
        {
            string target = NormalizeCategory(name);
            foreach (JsonObject t in Tools(root))
            {
                if (string.Equals(NormalizeCategory(t.GetString("category")), target, StringComparison.OrdinalIgnoreCase))
                    t.Remove("category");
            }
            SetGroups(root, GetGroups(root).Where(g => !string.Equals(g, target, StringComparison.OrdinalIgnoreCase)));
        }

        public static void SetHeader(JsonObject root, string title, string description, string accentColor)
        {
            if (title != null) root.Set("title", title.Trim());
            if (description != null) root.Set("description", description.Trim());
            if (accentColor != null) root.Set("accentColor", accentColor.Trim());
        }

        // ------------------------------------------------------------------
        // Trùng lệnh
        // ------------------------------------------------------------------

        /// <summary>
        /// Tìm tab đang dùng lệnh này (trên mọi tab). Bỏ qua chính nút đang sửa (exceptModuleDir + exceptToolId).
        /// Trả về tên tab, hoặc null nếu không trùng.
        /// </summary>
        public static string FindCommandOwner(IEnumerable<ModuleManifest> modules, string command, string exceptModuleDir = null, string exceptToolId = null)
        {
            string cmd = (command ?? string.Empty).Trim();
            if (cmd.Length == 0 || modules == null)
                return null;

            foreach (ModuleManifest m in modules)
            {
                if (m?.Tools == null)
                    continue;
                bool sameModule = exceptModuleDir != null && SamePath(m.DirectoryPath, exceptModuleDir);
                foreach (ToolItem t in m.Tools)
                {
                    if (sameModule && exceptToolId != null && string.Equals(t.Id, exceptToolId, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (string.Equals((t.Command ?? string.Empty).Trim(), cmd, StringComparison.OrdinalIgnoreCase))
                        return string.IsNullOrWhiteSpace(m.TabTitle) ? m.FolderName : m.TabTitle;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------
        // Nội bộ
        // ------------------------------------------------------------------

        private static void WriteToolFields(JsonObject obj, ToolItem tool)
        {
            SetOptional(obj, "category", NormalizeCategory(tool.Category), "id");
            obj.SetAfter("name", (tool.Name ?? string.Empty).Trim(), "category");
            SetOptional(obj, "badge", (tool.Badge ?? string.Empty).Trim().ToUpperInvariant(), "name");
            obj.SetAfter("command", (tool.Command ?? string.Empty).Trim(), "badge");
            SetOptional(obj, "lispFile", (tool.LispFile ?? string.Empty).Trim().Replace('\\', '/'), "command");
            SetOptional(obj, "icon", (tool.Icon ?? string.Empty).Trim().Replace('\\', '/'), "lispFile");
            obj.Set("description", (tool.Description ?? string.Empty).Trim());
        }

        private static void SetOptional(JsonObject obj, string key, string value, string afterKey)
        {
            if (string.IsNullOrEmpty(value))
                obj.Remove(key);
            else
                obj.SetAfter(key, value, afterKey);
        }

        private static void EnsureGroupListed(JsonObject root, string category)
        {
            string name = NormalizeCategory(category);
            if (name.Length == 0)
                return;
            List<string> groups = GetGroups(root);
            if (groups.Contains(name, StringComparer.OrdinalIgnoreCase))
                return;
            // Lần đầu có danh sách groups: giữ thứ tự các nhóm đang hiện
            if (!root.ContainsKey("groups"))
                groups = ModuleGrouping.GroupNames(ToolItemsOf(root), null).Where(g => !string.Equals(g, name, StringComparison.OrdinalIgnoreCase)).ToList();
            groups.Add(name);
            SetGroups(root, groups);
        }

        private static IEnumerable<ToolItem> ToolItemsOf(JsonObject root) =>
            Tools(root).Select(t => new ToolItem { Id = t.GetString("id"), Category = t.GetString("category") });

        private static bool IsLocked(JsonObject obj) => obj.Get("locked") is bool b && b;

        private static string NormalizeCategory(string category)
        {
            string c = (category ?? string.Empty).Trim();
            return ModuleGrouping.IsUngroupedName(c) ? string.Empty : c;
        }

        private static bool SamePath(string a, string b)
        {
            if (a == null || b == null)
                return false;
            string na = a.Replace('\\', '/').TrimEnd('/');
            string nb = b.Replace('\\', '/').TrimEnd('/');
            return string.Equals(na, nb, StringComparison.OrdinalIgnoreCase);
        }
    }
}
