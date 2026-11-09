using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OhtaTool.Plugin.Core;

namespace OhtaTool.Plugin.Views
{
    /// <summary>
    /// Cửa sổ thêm vật liệu mới / bổ sung độ dày (tab catalog) và bổ sung T thực theo độ dày (tab tam, khoá loại + T,
    /// kèm người xác nhận + ghi chú + 1–3 ảnh), gửi ngược lên Google Sheet. Mỗi T thực là 1 yêu cầu duyệt riêng.
    /// </summary>
    public partial class FnameMaterialWindow : Window
    {
        public delegate Task<(bool ok, string message)> SubmitHandler(string m, string loai, string z, List<string> thicknesses);
        public delegate Task<(bool ok, string message)> SubmitTamHandler(TamRecord record, List<byte[]> jpegs);

        private readonly Func<FnameCatalogData> _getCatalog;
        private readonly SubmitHandler _submit;
        private readonly SubmitTamHandler _submitTam;

        // Ô nhập T thực đang hiện, theo độ dày (giữ chữ đã gõ khi danh sách T được dựng lại)
        private readonly Dictionary<string, TextBox> _tamInputs = new Dictionary<string, TextBox>();
        private readonly Dictionary<string, string> _tamTyped = new Dictionary<string, string>();

        public FnameMaterialWindow(Func<FnameCatalogData> getCatalog, IEnumerable<string> loaiOptions,
                                   string initialM, string initialT, SubmitHandler submit, SubmitTamHandler submitTam = null)
        {
            InitializeComponent();
            _getCatalog = getCatalog;
            _submit = submit;
            _submitTam = submitTam;

            var logo = IconLoader.GetSettingsLogo();
            if (logo != null)
                Icon = logo;

            FillOptions(loaiOptions);
            CmbM.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler((s, e) => OnMTextChanged()));
            CmbLoai.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler((s, e) => RebuildTamRows()));
            CmbLoai.SelectionChanged += (s, e) => Dispatcher.BeginInvoke(new Action(RebuildTamRows));
            TxtT.TextChanged += (s, e) => RebuildTamRows();
            PanelEvidence.ShowStatus = ShowStatus;
            PreviewKeyDown += (s, e) => e.Handled |= PanelEvidence.TryHandlePasteKey(e);
            CmbM.Text = initialM ?? string.Empty;
            TxtT.Text = initialT ?? string.Empty;
            OnMTextChanged();
            Loaded += (s, e) => CmbM.Focus();
        }

        private void FillOptions(IEnumerable<string> loaiOptions)
        {
            FnameCatalogData cat = _getCatalog?.Invoke() ?? new FnameCatalogData();

            string mText = CmbM.Text;
            CmbM.Items.Clear();
            foreach (FnameMaterialSpec spec in cat.Materials)
                CmbM.Items.Add(spec.M);
            CmbM.Text = mText;

            if (loaiOptions != null)
            {
                CmbLoai.Items.Clear();
                var loai = new List<string>(loaiOptions);
                foreach (FnameMaterialSpec spec in cat.Materials)
                    if (!string.IsNullOrEmpty(spec.Loai) && !loai.Contains(spec.Loai, StringComparer.OrdinalIgnoreCase))
                        loai.Add(spec.Loai);
                foreach (string l in loai)
                    CmbLoai.Items.Add(l);
            }

            string zText = CmbZ.Text;
            CmbZ.Items.Clear();
            foreach (string z in cat.GetDistinctZList())
                CmbZ.Items.Add(z);
            CmbZ.Text = zText;
        }

        /// <summary>Mác có sẵn: điền Loai / Z hiện tại và cho xem độ dày đang có.</summary>
        private void OnMTextChanged()
        {
            string m = (CmbM.Text ?? string.Empty).Trim();
            FnameMaterialSpec spec = (_getCatalog?.Invoke())?.FindMaterial(m);
            if (m.Length == 0)
            {
                TxtMState.Text = "Chọn mác có sẵn để bổ sung độ dày, hoặc gõ tên mác mới.";
                TxtMState.Foreground = (Brush)new BrushConverter().ConvertFrom("#64748B");
            }
            else if (spec == null)
            {
                TxtMState.Text = "Mác mới — sẽ thêm 1 dòng mới vào catalog.";
                TxtMState.Foreground = (Brush)new BrushConverter().ConvertFrom("#B45309");
            }
            else
            {
                TxtMState.Text = "Mác có sẵn — độ dày hiện có: " + (spec.Thicknesses.Count > 0 ? string.Join(" ", spec.Thicknesses) : "(chưa có)");
                TxtMState.Foreground = (Brush)new BrushConverter().ConvertFrom("#047857");
                CmbLoai.Text = !string.IsNullOrEmpty(spec.Loai)
                    ? spec.Loai
                    : TraVCalculator.GetTraVMaterialKeys(spec.M).FirstOrDefault()?.Split(' ')[0] ?? string.Empty;
                CmbZ.Text = spec.Z ?? string.Empty;
            }
            RebuildTamRows();
        }

        // ---------- T thực theo độ dày ----------

        /// <summary>Loại dùng cho bảng tam: Loai của form, cộng "SIMA" khi tên mác có 縞板 / SIMA.</summary>
        private string TamLoai()
        {
            string loai = (CmbLoai.Text ?? string.Empty).Trim();
            if (loai.Length == 0)
                return string.Empty;
            string m = (CmbM.Text ?? string.Empty).Trim();
            bool sima = m.IndexOf("縞板", StringComparison.Ordinal) >= 0 || m.IndexOf("SIMA", StringComparison.OrdinalIgnoreCase) >= 0;
            return sima && TraVThuData.NormalizeLoai(loai).IndexOf("SIMA", StringComparison.Ordinal) < 0 ? loai + " SIMA" : loai;
        }

        /// <summary>Độ dày đang xét: chỉ các T người dùng gõ trong ô Độ dày (bỏ trùng, xếp tăng dần) — không liệt kê cả mác cho đỡ rối.</summary>
        private List<string> CurrentThicknesses()
        {
            var list = new List<string>();
            IEnumerable<string> typed = (TxtT.Text ?? string.Empty).Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => FnameCatalogData.FormatThickness(x)).Where(x => TraVThuData.ParseNumber(x) > 0);
            foreach (string t in typed)
                if (!list.Any(x => FnameCatalogData.ContainsThickness(new[] { x }, t)))
                    list.Add(t);
            return list.OrderBy(t => TraVThuData.ParseNumber(t) ?? 0).ToList();
        }

        private static string TKey(string t) => (TraVThuData.ParseNumber(t) ?? 0).ToString("0.###", CultureInfo.InvariantCulture);

        private void RebuildTamRows()
        {
            foreach (var kv in _tamInputs)
                _tamTyped[kv.Key] = kv.Value.Text;
            _tamInputs.Clear();
            PanelTam.Children.Clear();

            string loai = TamLoai();
            List<string> ts = CurrentThicknesses();
            if (loai.Length == 0)
            {
                TxtTamLoai.Text = "Nhập loại vật liệu để xem / bổ sung T thực.";
                UpdateEvidenceVisibility();
                return;
            }
            TxtTamLoai.Text = $"T thực áp cho loại {loai} — dùng chung cho mọi mác cùng loại. Chỉ nhập cho độ dày chưa có, để trống thì bỏ qua.";
            if (ts.Count == 0)
            {
                PanelTam.Children.Add(new TextBlock { Text = "Gõ độ dày ở ô Độ dày T= phía trên để xem / bổ sung T thực (vd 6.0).", FontSize = 11, Foreground = Hint, TextWrapping = TextWrapping.Wrap });
                UpdateEvidenceVisibility();
                return;
            }

            foreach (string t in ts)
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.Children.Add(new TextBlock { Text = "T=" + t, FontFamily = new FontFamily("Consolas"), FontSize = 12, VerticalAlignment = VerticalAlignment.Center });

                UIElement cell;
                TamRecord old = TraVCalculator.FindTam(loai, TraVThuData.ParseNumber(t) ?? 0);
                if (old != null)
                {
                    string note = old.IsBoSung ? (old.ChoDuyet ? "  (bổ sung · chờ duyệt)" : "  (bổ sung)") : string.Empty;
                    cell = new TextBlock
                    {
                        Text = "T thực = " + old.THienThi + note,
                        FontSize = 12,
                        Foreground = (Brush)new BrushConverter().ConvertFrom("#047857"),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                }
                else
                {
                    var box = new TextBox
                    {
                        Width = 80,
                        Height = 23,
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 12,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        ToolTip = "T thực để xem / tính toán (để trống = không bổ sung)"
                    };
                    if (_tamTyped.TryGetValue(TKey(t), out string typed))
                        box.Text = typed;
                    box.TextChanged += (s, e) => UpdateEvidenceVisibility();
                    _tamInputs[TKey(t)] = box;
                    var sp = new StackPanel { Orientation = Orientation.Horizontal };
                    sp.Children.Add(new TextBlock { Text = "T thực = ", FontSize = 12, VerticalAlignment = VerticalAlignment.Center });
                    sp.Children.Add(box);
                    sp.Children.Add(new TextBlock { Text = "  chưa có", FontSize = 11, Foreground = Hint, VerticalAlignment = VerticalAlignment.Center });
                    cell = sp;
                }
                Grid.SetColumn(cell, 1);
                row.Children.Add(cell);
                PanelTam.Children.Add(row);
            }
            UpdateEvidenceVisibility();
        }

        private static readonly Brush Hint = (Brush)new BrushConverter().ConvertFrom("#64748B");

        /// <summary>Các T thực đang được nhập (độ dày, chuỗi đã gõ).</summary>
        private List<KeyValuePair<string, string>> TamEntries() =>
            _tamInputs.Where(kv => kv.Value.Text.Trim().Length > 0)
                .Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value.Text.Trim())).ToList();

        private void UpdateEvidenceVisibility()
        {
            PanelEvidence.Visibility = TamEntries().Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // ---------- Gửi ----------

        private async void OnSubmitClick(object sender, RoutedEventArgs e)
        {
            string m = (CmbM.Text ?? string.Empty).Trim();
            string loai = (CmbLoai.Text ?? string.Empty).Trim();
            string z = (CmbZ.Text ?? string.Empty).Trim();

            if (m.Length == 0) { ShowStatus("Chưa nhập tên vật liệu M=.", false); CmbM.Focus(); return; }
            if (loai.Length == 0) { ShowStatus("Chưa nhập loại vật liệu.", false); CmbLoai.Focus(); return; }

            // Phần catalog: ô Độ dày trống thì không gửi; mác đã có đủ (cùng Loai / Z) thì cũng không gửi
            List<string> ts = new List<string>();
            bool catalogNeeded = false;
            if ((TxtT.Text ?? string.Empty).Trim().Length > 0)
            {
                if (!FnameCatalogData.TryParseThicknessInput(TxtT.Text, out ts, out string err))
                {
                    ShowStatus(err, false);
                    TxtT.Focus();
                    return;
                }
                FnameMaterialSpec spec = (_getCatalog?.Invoke())?.FindMaterial(m);
                bool sameLoai = spec != null && string.Equals(spec.Loai ?? string.Empty, loai, StringComparison.OrdinalIgnoreCase);
                bool sameZ = spec != null && (z.Length == 0 || string.Equals(spec.Z ?? string.Empty, z, StringComparison.OrdinalIgnoreCase));
                catalogNeeded = spec == null || !(sameLoai && sameZ && ts.All(t => FnameCatalogData.ContainsThickness(spec.Thicknesses, t)));
            }

            // Phần T thực: số hợp lệ + bằng chứng đủ
            var tamEntries = TamEntries();
            foreach (var kv in tamEntries)
                if (!(TraVThuData.ParseNumber(kv.Value) > 0))
                {
                    ShowStatus($"T thực của T={kv.Key} không hợp lệ (dùng dấu chấm, ví dụ 7.3).", false);
                    _tamInputs[kv.Key].Focus();
                    return;
                }
            string nguoi = PanelEvidence.NguoiXacNhan;
            string ghiChu = PanelEvidence.GhiChu;
            if (tamEntries.Count > 0)
            {
                string evErr = PanelEvidence.Validate("T thực");
                if (evErr != null) { ShowStatus(evErr, false); return; }
            }

            if (!catalogNeeded && tamEntries.Count == 0)
            {
                ShowStatus(ts.Count > 0
                    ? $"\"{m}\" đã có đủ các độ dày này và chưa nhập T thực nào — không có gì để gửi."
                    : "Chưa có gì để gửi: nhập độ dày mới hoặc T thực.", true);
                return;
            }
            if (_submit == null || (tamEntries.Count > 0 && _submitTam == null))
                return;

            BtnSubmit.IsEnabled = false;
            var lines = new List<string>();
            bool allOk = true;
            try
            {
                if (catalogNeeded)
                {
                    ShowStatus("Đang gửi vật liệu...", true);
                    var (ok, message) = await _submit(m, loai, z, ts);
                    allOk &= ok;
                    lines.Add((ok ? "✔ " : "✕ ") + message);
                    if (ok)
                        TxtT.Text = string.Empty;
                }

                string tamLoai = TamLoai();
                var images = PanelEvidence.Images;
                foreach (var kv in tamEntries)
                {
                    ShowStatus($"Đang gửi T thực T={kv.Key} kèm {images.Count} ảnh...", true);
                    var record = new TamRecord
                    {
                        Loai = tamLoai,
                        T = kv.Key,
                        THienThi = kv.Value,
                        Nguon = TamRecord.NguonBoSung,
                        NguoiXacNhan = nguoi,
                        GhiChu = ghiChu
                    };
                    var (ok, message) = await _submitTam(record, images);
                    allOk &= ok;
                    lines.Add((ok ? "✔ " : "✕ ") + message);
                    if (ok)
                        _tamTyped.Remove(kv.Key);   // gửi được -> dòng chuyển sang "bổ sung · chờ duyệt"
                }
                if (allOk && tamEntries.Count > 0)
                {
                    PanelEvidence.ClearImages();
                }
                ShowStatus(string.Join("\n", lines), allOk);
                FillOptions(null);
                OnMTextChanged();
            }
            catch (Exception ex) // async void: lỗi lọt ra ngoài sẽ làm sập CAD
            {
                lines.Add("Lỗi: " + ex.GetBaseException().Message);
                ShowStatus(string.Join("\n", lines), false);
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
