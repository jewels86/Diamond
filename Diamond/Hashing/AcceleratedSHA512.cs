using System.Text;
using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;
using static Diamond.CryptographicOperations;

namespace Diamond;

public static partial class Hashing
{
    #region Accelerated SHA512 Implementation
    public static byte[][] AcceleratedSHA512Family(byte[][] messages, ulong[] initialHash, int outputBytes, int aidx = -1)
    {
        if (aidx == -1) aidx = Compute.RequestAccelerator();
        
        byte[][] padded = messages.Select(PadSHA512).ToArray();
        int numMessages = messages.Length;
        
        int[] numChunksPerMessage = padded.Select(p => p.Length / 128).ToArray();
        int maxChunks = numChunksPerMessage.Max();

        List<float> allChunksList = [];
        for (int msgIdx = 0; msgIdx < numMessages; msgIdx++)
        {
            var msgFloats = ByteVectorProxy.Roll(padded[msgIdx]);
            allChunksList.AddRange(msgFloats);
        
            int paddingFloats = (maxChunks * 32) - msgFloats.Length;
            allChunksList.AddRange(Enumerable.Repeat(0f, paddingFloats));
        }
        var allChunks = allChunksList.ToArray();

        var allHV = new float[numMessages * 16];
        for (int i = 0; i < numMessages; i++)
            for (int j = 0; j < 8; j++)
            {
                var (f1, f2) = LongAsFloats(initialHash[j]);
                allHV[i * 16 + j * 2] = f1;
                allHV[i * 16 + j * 2 + 1] = f2;
            }
        
        var allW = new float[numMessages * 160];
        
        List<float> kFloats = [];
        foreach (var kVal in RoundConstantsSHA512())
        {
            var (f1, f2) = LongAsFloats(kVal);
            kFloats.Add(f1);
            kFloats.Add(f2);
        }
        var k = kFloats.ToArray();
        
        var numChunksFloat = numChunksPerMessage.Select(x => (float)x).ToArray();
        
        var hvBuffer = Compute.Get(aidx, allHV.Length).Set(allHV);
        var chunksBuffer = Compute.Get(aidx, allChunks.Length).Set(allChunks);
        var wBuffer = Compute.Get(aidx, allW.Length).Set(allW);
        var kBuffer = Compute.Get(aidx, k.Length).Set(k);
        var numChunksBuffer = Compute.Get(aidx, numChunksFloat.Length).Set(numChunksFloat);
        
        for (int chunkIdx = 0; chunkIdx < maxChunks; chunkIdx++)
            Compute.Call(aidx, ProcessChunk512Kernels, numMessages,
                hvBuffer, chunksBuffer, wBuffer, kBuffer, numChunksBuffer, chunkIdx, maxChunks);
        
        Compute.Synchronize(aidx);
        var finalHV = hvBuffer.GetAsArray1D();
        var results = new byte[numMessages][];
        
        for (int i = 0; i < numMessages; i++)
        {
            results[i] = new byte[outputBytes];
            int wordsNeeded = (outputBytes + 7) / 8;
            for (int j = 0; j < wordsNeeded; j++)
            {
                ulong val = FloatsAsLong(finalHV[i * 16 + j * 2], finalHV[i * 16 + j * 2 + 1]);
                int bytesToWrite = Math.Min(8, outputBytes - j * 8);
                for (int h = 0; h < bytesToWrite; h++)
                    results[i][j * 8 + h] = (byte)(val >> (56 - h * 8));
            }
        }
        
        Compute.Return(hvBuffer, chunksBuffer, wBuffer, kBuffer, numChunksBuffer);
        
        Compute.ReleaseAccelerator(aidx);
        return results;
    }
    #endregion
    
    #region SHA512
    public static byte[][] AcceleratedSHA512(byte[][] messages, int aidx = -1) => AcceleratedSHA512Family(messages, InitialHashValuesSHA512(), 64, aidx);
    public static byte[][] AcceleratedSHA512(string[] messages, int aidx = -1) => AcceleratedSHA512(messages.Select(Encoding.UTF8.GetBytes).ToArray(), aidx);
    public static string[] AcceleratedSHA512Hex(string[] messages, int aidx = -1) => AcceleratedSHA512(messages, aidx).Select(HexString).ToArray();
    #endregion
    
    #region Process Chunk Kernels
    private static Action<Index1D,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>,
        int, int>[] ProcessChunk512Kernels { get; } = Compute.Load((Index1D i, 
        ArrayView1D<float, Stride1D.Dense> hv, 
        ArrayView1D<float, Stride1D.Dense> allChunks, 
        ArrayView1D<float, Stride1D.Dense> w, 
        ArrayView1D<float, Stride1D.Dense> k, 
        ArrayView1D<float, Stride1D.Dense> nChunks, int currentChunk, int maxChunks) =>
    {
        if (currentChunk >= (int)nChunks[i]) return;
        
        int chunkBaseIdx = KernelProgramming.MatrixIndexOf(i, currentChunk * 32, maxChunks * 32); // 32 floats per chunk
        for (int j = 0; j < 32; j++) 
            w[(int)i * 160 + j] = allChunks[chunkBaseIdx + j]; // 160 = 80 ulongs * 2 floats per ulong

        for (int j = 16; j < 80; j++) // extend to 80 words
        {
            var wIdx = (int)i * 160 + j * 2; // each ulong takes 2 floats
            var w15 = FloatsAsLong(w[wIdx - 30], w[wIdx - 29]);
            var w2 = FloatsAsLong(w[wIdx - 4], w[wIdx - 3]);
            var w16 = FloatsAsLong(w[wIdx - 32], w[wIdx - 31]);
            var w7 = FloatsAsLong(w[wIdx - 14], w[wIdx - 13]);

            var s0 = LSigma0(w15);
            var s1 = LSigma1(w2);
            var result = w16 + s0 + w7 + s1;
            (w[wIdx], w[wIdx + 1]) = LongAsFloats(result);
        }
        
        int hvIdx = (int)i * 16;
        var a = FloatsAsLong(hv[hvIdx], hv[hvIdx + 1]);
        var b = FloatsAsLong(hv[hvIdx + 2], hv[hvIdx + 3]);
        var c = FloatsAsLong(hv[hvIdx + 4], hv[hvIdx + 5]);
        var d = FloatsAsLong(hv[hvIdx + 6], hv[hvIdx + 7]);
        var e = FloatsAsLong(hv[hvIdx + 8], hv[hvIdx + 9]);
        var f = FloatsAsLong(hv[hvIdx + 10], hv[hvIdx + 11]);
        var g = FloatsAsLong(hv[hvIdx + 12], hv[hvIdx + 13]);
        var h = FloatsAsLong(hv[hvIdx + 14], hv[hvIdx + 15]);

        for (int j = 0; j < 80; j++) // 80 rounds
        {
            var us1 = USigma1(e);
            var ch = Choose(e, f, g);
            var kVal = FloatsAsLong(k[j * 2], k[j * 2 + 1]);
            var wVal = FloatsAsLong(w[(int)i * 160 + j * 2], w[(int)i * 160 + j * 2 + 1]);
            var temp1 = h + us1 + ch + kVal + wVal;
            var us0 = USigma0(a);
            var maj = Majority(a, b, c);
            var temp2 = us0 + maj;

            (h, g, f, e) = (g, f, e, d + temp1);
            (d, c, b, a) = (c, b, a, temp1 + temp2);
        }

        var (aF1, aF2) = LongAsFloats(FloatsAsLong(hv[hvIdx], hv[hvIdx + 1]) + a);
        hv[hvIdx] = aF1; hv[hvIdx + 1] = aF2;
        
        var (bF1, bF2) = LongAsFloats(FloatsAsLong(hv[hvIdx + 2], hv[hvIdx + 3]) + b);
        hv[hvIdx + 2] = bF1; hv[hvIdx + 3] = bF2;
        
        var (cF1, cF2) = LongAsFloats(FloatsAsLong(hv[hvIdx + 4], hv[hvIdx + 5]) + c);
        hv[hvIdx + 4] = cF1; hv[hvIdx + 5] = cF2;
        
        var (dF1, dF2) = LongAsFloats(FloatsAsLong(hv[hvIdx + 6], hv[hvIdx + 7]) + d);
        hv[hvIdx + 6] = dF1; hv[hvIdx + 7] = dF2;
        
        var (eF1, eF2) = LongAsFloats(FloatsAsLong(hv[hvIdx + 8], hv[hvIdx + 9]) + e);
        hv[hvIdx + 8] = eF1; hv[hvIdx + 9] = eF2;
        
        var (fF1, fF2) = LongAsFloats(FloatsAsLong(hv[hvIdx + 10], hv[hvIdx + 11]) + f);
        hv[hvIdx + 10] = fF1; hv[hvIdx + 11] = fF2;
        
        var (gF1, gF2) = LongAsFloats(FloatsAsLong(hv[hvIdx + 12], hv[hvIdx + 13]) + g);
        hv[hvIdx + 12] = gF1; hv[hvIdx + 13] = gF2;
        
        var (hF1, hF2) = LongAsFloats(FloatsAsLong(hv[hvIdx + 14], hv[hvIdx + 15]) + h);
        hv[hvIdx + 14] = hF1; hv[hvIdx + 15] = hF2;
    });
    #endregion
}