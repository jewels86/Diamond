using ILGPU;

namespace Diamond;

public static class Extensions
{
    public static float AsFloat(this uint val) => Interop.IntAsFloat(val);
    public static uint AsUInt(this float val) => Interop.FloatAsInt(val);
    public static ulong AsULong(this float val) => Interop.FloatAsInt(val);
    
    public static ulong AsULong(this (float high, float low) tuple)
    {
        var high = tuple.high.AsUInt();
        var low = tuple.low.AsUInt();
        return (ulong)high << 32 | low;
    }

    public static (float high, float low) AsFloats(this ulong value)
    {
        var high = (uint)(value >> 32);
        var low = (uint)value;
        return (high.AsFloat(), low.AsFloat());
    }

    public static float[] AsFloats(this uint[] array) => array.Select(u => u.AsFloat()).ToArray();
    public static uint[] AsUInts(this float[] array) => array.Select(f => f.AsUInt()).ToArray();
}