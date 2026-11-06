using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using OhtaTool.Plugin.Core;

namespace OhtaTool.Plugin.Views
{
    /// <summary>
    /// Danh sách góc bẻ thử / nội suy / ngoại suy đang dùng trên máy (bảng trav_thu tải từ Sheet
    /// + dòng máy này đã gửi đang chờ duyệt), có lọc theo từ khoá và định danh.
    /// </summary>
    public partial class FnameTrialListWindow : Window
    {
        private const string TatCa = "Tất cả";

        public sealed class Row
        {
            public string Loai { get; set; }
            public string T { get; set; }
            public string Goc { get; set; }
            public double GocValue { get; set; }
            public string Dao { get; set; }
            public string Denta { get; set; }
            public string MaTam { get; set; }
            public string DinhDanh { get; set; }
            public string TrangThai { get; set; }

            internal string SearchText => TraVThuData.Fold(string.Join(" ", Loai, T, Goc, Dao, Denta, MaTam, DinhDanh, TrangThai));
        }

        private readonly Action<Window> _openAdd;
        private List<Row> _all = new List<Row>();

        public FnameTrialListWindow(string initialSearch, Action<Window> openAdd)
        {
            InitializeComponent();
            _openAdd = openAdd;

            var logo = IconLoader.GetSettingsLogo();
            if (logo != null)
                Icon = logo;

            CmbDinhDanh.Items.Add(TatCa);
            CmbDinhDanh.Items.Add(TraVThuRecord.BeThu);
            CmbDinhDanh.Items.Add(TraVThuRecord.NoiSuy);
            CmbDinhDanh.Items.Add(TraVThuRecord.NgoaiSuy);
            CmbDinhDanh.SelectedIndex = 0;

            TxtSearch.Text = initialSearch ?? string.Empty;
            TxtSearch.TextChanged += (s, e) => ApplyFilter();
            CmbDinhDanh.SelectionChanged += (s, e) => ApplyFilter();
            Reload();
            Loaded += (s, e) => TxtSearch.Focus();
        }

        private void Reload()
        {
            List<TraVThuRecord> pending = TraVThuLocalAdditions.Load(null);
            _all = TraVCalculator.GetTrialRecords()
                .OrderBy(r => TraVThuData.NormalizeLoai(r.Loai), StringComparer.Ordinal)
                .ThenBy(r => r.TValue)
                .ThenBy(r => TraVThuData.ParseNumber(r.Dao.Length > 1 ? r.Dao.Substring(1) : r.Dao) ?? double.MaxValue)
                .ThenBy(r => r.Goc)
                .Select(r => new Row
                {
                    Loai = r.Loai,
                    T = r.T,
                    Goc = TraVCalculator.FormatAngle(r.Goc),
                    GocValue = r.Goc,
                    Dao = r.Dao,
                    Denta = r.Denta,
                    MaTam = r.MaTam,
                    DinhDanh = r.SourceLabel,
                    TrangThai = pending.Any(p => p.SameKey(r) && p.Denta == r.Denta) ? "Chờ duyệt" : "Đã duyệt"
                }).ToList();
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string[] words = TraVThuData.Fold(TxtSearch.Text).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string dinh = CmbDinhDanh.SelectedItem as string ?? TatCa;
            List<Row> shown = _all.Where(r => (dinh == TatCa || r.DinhDanh == dinh)
                                              && words.All(w => r.SearchText.Contains(w))).ToList();
            GridRecords.ItemsSource = shown;

            int beThu = _all.Count(r => r.DinhDanh == TraVThuRecord.BeThu);
            int cho = _all.Count(r => r.TrangThai == "Chờ duyệt");
            TxtCount.Text = $"Hiện {shown.Count} / {_all.Count} dòng — {beThu} bẻ thử, {_all.Count - beThu} nội suy / ngoại suy"
                + (cho > 0 ? $", {cho} dòng máy này gửi đang chờ duyệt" : string.Empty)
                + ". Dữ liệu cập nhật từ Google Sheet ở lần mở tool đầu ngày hoặc khi bấm Cập nhật trong Cài đặt.";
        }

        private void OnAddClick(object sender, RoutedEventArgs e)
        {
            try
            {
                _openAdd?.Invoke(this);
                Reload();
            }
            catch (Exception ex)
            {
                OhtaTool.Plugin.Core.BugLog.Write("OHTA/FnameTrialListWindow.xaml", ex);
                MessageBox.Show(this, "Không mở được cửa sổ góc bẻ thử: " + ex.GetBaseException().Message,
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
