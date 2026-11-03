using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WinReg = Microsoft.Win32;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwCadPlugin.CadServices;
using ZwCadPlugin.Core;
using ZwCadPlugin.Models;

namespace ZwCadPlugin.ViewModels
{
    public sealed class DsnViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<UnconvertedGroupItem> Groups { get; } = new ObservableCollection<UnconvertedGroupItem>();

        private string _currentLayerName = "Chưa xác định";
        public string CurrentLayerName
        {
            get => _currentLayerName;
            private set
            {
                if (_currentLayerName != value)
                {
                    _currentLayerName = value;
                    OnPropertyChanged();
                }
            }
        }

        private UnconvertedGroupItem _selectedGroup;
        public UnconvertedGroupItem SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                if (_selectedGroup != value)
                {
                    _selectedGroup = value;
                    OnPropertyChanged();
                    if (_selectedGroup != null && _selectedGroup.EntityIds.Count > 0)
                    {
                        // Tự động zoom màn hình CAD bao trọn nhóm nét và chọn (select) các thực thể
                        CadViewService.ZoomToExtents(_selectedGroup.BoundingBox, 0.25);
                        CadViewService.HighlightEntities(_selectedGroup.EntityIds);
                    }
                }
            }
        }

        private string _thongKe = "Chưa quét nét";
        public string ThongKe
        {
            get => _thongKe;
            private set { if (_thongKe != value) { _thongKe = value; OnPropertyChanged(); } }
        }

        private int _totalUnconvertedCount;
        public int TotalUnconvertedCount
        {
            get => _totalUnconvertedCount;
            private set { if (_totalUnconvertedCount != value) { _totalUnconvertedCount = value; OnPropertyChanged(); } }
        }

        public ICommand RefreshCommand { get; }
        public ICommand ConvertToKakouCommand { get; }
        public ICommand ConvertToKegakiCommand { get; }
        public ICommand ConvertToSunpouCommand { get; }
        public ICommand ConvertToShtCommand { get; }
        public ICommand ConvertToZeroCommand { get; }

        public DsnViewModel()
        {
            RefreshCommand = new RelayCommand(RefreshFromDrawing);
            ConvertToKakouCommand = new RelayCommand(() => ChuyenLayer("0-KAKOU"), () => SelectedGroup != null && SelectedGroup.EntityIds.Count > 0);
            ConvertToKegakiCommand = new RelayCommand(() => ChuyenLayer("3-KEGAKI"), () => SelectedGroup != null && SelectedGroup.EntityIds.Count > 0);
            ConvertToSunpouCommand = new RelayCommand(() => ChuyenLayer("1-SUNPOU"), () => SelectedGroup != null && SelectedGroup.EntityIds.Count > 0);
            ConvertToShtCommand = new RelayCommand(() => ChuyenLayer("4-SHT"), () => SelectedGroup != null && SelectedGroup.EntityIds.Count > 0);
            ConvertToZeroCommand = new RelayCommand(() => ChuyenLayer("0"), () => SelectedGroup != null && SelectedGroup.EntityIds.Count > 0);
        }

        public void RefreshFromDrawing()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;

            // Đọc layer gốc bản vẽ đã chọn trước đó (Registry layer_xln từ lệnh GRO/XLN)
            string tenLayerUuTien = DocLayerGocMacDinh();

            // Nếu Registry chưa có hoặc layer không tồn tại trong bản vẽ, tự động lấy layer đông nét nhất
            if (string.IsNullOrWhiteSpace(tenLayerUuTien) || !LayerTonTai(db, tenLayerUuTien))
            {
                var dsLayer = LineScanner.DocDanhSachLayer(db);
                var layTotNhat = dsLayer.FirstOrDefault();
                if (layTotNhat != null)
                {
                    tenLayerUuTien = layTotNhat.LayerName;
                    LuuLayerGocMacDinh(tenLayerUuTien);
                }
                else
                {
                    tenLayerUuTien = "0";
                }
            }

            CurrentLayerName = tenLayerUuTien;
            QuetLayerHienTai();
        }

        private static bool LayerTonTai(Database db, string layerName)
        {
            if (db == null || string.IsNullOrWhiteSpace(layerName)) return false;
            try
            {
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                    return lt.Has(layerName);
                }
            }
            catch
            {
                return false;
            }
        }

        private static string DocLayerGocMacDinh()
        {
            try
            {
                using (var k = WinReg.Registry.CurrentUser.OpenSubKey(@"Software\OFT_Lisp\GRO"))
                {
                    return k?.GetValue("layer_xln") as string;
                }
            }
            catch
            {
                return null;
            }
        }

        private static void LuuLayerGocMacDinh(string layerName)
        {
            if (string.IsNullOrWhiteSpace(layerName)) return;
            try
            {
                using (var k = WinReg.Registry.CurrentUser.CreateSubKey(@"Software\OFT_Lisp\GRO"))
                {
                    k?.SetValue("layer_xln", layerName);
                }
            }
            catch
            {
            }
        }

        private void QuetLayerHienTai()
        {
            if (string.IsNullOrWhiteSpace(CurrentLayerName))
            {
                Groups.Clear();
                TotalUnconvertedCount = 0;
                ThongKe = "Chưa xác định layer";
                return;
            }

            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var nhom = LineScanner.QuetNetChuaDoi(doc.Database, CurrentLayerName);
            Groups.Clear();
            foreach (var g in nhom) Groups.Add(g);

            TotalUnconvertedCount = Groups.Sum(g => g.Count);
            ThongKe = LineClassifier.ThongKeDsn(TotalUnconvertedCount, Groups.Count);

            if (Groups.Count > 0)
                SelectedGroup = Groups[0];
            else
                SelectedGroup = null;
        }

        private void ChuyenLayer(string layerDich)
        {
            if (SelectedGroup == null || SelectedGroup.EntityIds.Count == 0) return;
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            int doi = LineScanner.ChuyenLayerNhom(doc, SelectedGroup.EntityIds, layerDich);
            doc.Editor.WriteMessage($"\n[DSN] Đã chuyển {doi} đối tượng nhóm \"{SelectedGroup.Linetype}\" sang layer {layerDich} (ByLayer).\n");

            // Quét lại để cập nhật danh sách
            QuetLayerHienTai();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
