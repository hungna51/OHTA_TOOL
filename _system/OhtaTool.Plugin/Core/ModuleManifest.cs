using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace OhtaTool.Plugin.Core
{
    /// <summary>
    /// Cấu hình của một thư mục mục con (tương ứng 1 Tab dọc trên bảng OHTA Tool Palettes).
    /// Thuần C#, không phụ thuộc SDK CAD để kiểm thử độc lập 100%.
    /// </summary>
    [DataContract]
    public sealed class ModuleManifest
    {
        [DataMember(Name = "id", IsRequired = false)]
        public string Id { get; set; }

        [DataMember(Name = "enabled", IsRequired = false)]
        public bool? Enabled { get; set; }

        /// <summary>Nếu true: chỉ hiện tab này trên các máy Admin (có tên máy trong tab "admin" của Google Sheet).</summary>
        [DataMember(Name = "adminOnly", IsRequired = false)]
        public bool? AdminOnly { get; set; }

        [DataMember(Name = "order", IsRequired = false)]
        public int? Order { get; set; }

        [DataMember(Name = "tabTitle", IsRequired = false)]
        public string TabTitle { get; set; }

        [DataMember(Name = "title", IsRequired = false)]
        public string Title { get; set; }

        [DataMember(Name = "description", IsRequired = false)]
        public string Description { get; set; }

        /// <summary>Phiên bản riêng của tab (vd "1.5"). Sửa tab nào thì tăng version tab đó. Hiện ở tab Info + đầu tab.</summary>
        [DataMember(Name = "version", IsRequired = false)]
        public string Version { get; set; }

        /// <summary>Giai đoạn tuỳ chọn (vd "beta") hiện sau phiên bản: "v1.1 (beta)".</summary>
        [DataMember(Name = "stage", IsRequired = false)]
        public string Stage { get; set; }

        [DataMember(Name = "accentColor", IsRequired = false)]
        public string AccentColor { get; set; }

        [DataMember(Name = "startupLisp", IsRequired = false)]
        public List<string> StartupLisp { get; set; }

        [DataMember(Name = "tools", IsRequired = false)]
        public List<ToolItem> Tools { get; set; }

        /// <summary>Thứ tự các nhóm (tên nhóm = ToolItem.Category). Xem ADR 0002.</summary>
        [DataMember(Name = "groups", IsRequired = false)]
        public List<string> Groups { get; set; }

        // Thuộc tính runtime do ModuleDiscovery gán khi quét thư mục
        public string FolderName { get; set; }
        public string DirectoryPath { get; set; }
        public string ReadmePath { get; set; }
        public string RulesDirectoryPath { get; set; }
        public bool HasManifestFile { get; set; }
        public string LoadWarning { get; set; }

        public bool IsEnabled => Enabled ?? true;
        public bool IsAdminOnly => AdminOnly ?? false;
        public int EffectiveOrder => Order ?? 1000;

        /// <summary>"v1.1 (beta)", "v1.5"; chưa khai báo version -> "".</summary>
        public string VersionLabel
        {
            get
            {
                string v = (Version ?? string.Empty).Trim();
                if (v.Length == 0)
                    return string.Empty;
                if (!v.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                    v = "v" + v;
                string stage = (Stage ?? string.Empty).Trim();
                return stage.Length == 0 ? v : v + " (" + stage + ")";
            }
        }

        /// <summary>Tiêu đề đầu tab kèm phiên bản: "GURO · v1.5".</summary>
        public string DisplayTitle
        {
            get
            {
                string label = VersionLabel;
                string title = (Title ?? string.Empty).Trim();
                if (label.Length == 0)
                    return title;
                return title.Length == 0 ? label : title + " · " + label;
            }
        }
    }

    /// <summary>
    /// Thông tin một nút lệnh hiển thị bên trong Tab của mục con.
    /// </summary>
    [DataContract]
    public sealed class ToolItem
    {
        [DataMember(Name = "id", IsRequired = false)]
        public string Id { get; set; }

        [DataMember(Name = "category", IsRequired = false)]
        public string Category { get; set; }

        [DataMember(Name = "name", IsRequired = false)]
        public string Name { get; set; }

        [DataMember(Name = "badge", IsRequired = false)]
        public string Badge { get; set; }

        [DataMember(Name = "command", IsRequired = false)]
        public string Command { get; set; }

        [DataMember(Name = "lispFile", IsRequired = false)]
        public string LispFile { get; set; }

        [DataMember(Name = "description", IsRequired = false)]
        public string Description { get; set; }

        /// <summary>Ảnh nút (.png/.ico), đường dẫn tương đối trong thư mục tab. Trống → dùng Badge.</summary>
        [DataMember(Name = "icon", IsRequired = false)]
        public string Icon { get; set; }

        /// <summary>Nút gốc do lập trình viên khai báo: giao diện không cho xoá.</summary>
        [DataMember(Name = "locked", IsRequired = false)]
        public bool? Locked { get; set; }

        public bool IsLocked => Locked ?? false;
    }
}

