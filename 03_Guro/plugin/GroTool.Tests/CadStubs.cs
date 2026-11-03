using System;

namespace ZwSoft.ZwCAD.DatabaseServices
{
    public struct ObjectId : IEquatable<ObjectId>
    {
        public static ObjectId Null => default;
        public bool Equals(ObjectId other) => true;
        public static bool operator ==(ObjectId a, ObjectId b) => true;
        public static bool operator !=(ObjectId a, ObjectId b) => false;
        public override bool Equals(object obj) => obj is ObjectId;
        public override int GetHashCode() => 0;
    }

    public struct Extents3d
    {
        public ZwSoft.ZwCAD.Geometry.Point3d MinPoint { get; set; }
        public ZwSoft.ZwCAD.Geometry.Point3d MaxPoint { get; set; }
        public Extents3d(ZwSoft.ZwCAD.Geometry.Point3d min, ZwSoft.ZwCAD.Geometry.Point3d max)
        {
            MinPoint = min;
            MaxPoint = max;
        }
    }
}

namespace ZwSoft.ZwCAD.Geometry
{
    public struct Point3d
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public Point3d(double x, double y, double z) { X = x; Y = y; Z = z; }
    }
}
