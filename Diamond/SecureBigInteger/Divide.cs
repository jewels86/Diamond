using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger operator /(SecureBigInteger a, SecureBigInteger b) => HostDivide(a, b).quotient;
    public static SecureBigInteger operator %(SecureBigInteger a, SecureBigInteger b) => HostDivide(a, b).remainder;
    
    #region Host
    public static (SecureBigInteger quotient, SecureBigInteger remainder) HostDivide(SecureBigInteger a, SecureBigInteger b)
    {
        var (hostA, hostB) = (a.AsHost(), b.AsHost());
        var quotient = new uint[Math.Max(1, hostA.Length - hostB.Length + 1)];
        var remainder = new uint[hostA.Length];
    
        CryptographicOperations.ConstantTime.Copy(hostA, 0, remainder, 0, hostA.Length);
    
        for (int i = hostA.Length - 1; i >= hostB.Length - 1; i--)
        {
            var topRemainder = (ulong)remainder[i] << 32 | remainder[i - 1];
            var topDivisor = hostB[^1];
            var qEstimate = (uint)(topRemainder / topDivisor);

            var borrow = 0UL;
            for (int j = 0; j < b.Length; j++)
            {
                var product = (ulong)qEstimate * hostB[j];
                var diff = (ulong)remainder[i - hostB.Length + 1 + j] - (uint)product - borrow;
                remainder[i - hostB.Length + 1 + j] = (uint)diff;
                borrow = (product >> 32) + (diff >> 32 & 1);
            }

            var overestimated = (uint)(-(long)borrow >> 63) & 1;
            var correctedQ = qEstimate - overestimated;
            quotient[i - hostB.Length + 1] = correctedQ;

            var carry = 0UL;
            for (int j = 0; j < hostB.Length; j++)
            {
                var bValue = CryptographicOperations.ConstantTime.Select(overestimated, hostB[j], 0U);
                var sum = (ulong)remainder[i - hostB.Length + 1 + j] + bValue + carry;
                remainder[i - hostB.Length + 1 + j] = (uint)sum;
                carry = sum >> 32;
            }
        }
    
        return (new(quotient), new(remainder));
    }
    #endregion
    #region Batched
    public static (SecureBigInteger[] quotients, SecureBigInteger[] remainders) BatchedDivide(SecureBigInteger[] allA, SecureBigInteger[] allB)
    {
        if (allA.Length != allB.Length) throw new ArgumentException("Arrays must be the same length");

        var batchSize = allA.Length;
        var (allAHost, allBHost) = (allA.Select(b => b.AsHost()).ToArray(), allB.Select(b => b.AsHost()).ToArray());
        var (allAAccelerated, allBAccelerated) = (allA.Select(b => b.AsAccelerated()).ToArray(), allB.Select(b => b.AsAccelerated()).ToArray());
        var aidx = allBAccelerated[0].AcceleratorIndex; // we can do this better- dont accelerate all of them

        var aLengths = allAAccelerated.Select(a => a.TotalSize).ToArray();
        var bLengths = allBAccelerated.Select(b => b.TotalSize).ToArray();
        var quotientLengths = aLengths.Zip(bLengths, (aLength, bLength) => Math.Max(1, aLength - bLength + 1)).ToArray();
        var remainderLengths = aLengths;
        
        var aOffsets = new int[batchSize];
        var bOffsets = new int[batchSize];
        var quotientOffsets = new int[batchSize];
        var remainderOffsets = new int[batchSize];
        
        var (aTotal, bTotal, quotientTotal, remainderTotal) = (0, 0, 0, 0);
        for (int i = 0; i < batchSize; i++)
        {
            aOffsets[i] = aTotal;
            bOffsets[i] = bTotal;
            quotientOffsets[i] = quotientTotal;
            remainderOffsets[i] = remainderTotal;
        
            aTotal += aLengths[i];
            bTotal += bLengths[i];
            quotientTotal += quotientLengths[i];
            remainderTotal += remainderLengths[i];
        }

        var a = Compute.Get(aidx, aTotal);
        var b = Compute.Get(aidx, bTotal);
        var quotients = Compute.Get(aidx, quotientTotal);
        var remainders = Compute.Get(aidx, remainderTotal);

        var aFlat = new float[aTotal];
        var bFlat = new float[bTotal];
        for (int i = 0; i < batchSize; i++)
        {
            Array.Copy(allAHost[i].AsFloats(), 0, aFlat, aOffsets[i], aLengths[i]);
            Array.Copy(allBHost[i].AsFloats(), 0, bFlat, bOffsets[i], bLengths[i]);
        }
        a.Set(aFlat);
        b.Set(bFlat);
        
        var aLengthsAccelerated = Compute.Get(aidx, aLengths.Length).Set(aLengths.Select(l => (float)l).ToArray());
        var bLengthsAccelerated = Compute.Get(aidx, bLengths.Length).Set(bLengths.Select(l => (float)l).ToArray());
        var aOffsetsAccelerated = Compute.Get(aidx, aOffsets.Length).Set(aOffsets.Select(l => (float)l).ToArray());
        var bOffsetsAccelerated = Compute.Get(aidx, bOffsets.Length).Set(bOffsets.Select(l => (float)l).ToArray());
        var outOffsetsAccelerated = Compute.Get(aidx, quotientOffsets.Length).Set(quotientOffsets.Select(l => (float)l).ToArray());
        
        Compute.Call(aidx, BatchedDivideKernel, batchSize,
            quotients, remainders, a, b, aLengthsAccelerated, bLengthsAccelerated, aOffsetsAccelerated, bOffsetsAccelerated, outOffsetsAccelerated);

        Compute.Synchronize(aidx);
        var quotientsHost = quotients.GetAsArray1D();
        var remaindersHost = remainders.GetAsArray1D();
        var quotientResults = new SecureBigInteger[batchSize];
        var remainderResults = new SecureBigInteger[batchSize];

        for (int i = 0; i < batchSize; i++)
        {
            var quotientData = new ArraySegment<float>(quotientsHost, quotientOffsets[i], quotientLengths[i]);
            var remainderData = new ArraySegment<float>(remaindersHost, remainderOffsets[i], remainderLengths[i]);

            quotientResults[i] = new(quotientData.ToArray());
            remainderResults[i] = new(remainderData.ToArray());
        }
        
        return (quotientResults, remainderResults);
    }
    #endregion
    #region Kernels
    public static KernelStorage<Action<Index1D,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>>> BatchedDivideKernel { get; } = new((index, quotients, remainders, aValues, bValues, aLengths, bLengths, aOffsets, bOffsets, outOffsets) =>
    {
        var aLength = (int)aLengths[index];
        var bLength = (int)bLengths[index];
        var aOffset = (int)aOffsets[index];
        var bOffset = (int)bOffsets[index];
        var outOffset = (int)outOffsets[index];
        
        for (int i = 0; i < aLength; i++)
            remainders[outOffset + i] = aValues[aOffset + i];
        
        for (int i = aLength - 1; i >= bLength - 1; i--)
        {
            var topRemainder = (ulong)remainders[outOffset + i] << 32 | remainders[outOffset + i - 1].AsUInt();
            var topDivisor = bValues[bOffset + bLength - 1].AsUInt();
            var qEstimate = (uint)(topRemainder / topDivisor);

            var borrow = 0UL;
            for (int j = 0; j < bLength; j++)
            {
                var product = (ulong)qEstimate * bValues[bOffset + j].AsUInt();
                var diff = (ulong)remainders[outOffset + i - bLength + 1 + j].AsUInt() - (uint)product - borrow;
                remainders[outOffset + i - bLength + 1 + j] = ((uint)diff).AsFloat();
                borrow = (product >> 32) + (diff >> 32 & 1);
            }

            var overestimated = (uint)(-(long)borrow >> 63) & 1;
            var correctedQ = qEstimate - overestimated;
            quotients[outOffset + i - bLength + 1] = correctedQ.AsFloat();

            var carry = 0UL;
            for (int j = 0; j < bLength; j++)
            {
                var bValue = CryptographicOperations.ConstantTime.Select(overestimated, bValues[bOffset + j].AsUInt(), 0U);
                var sum = (ulong)remainders[outOffset + i - bLength + 1 + j].AsUInt() + bValue + carry;
                remainders[outOffset + i - bLength + 1 + j] = ((uint)sum).AsFloat();
                carry = sum >> 32;
            }
        }
    });
    #endregion
}