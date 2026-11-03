using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ZwCadPlugin.CadServices;
using ZwCadPlugin.Models;
using ZwCadPlugin.ViewModels;

namespace ZwCadPlugin.Views
{
    public partial class PlatePaletteControl : UserControl
    {
        public PlatePaletteControl(PlatePaletteViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        /// <summary>
        /// Cuon danh sach theo mot nac con lan (delta = +-120 moi nac), so dong moi nac
        /// theo cai dat Windows. Goi tu bo bat thong diep cua palette.
        /// </summary>
        public void CuonTheoConLan(int delta)
        {
            int tab = DataContext is PlatePaletteViewModel vm ? vm.SelectedTabIndex : PlatePaletteViewModel.TabDanhSach;
            ScrollViewer sv = tab == PlatePaletteViewModel.TabThongTin ? ThongTinScroll
                            : TimScrollViewer(tab == PlatePaletteViewModel.TabNet ? (DependencyObject)DsnDataGrid : PlatesDataGrid);
            if (sv == null) return;
            int nac = Math.Max(1, Math.Abs(delta) / 120);
            int dong = nac * Math.Max(1, SystemParameters.WheelScrollLines);
            for (int i = 0; i < dong; i++)
            {
                if (delta > 0) sv.LineUp(); else sv.LineDown();
            }
        }

        private static ScrollViewer TimScrollViewer(DependencyObject o)
        {
            if (o is ScrollViewer sv) return sv;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                ScrollViewer kq = TimScrollViewer(VisualTreeHelper.GetChild(o, i));
                if (kq != null) return kq;
            }
            return null;
        }

        /// <summary>Nút "Tấm lỗi tiếp" đổi dòng chọn từ ViewModel -> cuộn cho dòng đó hiện ra.</summary>
        private void PlatesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlatesDataGrid.SelectedItem != null)
                PlatesDataGrid.ScrollIntoView(PlatesDataGrid.SelectedItem);
        }

        /// <summary>
        /// Dong hinh co don chua co text tren ban ve -> khong cho sua. Co "da sua" do
        /// PlateItem tu tinh (so voi gia tri luc quet), khong can theo doi o day.
        /// </summary>
        private void PlatesDataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (e.Row.Item is PlateItem plate && plate.IsOrphan)
                e.Cancel = true;
        }

        /// <summary>
        /// Khi nguoi dung chon dong trong bang net chua doi -> tu dong zoom CAD toi nhom net va chon (select) cac thuc the.
        /// </summary>
        private void DsnDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DsnDataGrid.SelectedItem is UnconvertedGroupItem grp && grp.EntityIds.Count > 0)
            {
                DsnDataGrid.ScrollIntoView(grp);
                CadViewService.ZoomToExtents(grp.BoundingBox, 0.25);
                CadViewService.HighlightEntities(grp.EntityIds);
            }
        }

        /// <summary>
        /// Kich chuot vao dong nhom net (ke ca dong da duoc chon) -> tu dong zoom CAD toi nhom net va chon (select) cac thuc the.
        /// </summary>
        private void DsnDataGridRow_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && row.Item is UnconvertedGroupItem grp && grp.EntityIds.Count > 0)
            {
                CadViewService.ZoomToExtents(grp.BoundingBox, 0.25);
                CadViewService.HighlightEntities(grp.EntityIds);
            }
        }
    }
}
