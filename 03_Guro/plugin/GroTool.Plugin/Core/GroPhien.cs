using System;
using System.Collections.Generic;

namespace ZwCadPlugin.Core
{
    /// <summary>
    /// Mục 171 (GĐ 0): PHIÊN của một lần chạy GRO — dữ liệu các bước C# để lại cho bước sau
    /// (ghi chú, tấm, khung…) nằm trong C# thay vì đi vòng qua lisp bằng chuỗi (bài học mục 170:
    /// toạ độ qua <c>rtos</c> mất chính xác). Lisp chỉ trao handle / mã.
    ///
    /// Mỗi bản vẽ một phiên (khoá = tên / đường dẫn document). <c>(gro_net_phien_moi)</c> ở đầu
    /// <c>c:gro</c> xoá phiên cũ. Kiểu dữ liệu từng mục do bước tạo ra tự định nghĩa (Core).
    /// </summary>
    public sealed class GroPhien
    {
        private static readonly Dictionary<string, GroPhien> _ds = new Dictionary<string, GroPhien>(StringComparer.OrdinalIgnoreCase);

        public string Khoa { get; }
        public DateTime BatDau { get; }
        private readonly Dictionary<string, object> _du = new Dictionary<string, object>(StringComparer.Ordinal);

        /// <summary>Dòng nhật ký so A/B của lần chạy (in lại cuối lệnh nếu cần).</summary>
        public List<string> NhatKyAB { get; } = new List<string>();

        private GroPhien(string khoa) { Khoa = khoa; BatDau = DateTime.Now; }

        /// <summary>Tạo phiên MỚI cho bản vẽ (xoá phiên cũ).</summary>
        public static GroPhien Moi(string khoa)
        {
            var p = new GroPhien(khoa ?? "");
            _ds[khoa ?? ""] = p;
            return p;
        }

        /// <summary>Phiên hiện tại của bản vẽ; chưa có thì tạo mới.</summary>
        public static GroPhien Lay(string khoa)
        {
            return _ds.TryGetValue(khoa ?? "", out GroPhien p) ? p : Moi(khoa);
        }

        public void Dat<T>(string ten, T giaTri) => _du[ten] = giaTri;

        public bool Co(string ten) => _du.ContainsKey(ten);

        public T Doc<T>(string ten) where T : class =>
            _du.TryGetValue(ten, out object o) ? o as T : null;
    }
}
