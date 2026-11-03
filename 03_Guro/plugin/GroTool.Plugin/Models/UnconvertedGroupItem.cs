using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace ZwCadPlugin.Models
{
    /// <summary>
    /// Model hiển thị một nhóm nét chưa đổi trên DataGrid của Tab DSN.
    /// </summary>
    public sealed class UnconvertedGroupItem : INotifyPropertyChanged
    {
        public string Linetype { get; set; } = string.Empty;
        public int ColorIndex { get; set; }
        public string ColorName { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<ObjectId> EntityIds { get; set; } = new List<ObjectId>();
        public Extents3d BoundingBox { get; set; }

        public string DisplayText => $"{Linetype} — {Count} đối tượng";

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
