using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using OhtaTool.Plugin.Core;

namespace OhtaTool.Plugin.Views
{
    /// <summary>
    /// Cửa sổ bổ sung 1 góc bẻ thử (đo thực tế) vào bảng trav_thu, gửi vào hàng chờ duyệt trên Google Sheet.
    /// Góc nội suy không nhập ở đây — tool tự tính và tự gửi khi đặt / cập nhật Fname.
    /// </summary>
    public partial class FnameTrialBendWindow : Window
    {
        public delegate Task<(bool ok, string message)> SubmitHandler(TraVThuRecord record, string ghiChu);

        private readonly SubmitHandler _submit;

        public FnameTrialBendWindow(string initialM, string initialT, string initialAngle, SubmitHandler submit)
        {
            InitializeComponent();
            _submit = submit;

            var logo = IconLoader.GetSettingsLogo();
            if (logo != null)
                Icon = logo;

            foreach (string l in TraVCalculator.GetTrialMaterialTypeOptions())
                CmbLoai.Items.Add(l);
            foreach (string d in TraVCalculator.GetDieOptions())
                CmbDao.Items.Add(d);
            CmbDinhDanh.Items.Add(TraVThuRecord.BeThu);
            CmbDinhDanh.Items.Add(TraVThuRecord.NoiSuy);
            CmbDinhDanh.SelectedIndex = 0;

            // Mác 縞板 cho 2 loại (SIMA Trong / Ngoài) -> lấy loại đầu, người dùng đổi nếu cần
            CmbLoai.Text = string.IsNullOrWhiteSpace(initialM) ? string.Empty : TraVCalculator.GetTraVMaterialKeys(initialM).FirstOrDefault() ?? string.Empty;
            TxtT.Text = initialT ?? string.Empty;
            TxtGoc.Text = initialAngle ?? string.Empty;

            // Đổi loại / T / dao -> gợi ý lại 4 thông số dao
            foreach (var cmb in new[] { CmbLoai, CmbDao })
            {
                cmb.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
                    new System.Windows.Controls.TextChangedEventHandler((s, e) => SuggestDie()));
                cmb.SelectionChanged += (s, e) => Dispatcher.BeginInvoke(new Action(SuggestDie));
            }
            TxtT.TextChanged += (s, e) => SuggestDie();
            SuggestDie();
            Loaded += (s, e) => (CmbLoai.Text.Length == 0 ? (UIElement)CmbLoai : TxtGoc).Focus();
        }

        // Giá trị tool tự điền lần trước cho từng ô — ô người dùng đã sửa khác đi thì không ghi đè nữa
        private readonly Dictionary<System.Windows.Controls.TextBox, string> _dieAuto = new Dictionary<System.Windows.Controls.TextBox, string>();

        private void SuggestDie()
        {
            string loai = (CmbLoai.Text ?? string.Empty).Trim();
            double? t = TraVThuData.ParseNumber(TxtT.Text);
            string dao = TraVThuData.NormalizeDao(CmbDao.Text);
            string source = string.Empty;
            TraVThuRecord s = loai.Length > 0 && t.HasValue && dao.Length > 1
                ? TraVCalculator.SuggestDieParams(loai, t.Value, dao, out source) : null;

            foreach (var (box, value) in new[] { (TxtSaichou, s?.Saichou), (TxtLSaitei, s?.LSaitei), (TxtZSaitei, s?.ZSaitei), (TxtRend, s?.Rend) })
            {
                string cur = box.Text.Trim();
                if (cur.Length == 0 || (_dieAuto.TryGetValue(box, out string last) && cur == last))
                {
                    box.Text = value ?? string.Empty;
                    _dieAuto[box] = box.Text;
                }
            }

            TxtDieHint.Text = s != null
                ? $"Gợi ý theo {source} — sửa nếu số đo thực tế khác."
                : dao.Length > 1
                    ? $"Dao {dao} chưa có thông số trong dữ liệu — nhập nếu có, để trống cũng được."
                    : "Tuỳ chọn — thông số dao lúc bẻ thử, hiện dưới dòng V= như bẻ thường.";
        }

        private async void OnSubmitClick(object sender, RoutedEventArgs e)
        {
            string loai = (CmbLoai.Text ?? string.Empty).Trim();
            double? t = TraVThuData.ParseNumber(TxtT.Text);
            double? goc = TraVThuData.ParseNumber(TxtGoc.Text);
            string dao = TraVThuData.NormalizeDao(CmbDao.Text);
            double? denta = TraVThuData.ParseNumber(TxtDenta.Text);
            string maTam = (TxtMaTam.Text ?? string.Empty).Trim();

            if (loai.Length == 0) { ShowStatus("Chưa nhập loại vật liệu.", false); CmbLoai.Focus(); return; }
            if (!t.HasValue || t.Value <= 0) { ShowStatus("Độ dày T không hợp lệ (dùng dấu chấm, ví dụ 3.2).", false); TxtT.Focus(); return; }
            if (!goc.HasValue || goc.Value <= 0 || goc.Value >= 180) { ShowStatus("Góc bẻ phải trong khoảng 0–180°.", false); TxtGoc.Focus(); return; }
            if (dao.Length < 2) { ShowStatus("Chưa nhập dao V / U.", false); CmbDao.Focus(); return; }
            if (!denta.HasValue) { ShowStatus("Denta không hợp lệ (dùng dấu chấm, ví dụ 1.25).", false); TxtDenta.Focus(); return; }
            if (maTam.Length == 0) { ShowStatus("Chưa nhập mã tấm.", false); TxtMaTam.Focus(); return; }

            var record = new TraVThuRecord
            {
                Loai = loai,
                T = TxtT.Text.Trim(),
                Goc = goc.Value,
                Dao = dao,
                Denta = TxtDenta.Text.Trim(),
                MaTam = maTam,
                DinhDanh = CmbDinhDanh.SelectedItem as string ?? TraVThuRecord.BeThu,
                Saichou = TxtSaichou.Text.Trim(),
                LSaitei = TxtLSaitei.Text.Trim(),
                ZSaitei = TxtZSaitei.Text.Trim(),
                Rend = TxtRend.Text.Trim()
            };
            // Thông số dao tuỳ chọn: đã nhập thì phải là số
            foreach (var (box, name) in new[] { (TxtSaichou, "最長"), (TxtLSaitei, "Ｌ最低"), (TxtZSaitei, "Ｚ最低"), (TxtRend, "Rend") })
                if (box.Text.Trim().Length > 0 && !TraVThuData.ParseNumber(box.Text).HasValue)
                {
                    ShowStatus(name + " không hợp lệ (dùng dấu chấm, ví dụ 2.5).", false);
                    box.Focus();
                    return;
                }

            TraVThuRecord old = TraVCalculator.GetTrialRecords().FirstOrDefault(r => r.SameKey(record));
            if (old != null)
            {
                bool sameDie = string.Join("|", old.DieParts()) == string.Join("|", record.DieParts());
                if (old.SourceLabel == record.SourceLabel && old.DentaValue == denta && string.Equals(old.MaTam, maTam, StringComparison.OrdinalIgnoreCase)
                    && (sameDie || !record.HasDieParams))
                {
                    ShowStatus("Góc này đã có trong dữ liệu, không cần gửi lại.", true);
                    return;
                }
                // Gửi lại dòng đã có mà để trống thông số dao thì giữ thông số dao cũ (không xoá mất)
                if (!record.HasDieParams)
                    record.CopyDieParams(old);
                string oldDie = old.HasDieParams ? " — " + string.Join(" - ", old.DieParts()) : string.Empty;
                string newDie = record.HasDieParams ? " — " + string.Join(" - ", record.DieParts()) : string.Empty;
                if (MessageBox.Show(this, $"Đã có {old.DinhDanh.ToLowerInvariant()} {old.Loai} T={old.T} {TraVCalculator.FormatAngle(old.Goc)}° {old.Dao}: "
                        + $"Denta {old.Denta} (tấm {old.MaTam}){oldDie}.\n\nGửi yêu cầu thay bằng Denta {record.Denta} (tấm {maTam}){newDie}?",
                        "Đã có góc này", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;
            }

            if (_submit == null)
                return;

            BtnSubmit.IsEnabled = false;
            ShowStatus("Đang gửi yêu cầu...", true);
            try
            {
                var (ok, message) = await _submit(record, old != null ? $"Thay Denta cũ {old.Denta} (tấm {old.MaTam})" : string.Empty);
                ShowStatus(message, ok);
                if (ok)
                {
                    TxtDenta.Clear();
                    TxtMaTam.Clear();
                }
            }
            catch (Exception ex) // async void: lỗi lọt ra ngoài sẽ làm sập CAD
            {
                ShowStatus("Lỗi: " + ex.GetBaseException().Message, false);
            }
            finally
            {
                BtnSubmit.IsEnabled = true;
            }
        }

        private void ShowStatus(string text, bool ok)
        {
            StatusBorder.Visibility = Visibility.Visible;
            TxtStatus.Text = text ?? string.Empty;
            var conv = new BrushConverter();
            StatusBorder.Background = (Brush)conv.ConvertFrom(ok ? "#EFF6FF" : "#FEF2F2");
            StatusBorder.BorderBrush = (Brush)conv.ConvertFrom(ok ? "#BFDBFE" : "#FECACA");
            TxtStatus.Foreground = (Brush)conv.ConvertFrom(ok ? "#1E3A8A" : "#B91C1C");
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
