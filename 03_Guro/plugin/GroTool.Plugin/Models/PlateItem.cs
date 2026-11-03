using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwCadPlugin.Core;

namespace ZwCadPlugin.Models
{
    /// <summary>
    /// Một dòng của bảng DST = một tấm. Cột hiển thị theo ĐÚNG thứ tự CSV của GRO:
    /// Bango, Q, M, T, GC, Ghi chú.
    /// </summary>
    public class PlateItem : INotifyPropertyChanged
    {
        private string _bango = string.Empty;
        private string _thickness = string.Empty;
        private string _quantity = string.Empty;
        private string _material = string.Empty;
        private string _gc = string.Empty;
        private string _status = string.Empty;
        private bool _isOrphan;
        private bool _isModified;
        private string _canhBao;

        private string _role = string.Empty;
        private string _primaryBango = string.Empty;
        private string _customerNote = string.Empty;
        private bool _noCuttingShape;

        public string Bango
        {
            get => _bango;
            set { if (_bango != value) { _bango = value; OnPropertyChanged(); CapNhatDaSua(); } }
        }

        /// <summary>Cột Q.</summary>
        public string Quantity
        {
            get => _quantity;
            set { if (_quantity != value) { _quantity = value; OnPropertyChanged(); CapNhatDaSua(); } }
        }

        /// <summary>Cột M.</summary>
        public string Material
        {
            get => _material;
            set { if (_material != value) { _material = value; OnPropertyChanged(); CapNhatDaSua(); } }
        }

        /// <summary>Cột T.</summary>
        public string Thickness
        {
            get => _thickness;
            set { if (_thickness != value) { _thickness = value; OnPropertyChanged(); CapNhatDaSua(); } }
        }

        /// <summary>
        /// Cột GC (gia công) — đúng cột V của CSV: "" tấm đơn, "m" bẻ, "k" vát, "*" từ 2 loại gia công khác nhau trở lên.
        /// m, k, * do GRO tự sinh; s, t, r, * người dùng có thể nhập trực tiếp trong bảng.
        /// </summary>
        public string GC
        {
            get => _gc;
            set { if (_gc != value) { _gc = value; OnPropertyChanged(); CapNhatDaSua(); } }
        }

        /// <summary>
        /// Vai trò cặp đối xứng: "HONZU" (Bản chính), "GYAKUZU" (Bản nghịch), hoặc "" (tấm đơn độc lập).
        /// </summary>
        public string Role
        {
            get => _role;
            set
            {
                if (_role != value)
                {
                    _role = value ?? string.Empty;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Note));
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        /// <summary>
        /// Mã bango của bản chính (khi tấm này là bản nghịch Gyakuzu).
        /// </summary>
        public string PrimaryBango
        {
            get => _primaryBango;
            set
            {
                if (_primaryBango != value)
                {
                    _primaryBango = value ?? string.Empty;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Note));
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        /// <summary>
        /// Ghi chú trích xuất từ text của khách (đã qua từ điển dịch JapanNoteTranslator hoặc giữ nguyên tiếng Nhật).
        /// </summary>
        public string CustomerNote
        {
            get => _customerNote;
            set
            {
                if (_customerNote != value)
                {
                    _customerNote = value ?? string.Empty;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LaXuLySht));
                    TinhLoi();
                }
            }
        }

        /// <summary>Tấm có ghi chú yêu cầu xử lý đổi nét sang layer 4-SHT.</summary>
        public bool LaXuLySht => JapanNoteTranslator.LaXuLySht(_customerNote);

        private string _ghiChuThem = string.Empty;

        /// <summary>Ghi chú thường (không phải cảnh báo) thêm vào cột Ghi chú — vd "Mã khắc thu nhỏ 1/2" (mục 117).</summary>
        public string GhiChuThem
        {
            get => _ghiChuThem;
            set
            {
                if (_ghiChuThem != value)
                {
                    _ghiChuThem = value ?? string.Empty;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Note));
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        /// <summary>
        /// Cờ báo tấm ghi chú không tìm thấy hình cắt tương ứng trong bản vẽ (tính là lỗi đỏ MucDo = 2).
        /// </summary>
        public bool NoCuttingShape
        {
            get => _noCuttingShape;
            set
            {
                if (_noCuttingShape != value)
                {
                    _noCuttingShape = value;
                    OnPropertyChanged();
                    TinhLoi();
                }
            }
        }

        // Gia tri LUC QUET. Dung de (1) biet o nao THAT SU bi sua - chi o do moi ghi
        // vao ban ve / CSV, o khac giu nguyen tung ky tu; (2) tim dong CSV theo bango cu.
        public string GocBango { get; private set; }
        public string GocQuantity { get; private set; }
        public string GocMaterial { get; private set; }
        public string GocThickness { get; private set; }

        public bool DaSuaBango => !Bang(_bango, GocBango);
        public bool DaSuaQuantity => !Bang(_quantity, GocQuantity);
        public bool DaSuaMaterial => !Bang(_material, GocMaterial);
        public bool DaSuaThickness => !Bang(_thickness, GocThickness);
        public string GocGC { get; private set; }
        /// <summary>GC chỉ ghi vào CSV (bản vẽ không lưu GC).</summary>
        public bool DaSuaGC => !string.Equals(CsvGro.ChuanHoaGC(_gc), CsvGro.ChuanHoaGC(GocGC), StringComparison.Ordinal);

        private static bool Bang(string a, string b) =>
            string.Equals((a ?? string.Empty).Trim(), (b ?? string.Empty).Trim(), StringComparison.Ordinal);

        /// <summary>Sửa rồi gõ lại đúng giá trị cũ thì dòng trở về "khớp".</summary>
        private void CapNhatDaSua()
        {
            IsModified = DaSuaBango || DaSuaQuantity || DaSuaMaterial || DaSuaThickness || DaSuaGC;
            TinhLoi();
        }

        // ---------------- Loi du lieu (to noi bat) ----------------

        private string _loi;
        private bool _trungBango;

        /// <summary>Q / T của dòng CSV khớp bango (null = không có CSV / không có dòng).</summary>
        public string CsvQ { get; set; }
        public string CsvT { get; set; }

        /// <summary>Bango trùng với một tấm khác — ViewModel tính lại mỗi khi có bango đổi.</summary>
        public bool TrungBango
        {
            get => _trungBango;
            set { if (_trungBango != value) { _trungBango = value; TinhLoi(); } }
        }

        /// <summary>Các lỗi dữ liệu, nối bằng "; ". Null = không lỗi.</summary>
        public string Loi => _loi;

        /// <summary>0 = khớp, 1 = cảnh báo (vàng), 2 = LỖI hoặc hình cô đơn / không tìm thấy hình cắt (đỏ) — dùng tô màu dòng.</summary>
        public int MucDo => _isOrphan || _noCuttingShape || !string.IsNullOrEmpty(_loi) ? 2
                          : _isModified || !string.IsNullOrEmpty(_canhBao) || LaXuLySht ? 1 : 0;

        /// <summary>
        /// Tính lại lỗi theo giá trị ĐANG hiển thị — sửa đúng là hết đỏ ngay, không cần lưu.
        /// Ô nào đang sửa thì không so với CSV nữa: lưu xong CSV sẽ được ghi theo.
        /// </summary>
        private void TinhLoi()
        {
            string moi = null;
            if (!_isOrphan)
            {
                var ds = KiemTraTam.Loi(_quantity, _thickness, _material,
                                        DaSuaQuantity ? null : CsvQ,
                                        DaSuaThickness ? null : CsvT);
                if (_trungBango) ds.Insert(0, "Trùng bango");
                if (ds.Count > 0) moi = string.Join("; ", ds);
            }
            if (moi != _loi)
            {
                _loi = moi;
                OnPropertyChanged(nameof(Loi));
            }
            _status = TinhGhiChu();
            OnPropertyChanged(nameof(Note));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(MucDo));
            OnPropertyChanged(nameof(StatusColor));
        }

        /// <summary>
        /// Nội dung hiển thị tại cột "Ghi chú":
        /// - Bản chính / Bản nghịch - &lt;bango&gt;
        /// - Ghi chú khách (đã dịch hoặc tiếng Nhật)
        /// - Cảnh báo / Lỗi / Không tìm thấy hình cắt
        /// Khi bình thường, không có ghi chú: chuỗi rỗng "" (xóa chữ "Đã lưu / Khớp").
        /// </summary>
        public string Note => TinhGhiChu();

        public string Status
        {
            get => Note;
            set { if (_status != value) { _status = value; OnPropertyChanged(); } }
        }

        public bool IsOrphan
        {
            get => _isOrphan;
            set
            {
                if (_isOrphan != value)
                {
                    _isOrphan = value;
                    OnPropertyChanged();
                    TinhLoi();
                }
            }
        }

        public bool IsModified
        {
            get => _isModified;
            private set
            {
                if (_isModified != value)
                {
                    _isModified = value;
                    _status = TinhGhiChu();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Note));
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(StatusColor));
                    OnPropertyChanged(nameof(MucDo));
                }
            }
        }

        public string StatusColor
        {
            get
            {
                if (MucDo == 2)
                {
                    if (_isOrphan) return "#E53935"; // Red: hinh co don
                    if (_noCuttingShape) return "#DC2626"; // Red: khong tim thay hinh cat
                    return "#B91C1C"; // Do dam: loi du lieu
                }
                if (MucDo == 1)
                {
                    if (_isModified) return "#FB8C00"; // Orange: da sua chua luu
                    return "#D97706"; // Amber: canh bao luc quet
                }
                return "#43A047"; // Green: binh thuong / khop
            }
        }

        /// <summary>
        /// Điều đáng ngờ phát hiện lúc quét (trùng bango, thiếu mã khắc / F-name, lệch CSV...).
        /// Giữ riêng khỏi Status để không bị "Đã sửa (Chưa lưu)" ghi đè mất.
        /// </summary>
        public string CanhBao
        {
            get => _canhBao;
            set
            {
                if (_canhBao != value)
                {
                    _canhBao = value;
                    _status = TinhGhiChu();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Note));
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(StatusColor));
                    OnPropertyChanged(nameof(MucDo));
                }
            }
        }

        /// <summary>
        /// Mục 159: hiện dòng "Bản chính / Bản nghịch - N" trong cột Ghi chú (chung cho mọi dòng).
        /// Chỉ ảnh hưởng hiển thị — vai trò (Role / PrimaryBango) vẫn giữ để ghép cặp, lưu bản vẽ.
        /// </summary>
        public static bool HienVaiTro { get; set; } = true;

        /// <summary>Báo giao diện vẽ lại cột Ghi chú (sau khi đổi <see cref="HienVaiTro"/>).</summary>
        public void LamMoiGhiChu()
        {
            _status = TinhGhiChu();
            OnPropertyChanged(nameof(Note));
            OnPropertyChanged(nameof(Status));
        }

        /// <summary>Thêm một cảnh báo, nối với cảnh báo đã có.</summary>
        public void ThemCanhBao(string s)
        {
            CanhBao = string.IsNullOrEmpty(_canhBao) ? s : _canhBao + "; " + s;
        }

        private string TinhGhiChu()
        {
            var lines = new List<string>();

            // 1. Dòng Ghi chú đặc biệt (cặp đối xứng & ghi chú khách)
            //    Mục 159: tắt được bằng ô tích "Bản chính/nghịch" trên thanh công cụ DST (HienVaiTro).
            if (HienVaiTro && string.Equals(_role, "HONZU", StringComparison.OrdinalIgnoreCase))
            {
                lines.Add("Bản chính");
            }
            else if (HienVaiTro && string.Equals(_role, "GYAKUZU", StringComparison.OrdinalIgnoreCase))
            {
                lines.Add(string.IsNullOrEmpty(_primaryBango) ? "Bản nghịch" : "Bản nghịch - " + _primaryBango);
            }

            if (!string.IsNullOrWhiteSpace(_customerNote))
            {
                lines.Add(_customerNote.Trim());
            }

            if (!string.IsNullOrWhiteSpace(_ghiChuThem))
            {
                lines.Add(_ghiChuThem.Trim());
            }

            // 2. Dòng Lỗi / Cảnh báo / Trạng thái
            if (_isOrphan)
            {
                lines.Add("[!] Hình cô đơn (Thiếu thông tin)");
            }
            else if (_noCuttingShape)
            {
                lines.Add("[!] Không tìm thấy hình cắt");
            }

            if (!string.IsNullOrEmpty(_loi))
            {
                lines.Add("LỖI: " + _loi);
            }

            if (_isModified)
            {
                lines.Add("Đã sửa (Chưa lưu)");
            }

            if (!string.IsNullOrEmpty(_canhBao))
            {
                lines.Add(_canhBao);
            }

            if (LaXuLySht)
            {
                lines.Add("[!] Cần xử lý đổi nét 4-SHT");
            }

            return string.Join("\n", lines);
        }

        /// <summary>
        /// Gọi khi bảng khớp với bản vẽ (sau khi quét / sau khi lưu): chụp lại giá trị hiện
        /// tại làm "giá trị gốc".
        /// </summary>
        public void DanhDauDaKhop()
        {
            GocBango = _bango;
            GocQuantity = _quantity;
            GocMaterial = _material;
            GocThickness = _thickness;
            GocGC = _gc;
            CapNhatDaSua();
            _status = TinhGhiChu();
            OnPropertyChanged(nameof(Note));
            OnPropertyChanged(nameof(Status));
        }

        // CAD Entity References
        public ObjectId FrameId { get; set; } = ObjectId.Null;
        public ObjectId BangoTextId { get; set; } = ObjectId.Null;
        public ObjectId MaKhacTextId { get; set; } = ObjectId.Null;
        public List<ObjectId> FnameTextIds { get; set; } = new List<ObjectId>();
        /// <summary>
        /// Vùng để ZOOM tới: hộp của CHÍNH tấm (TEXT bango + mã khắc + F-name).
        /// </summary>
        public Extents3d BoundingBox { get; set; }

        /// <summary>
        /// Hộp bao của khung Khung_IN (nếu có). Dùng để zoom tới khung in tương ứng của tấm khi người dùng chọn trong bảng DST.
        /// </summary>
        public Extents3d? FrameBoundingBox { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
