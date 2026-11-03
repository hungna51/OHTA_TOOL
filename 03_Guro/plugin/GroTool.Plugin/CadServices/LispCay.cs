using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;
using ZwSoft.ZwCAD.Runtime;

namespace ZwCadPlugin.CadServices
{
    /// <summary>
    /// Mục 174: đọc tham số lisp (ResultBuffer) thành cây: List&lt;object&gt; (danh sách), double (mọi số),
    /// string, ObjectId (ename), null (nil), true (T). Danh sách 2 / 3 số thực ZWCAD có thể gửi dạng
    /// Point2d / Point3d — đổi lại thành danh sách số. Nhờ vậy lisp gửi THẲNG list số thực, không qua
    /// chuỗi (bài học mục 170).
    /// </summary>
    public static class LispCay
    {
        /// <summary>Các tham số cấp cao nhất.</summary>
        public static List<object> Doc(ResultBuffer rb)
        {
            var goc = new List<object>();
            if (rb == null) return goc;
            var chong = new Stack<List<object>>();
            List<object> cur = goc;
            foreach (TypedValue tv in rb)
            {
                switch ((LispDataType)tv.TypeCode)
                {
                    case LispDataType.ListBegin:
                        var moi = new List<object>();
                        cur.Add(moi);
                        chong.Push(cur);
                        cur = moi;
                        break;
                    case LispDataType.ListEnd:
                    case LispDataType.DottedPair:
                        if (chong.Count > 0) cur = chong.Pop();
                        break;
                    case LispDataType.Nil:
                        cur.Add(null);
                        break;
                    case LispDataType.T_atom:
                        cur.Add(true);
                        break;
                    default:
                        cur.Add(DoiGiaTri(tv.Value));
                        break;
                }
            }
            return goc;
        }

        private static object DoiGiaTri(object v)
        {
            switch (v)
            {
                case double d: return d;
                case int i: return (double)i;
                case short s: return (double)s;
                case long l: return (double)l;
                case Point3d p3: return new List<object> { p3.X, p3.Y, p3.Z };
                case Point2d p2: return new List<object> { p2.X, p2.Y };
                default: return v;
            }
        }

        public static double? So(object o) => o is double d ? d : (double?)null;

        public static List<object> Ds(object o) => o as List<object>;
    }
}
