using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OhtaTool.Plugin.Core
{
    /// <summary>Một nhóm nút lệnh trong tab (tên nhóm = ToolItem.Category).</summary>
    public sealed class ToolGroup
    {
        public string Name { get; set; }
        public bool IsUngrouped { get; set; }
        public List<ToolItem> Tools { get; } = new List<ToolItem>();
    }

    /// <summary>
    /// Gom nút lệnh thành nhóm (ADR 0002): thứ tự theo manifest.groups, nhóm chưa khai báo xếp sau
    /// theo thứ tự xuất hiện, nút không có category vào nhóm "Khác" ở cuối.
    /// </summary>
    public static class ModuleGrouping
    {
        public const string UngroupedName = "Khác";

        public static List<ToolGroup> BuildGroups(IEnumerable<ToolItem> tools, IEnumerable<string> groupOrder)
        {
            var byName = new Dictionary<string, ToolGroup>(StringComparer.OrdinalIgnoreCase);
            var ordered = new List<ToolGroup>();

            if (groupOrder != null)
            {
                foreach (string g in groupOrder)
                {
                    string name = (g ?? string.Empty).Trim();
                    if (name.Length == 0 || IsUngroupedName(name) || byName.ContainsKey(name))
                        continue;
                    var group = new ToolGroup { Name = name };
                    byName[name] = group;
                    ordered.Add(group);
                }
            }

            var ungrouped = new ToolGroup { Name = UngroupedName, IsUngrouped = true };
            if (tools != null)
            {
                foreach (ToolItem tool in tools)
                {
                    if (tool == null)
                        continue;
                    string cat = (tool.Category ?? string.Empty).Trim();
                    if (cat.Length == 0 || IsUngroupedName(cat))
                    {
                        ungrouped.Tools.Add(tool);
                        continue;
                    }

                    if (!byName.TryGetValue(cat, out ToolGroup group))
                    {
                        group = new ToolGroup { Name = cat };
                        byName[cat] = group;
                        ordered.Add(group);
                    }
                    group.Tools.Add(tool);
                }
            }

            // Nhóm đã khai báo nhưng chưa có nút vẫn giữ (để chọn khi thêm lệnh), "Khác" chỉ hiện khi có nút
            if (ungrouped.Tools.Count > 0)
                ordered.Add(ungrouped);
            return ordered;
        }

        /// <summary>Tên các nhóm theo đúng thứ tự hiển thị (không gồm "Khác").</summary>
        public static List<string> GroupNames(IEnumerable<ToolItem> tools, IEnumerable<string> groupOrder)
        {
            return BuildGroups(tools, groupOrder).Where(g => !g.IsUngrouped).Select(g => g.Name).ToList();
        }

        public static bool IsUngroupedName(string name) =>
            string.Equals((name ?? string.Empty).Trim(), UngroupedName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Lưu nhóm nào đang thu gọn — riêng từng máy, không ghi vào module.json.
    /// Mỗi dòng: moduleId|tên nhóm.
    /// </summary>
    public sealed class GroupCollapseStore
    {
        private readonly string _filePath;
        private readonly HashSet<string> _collapsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public GroupCollapseStore(string filePath)
        {
            _filePath = filePath;
            try
            {
                if (!string.IsNullOrEmpty(_filePath) && File.Exists(_filePath))
                {
                    foreach (string line in File.ReadAllLines(_filePath, Encoding.UTF8))
                    {
                        if (line.IndexOf('|') > 0)
                            _collapsed.Add(line.Trim());
                    }
                }
            }
            catch
            {
                // File hỏng/khoá: coi như chưa thu gọn nhóm nào
            }
        }

        public static string DefaultPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OHTA_TOOL", "ui_state.txt");

        public bool IsCollapsed(string moduleId, string group) => _collapsed.Contains(Key(moduleId, group));

        public void SetCollapsed(string moduleId, string group, bool collapsed)
        {
            string key = Key(moduleId, group);
            bool changed = collapsed ? _collapsed.Add(key) : _collapsed.Remove(key);
            if (!changed || string.IsNullOrEmpty(_filePath))
                return;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
                File.WriteAllLines(_filePath, _collapsed.OrderBy(s => s, StringComparer.OrdinalIgnoreCase), new UTF8Encoding(false));
            }
            catch
            {
                // Không lưu được thì chỉ mất trạng thái thu gọn, không ảnh hưởng lệnh
            }
        }

        private static string Key(string moduleId, string group) =>
            (moduleId ?? string.Empty).Trim() + "|" + (group ?? string.Empty).Trim();
    }
}
