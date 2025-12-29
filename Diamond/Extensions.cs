using ILGPU;

namespace Diamond;

public static class Extensions
{
    public static float AsFloat(this uint val) => Interop.IntAsFloat(val);
    public static uint AsUInt(this float val) => Interop.FloatAsInt(val);
    public static ulong AsULong(this float val) => Interop.FloatAsInt(val);
    
    public static ulong AsULong(this (float f1, float f2) tuple)
    {
        var high = tuple.f1.AsUInt();
        var low = tuple.f2.AsUInt();
        return (ulong)high << 32 | low;
    }

    public static (float, float) AsFloats(this ulong value)
    {
        var high = (uint)(value >> 32);
        var low = (uint)value;
        return (high.AsFloat(), low.AsFloat());
    }

    public static float[] AsFloats(this uint[] array) => array.Select(u => u.AsFloat()).ToArray();
    public static uint[] AsUInts(this float[] array) => array.Select(f => f.AsUInt()).ToArray();
}