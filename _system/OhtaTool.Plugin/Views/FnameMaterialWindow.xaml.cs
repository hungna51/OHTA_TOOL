using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using OhtaTool.Plugin.Core;

namespace OhtaTool.Plugin.Views
{
    /// <summary>
    /// Cửa sổ thêm vật liệu mới / bổ sung độ dày cho vật liệu có sẵn, gửi ngược lên Google Sheet.
    /// </summary>
    public partial class FnameMaterialWindow : Window
    {
        public delegate Task<(bool ok, string message)> SubmitHandler(string m, string loai, string z, List<string> thicknesses);

        private readonly Func<FnameCatalogData> _getCatalog;
        private readonly SubmitHandler _submit;

        public FnameMaterialWindow(Func<FnameCatalogData> getCatalog, IEnumerable<string> loaiOptions,
                                   string initialM, string initialT, SubmitHandler submit)
        {
            InitializeComponent();
            _getCatalog = getCatalog;
            _submit = submit;

            var logo = IconLoader.GetSettingsLogo();
            if (logo != null)
                Icon = logo;

            FillOptions(loaiOptions);
            CmbM.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler((s, e) => OnMTextChanged()));
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
                return;
            }
            if (spec == null)
            {
                TxtMState.Text = "Mác mới — sẽ thêm 1 dòng mới vào catalog.";
                TxtMState.Foreground = (Brush)new BrushConverter().ConvertFrom("#B45309");
                return;
            }

            TxtMState.Text = "Mác có sẵn — độ dày hiện có: " + (spec.Thicknesses.Count > 0 ? string.Join(" ", spec.Thicknesses) : "(chưa có)");
            TxtMState.Foreground = (Brush)new BrushConverter().ConvertFrom("#047857");
            CmbLoai.Text = !string.IsNullOrEmpty(spec.Loai)
                ? spec.Loai
                : TraVCalculator.GetTraVMaterialKeys(spec.M).FirstOrDefault()?.Split(' ')[0] ?? string.Empty;
            CmbZ.Text = spec.Z ?? string.Empty;
        }

        private async void OnSubmitClick(object sender, RoutedEventArgs e)
        {
            string m = (CmbM.Text ?? string.Empty).Trim();
            string loai = (CmbLoai.Text ?? string.Empty).Trim();
            string z = (CmbZ.Text ?? string.Empty).Trim();

            if (m.Length == 0) { ShowStatus("Chưa nhập tên vật liệu M=.", false); CmbM.Focus(); return; }
            if (loai.Length == 0) { ShowStatus("Chưa nhập loại vật liệu.", false); CmbLoai.Focus(); return; }
            if (!FnameCatalogData.TryParseThicknessInput(TxtT.Text, out List<string> ts, out string err))
            {
                ShowStatus(err, false);
                TxtT.Focus();
                return;
            }

            FnameMaterialSpec spec = (_getCatalog?.Invoke())?.FindMaterial(m);
            if (spec != null)
            {
                bool sameLoai = string.Equals(spec.Loai ?? string.Empty, loai, StringComparison.OrdinalIgnoreCase);
                bool sameZ = z.Length == 0 || string.Equals(spec.Z ?? string.Empty, z, StringComparison.OrdinalIgnoreCase);
                if (sameLoai && sameZ && ts.All(t => FnameCatalogData.ContainsThickness(spec.Thicknesses, t)))
                {
                    ShowStatus($"\"{spec.M}\" đã có đủ các độ dày này, không cần cập nhật.", true);
                    return;
                }
            }

            if (_submit == null)
                return;

            BtnSubmit.IsEnabled = false;
            ShowStatus("Đang gửi yêu cầu...", true);
            try
            {
                var (ok, message) = await _submit(m, loai, z, ts);
                ShowStatus(message, ok);
                if (ok)
                {
                    FillOptions(null);
                    OnMTextChanged();
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
