using System.Text;
using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;
using static Diamond.CryptographicOperations;

namespace Diamond;

public static partial class Hashing
{
    #region Accelerated SHA256 Implementation
    public static byte[][] AcceleratedSHA256Family(byte[][] messages, uint[] initialHash, int outputBytes, int aidx = -1)
    {
        if (aidx == -1) aidx = Compute.RequestAccelerator();
        
        byte[][] padded = messages.Select(PadSHA256).ToArray();
        int numMessages = messages.Length;
        
        int[] numChunksPerMessage = padded.Select(p => p.Length / 64).ToArray();
        int maxChunks = numChunksPerMessage.Max();

        List<float> allChunksList = [];
        for (int msgIdx = 0; msgIdx < numMessages; msgIdx++)
        {
            var msgFloats = ByteVectorProxy.Roll(padded[msgIdx]);
            allChunksList.AddRange(msgFloats);
        
            int paddingFloats = (maxChunks * 16) - msgFloats.Length;
            allChunksList.AddRange(Enumerable.Repeat(0f, paddingFloats));
        }
        var allChunks = allChunksList.ToArray();
        
        var allHV = new float[numMessages * 8];
        for (int i = 0; i < numMessages; i++)
            for (int j = 0; j < 8; j++)
                allHV[i * 8 + j] = Interop.IntAsFloat(initialHash[j]);
        
        var allW = new float[numMessages * 64];
        var k = RoundConstantsSHA256().Select(Interop.IntAsFloat).ToArray();
        var numChunksFloat = numChunksPerMessage.Select(x => (float)x).ToArray();
        
        var hvBuffer = Compute.Get(aidx, allHV.Length).Set(allHV);
        var chunksBuffer = Compute.Get(aidx, allChunks.Length).Set(allChunks);
        var wBuffer = Compute.Get(aidx, allW.Length).Set(allW);
        var kBuffer = Compute.Get(aidx, k.Length).Set(k);
        var numChunksBuffer = Compute.Get(aidx, numChunksFloat.Length).Set(numChunksFloat);
        
        for (int chunkIdx = 0; chunkIdx < maxChunks; chunkIdx++)
            Compute.Call(aidx, ProcessChunk256Kernels, numMessages,
                hvBuffer, chunksBuffer, wBuffer, kBuffer, numChunksBuffer, chunkIdx, maxChunks);
        
        Compute.Synchronize(aidx);
        var finalHV = hvBuffer.GetAsArray1D();
        var results = new byte[numMessages][];
        
        for (int i = 0; i < numMessages; i++)
        {
            results[i] = new byte[outputBytes];
            int wordsNeeded = (outputBytes + 3) / 4;
            for (int j = 0; j < wordsNeeded; j++)
            {
                uint val = Interop.FloatAsInt(finalHV[i * 8 + j]);
                int bytesToWrite = Math.Min(4, outputBytes - j * 4);
                for (int h = 0; h < bytesToWrite; h++)
                    results[i][j * 4 + h] = (byte)(val >> (24 - h * 8));
            }
        }
        
        Compute.Return(hvBuffer, chunksBuffer, wBuffer, kBuffer, numChunksBuffer);
        
        Compute.ReleaseAccelerator(aidx);
        return results;
    }
    #endregion

    #region SHA256
    public static byte[][] AcceleratedSHA256(byte[][] messages, int aidx = -1) => AcceleratedSHA256Family(messages, InitialHashValuesSHA256(), 32, aidx);
    public static byte[][] AcceleratedSHA256(string[] messages, int aidx = -1) => AcceleratedSHA256(messages.Select(Encoding.UTF8.GetBytes).ToArray(), aidx);
    public static string[] AcceleratedSHA256Hex(string[] messages, int aidx = -1) => AcceleratedSHA256(messages, aidx).Select(HexString).ToArray();
    #endregion
    #region SHA224
    public static byte[][] AcceleratedSHA224(byte[][] messages, int aidx = -1) => AcceleratedSHA256Family(messages, InitialHashValuesSHA224(), 28, aidx);
    public static byte[][] AcceleratedSHA224(string[] messages, int aidx = -1) => AcceleratedSHA224(messages.Select(Encoding.UTF8.GetBytes).ToArray(), aidx);
    public static string[] AcceleratedSHA224Hex(string[] messages, int aidx = -1) => AcceleratedSHA224(messages, aidx).Select(x => BitConverter.ToString(x).Replace("-", "").ToLower()).ToArray();
    #endregion
    
    #region Process Chunk Kernels
    private static Action<Index1D,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>,
        int, int>[] ProcessChunk256Kernels { get; } = Compute.Load((Index1D i, 
        ArrayView1D<float, Stride1D.Dense> hv, 
        ArrayView1D<float, Stride1D.Dense> allChunks, 
        ArrayView1D<float, Stride1D.Dense> w, 
        ArrayView1D<float, Stride1D.Dense> k, 
        ArrayView1D<float, Stride1D.Dense> nChunks, int currentChunk, int maxChunks) =>
    {
        if (currentChunk >= (int)nChunks[i]) return;
        
        int chunkBaseIdx = KernelProgramming.MatrixIndexOf(i, currentChunk * 16, maxChunks * 16);
        for (int j = 0; j < 16; j++)
        {
            w[(int)i * 64 + j] = allChunks[chunkBaseIdx + j];
        }

        for (int j = 16; j < 64; j++)
        {
            var wIdx = (int)i * 64 + j;
            var w15 = Interop.FloatAsInt(w[wIdx - 15]);
            var w2 = Interop.FloatAsInt(w[wIdx - 2]);
            var w16 = Interop.FloatAsInt(w[wIdx - 16]);
            var w7 = Interop.FloatAsInt(w[wIdx - 7]);

            var s0 = LSigma0(w15);
            var s1 = LSigma1(w2);
            w[wIdx] = Interop.IntAsFloat(w16 + s0 + w7 + s1);
        }
        
        int hvIdx = (int)i * 8;
        var a = Interop.FloatAsInt(hv[hvIdx]);
        var b = Interop.FloatAsInt(hv[hvIdx + 1]);
        var c = Interop.FloatAsInt(hv[hvIdx + 2]);
        var d = Interop.FloatAsInt(hv[hvIdx + 3]);
        var e = Interop.FloatAsInt(hv[hvIdx + 4]);
        var f = Interop.FloatAsInt(hv[hvIdx + 5]);
        var g = Interop.FloatAsInt(hv[hvIdx + 6]);
        var h = Interop.FloatAsInt(hv[hvIdx + 7]);

        for (int j = 0; j < 64; j++)
        {
            var us1 = USigma1(e);
            var ch = Choose(e, f, g);
            var kVal = Interop.FloatAsInt(k[j]);
            var wVal = Interop.FloatAsInt(w[(int)i * 64 + j]);
            var temp1 = h + us1 + ch + kVal + wVal;
            var us0 = USigma0(a);
            var maj = Majority(a, b, c);
            var temp2 = us0 + maj;

            (h, g, f, e) = (g, f, e, d + temp1);
            (d, c, b, a) = (c, b, a, temp1 + temp2);
        }

        hv[hvIdx] = Interop.IntAsFloat(Interop.FloatAsInt(hv[hvIdx]) + a);
        hv[hvIdx + 1] = Interop.IntAsFloat(Interop.FloatAsInt(hv[hvIdx + 1]) + b);
        hv[hvIdx + 2] = Interop.IntAsFloat(Interop.FloatAsInt(hv[hvIdx + 2]) + c);
        hv[hvIdx + 3] = Interop.IntAsFloat(Interop.FloatAsInt(hv[hvIdx + 3]) + d);
        hv[hvIdx + 4] = Interop.IntAsFloat(Interop.FloatAsInt(hv[hvIdx + 4]) + e);
        hv[hvIdx + 5] = Interop.IntAsFloat(Interop.FloatAsInt(hv[hvIdx + 5]) + f);
        hv[hvIdx + 6] = Interop.IntAsFloat(Interop.FloatAsInt(hv[hvIdx + 6]) + g);
        hv[hvIdx + 7] = Interop.IntAsFloat(Interop.FloatAsInt(hv[hvIdx + 7]) + h);
    });
    #endregion
}