using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WinReg = Microsoft.Win32;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
#endif
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
        public ICommand ZoomNhomCommand { get; }
        public ICommand ConvertToKakouCommand { get; }
        public ICommand ConvertToKegakiCommand { get; }
        public ICommand ConvertToShtCommand { get; }
        public ICommand ConvertToZeroCommand { get; }

        public Action SauKhiChuyenLayer { get; set; }

        public DsnViewModel()
        {
            RefreshCommand = new RelayCommand(RefreshFromDrawing);
            ZoomNhomCommand = new RelayCommand(p => ZoomNhom(p as UnconvertedGroupItem ?? SelectedGroup));
            ConvertToKakouCommand = new RelayCommand(p => ChuyenNhom(p as UnconvertedGroupItem ?? SelectedGroup, "0-KAKOU"),
                p => ((p as UnconvertedGroupItem) ?? SelectedGroup)?.EntityIds.Count > 0);
            ConvertToKegakiCommand = new RelayCommand(p => ChuyenNhom(p as UnconvertedGroupItem ?? SelectedGroup, "3-KEGAKI"),
                p => ((p as UnconvertedGroupItem) ?? SelectedGroup)?.EntityIds.Count > 0);
            ConvertToShtCommand = new RelayCommand(p => ChuyenNhom(p as UnconvertedGroupItem ?? SelectedGroup, "4-SHT"),
                p => ((p as UnconvertedGroupItem) ?? SelectedGroup)?.EntityIds.Count > 0);
            ConvertToZeroCommand = new RelayCommand(p => ChuyenNhom(p as UnconvertedGroupItem ?? SelectedGroup, "0"),
                p => ((p as UnconvertedGroupItem) ?? SelectedGroup)?.EntityIds.Count > 0);
        }

        public void ZoomNhom(UnconvertedGroupItem grp)
        {
            if (grp == null || grp.EntityIds.Count == 0) return;
            SelectedGroup = grp;
            CloudLoiCad.Xoa();
            CadViewService.ZoomToExtents(grp.BoundingBox, 0.25);
            CadViewService.HighlightEntities(grp.EntityIds);
        }

        public void DatChanQuet()
        {
            _selectedGroup = null;
            OnPropertyChanged(nameof(SelectedGroup));
            Groups.Clear();
            TotalUnconvertedCount = 0;
            CurrentLayerName = "—";
            ThongKe = "Đã chặn quét (0 tấm)";
        }

        public void RefreshFromDrawing()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;

            // Đọc layer gốc bản vẽ đã chọn trước đó (Registry layer_xln từ lệnh GRO/XLN)
            string tenLayerUuTien = DocLayerGocMacDinh(doc);

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

        /// <summary>
        /// Layer gốc: chọn trên tab Guro cho bản vẽ này (mục 194), không có thì layer gốc lần
        /// GRO chạy gần nhất (registry layer_xln — lisp ghi lại từ mục 204).
        /// </summary>
        private static string DocLayerGocMacDinh(Document doc)
        {
            try
            {
                string tab = LayerTheoBanVe.Lay(doc)[LayerTheoBanVe.Goc];
                if (!string.IsNullOrWhiteSpace(tab)) return tab;
            }
            catch { }
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

        private void ChuyenNhom(UnconvertedGroupItem grp, string layerDich)
        {
            if (grp == null || grp.EntityIds.Count == 0) return;
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            CloudLoiCad.Xoa();
            int doi = LineScanner.ChuyenLayerNhom(doc, grp.EntityIds, layerDich);
            doc.Editor.WriteMessage($"\n[DSN] Da chuyen {doi} doi tuong nhom \"{grp.Linetype}\" sang layer {layerDich} (ByLayer).\n");

            // Quét lại để cập nhật danh sách
            QuetLayerHienTai();
            SauKhiChuyenLayer?.Invoke();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
