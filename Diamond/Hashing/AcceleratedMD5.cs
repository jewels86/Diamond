using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;
using static Diamond.CryptographicOperations;

namespace Diamond;

public static partial class Hashing
{
    #region Accelerated MD5
    public static byte[][] AcceleratedMD5(byte[][] messages, int aidx = -1)
    {
        if (aidx == -1) aidx = Compute.RequestOptimalAccelerator();
        
        byte[][] padded = messages.Select(PadMD5).ToArray();
        int numMessages = messages.Length;
        
        int[] numChunksPerMessage = padded.Select(p => p.Length / 64).ToArray();
        int maxChunks = numChunksPerMessage.Max();

        List<float> allChunksList = [];
        for (int msgIdx = 0; msgIdx < numMessages; msgIdx++)
        {
            var paddedMsg = padded[msgIdx];
            var msgUints = new uint[paddedMsg.Length / 4];
            for (int j = 0; j < msgUints.Length; j++)
            {
                msgUints[j] = paddedMsg[j * 4]
                              | (uint)paddedMsg[j * 4 + 1] << 8
                              | (uint)paddedMsg[j * 4 + 2] << 16
                              | (uint)paddedMsg[j * 4 + 3] << 24;
            }
            var msgFloats = msgUints.Select(Interop.IntAsFloat).ToArray();
            allChunksList.AddRange(msgFloats);
        
            int paddingFloats = maxChunks * 16 - msgFloats.Length;
            allChunksList.AddRange(Enumerable.Repeat(0f, paddingFloats));
        }
        var allChunks = allChunksList.ToArray();
        
        var allHV = new float[numMessages * 4];
        var initialHash = InitialHashValuesMD5();
        for (int i = 0; i < numMessages; i++)
            for (int j = 0; j < 4; j++)
                allHV[i * 4 + j] = Interop.IntAsFloat(initialHash[j]);
        
        var allM = new float[numMessages * 16];
        var k = RoundConstantsMD5().Select(Interop.IntAsFloat).ToArray();
        var numChunksFloat = numChunksPerMessage.Select(x => (float)x).ToArray();
        
        var messageIndex = MessageIndexPatternsMD5().Select(i => (float)i).ToArray();
        var shifts = ShiftsMD5().Select(i => (float)i).ToArray();
        
        var hvBuffer = Compute.Get(aidx, allHV.Length).Set(allHV);
        var chunksBuffer = Compute.Get(aidx, allChunks.Length).Set(allChunks);
        var mBuffer = Compute.Get(aidx, allM.Length).Set(allM);
        var kBuffer = Compute.Get(aidx, k.Length).Set(k);
        var numChunksBuffer = Compute.Get(aidx, numChunksFloat.Length).Set(numChunksFloat);
        var messageIndexBuffer = Compute.Get(aidx, messageIndex.Length).Set(messageIndex);
        var shiftsBuffer = Compute.Get(aidx, shifts.Length).Set(shifts);
        
        for (int chunkIdx = 0; chunkIdx < maxChunks; chunkIdx++)
            Compute.Call(aidx, ProcessChunkMD5Kernels, numMessages,
                hvBuffer, chunksBuffer, mBuffer, kBuffer, numChunksBuffer, 
                messageIndexBuffer, shiftsBuffer, chunkIdx, maxChunks);
        
        Compute.Synchronize(aidx);
        var finalHV = hvBuffer.GetAsArray1D();
        var results = new byte[numMessages][];
        
        for (int i = 0; i < numMessages; i++)
        {
            results[i] = new byte[16];
            for (int j = 0; j < 4; j++)
            {
                uint val = Interop.FloatAsInt(finalHV[i * 4 + j]);
                results[i][j * 4] = (byte)val;
                results[i][j * 4 + 1] = (byte)(val >> 8);
                results[i][j * 4 + 2] = (byte)(val >> 16);
                results[i][j * 4 + 3] = (byte)(val >> 24);
            }
        }
        
        Compute.Return(hvBuffer, chunksBuffer, mBuffer, kBuffer, numChunksBuffer, 
            messageIndexBuffer, shiftsBuffer);
        return results;
    }
    public static byte[][] AcceleratedMD5(string[] messages, int aidx = -1) => AcceleratedMD5(messages.Select(FromString).ToArray(), aidx);
    public static string[] AcceleratedMD5Hex(string[] messages, int aidx = -1) => AcceleratedMD5(messages, aidx).Select(HexString).ToArray();
    #endregion
    
    #region Process Chunk Kernels
    public static KernelStorage<Action<Index1D, 
        ArrayView1D<float, Stride1D.Dense>, ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>, ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>, ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>, int, int>> ProcessChunkMD5Kernels { get; } =
        new((i, hv, allChunks, m, k, nChunks, messageIndex, shifts, currentChunk, maxChunks) =>
        {
            if (currentChunk >= (int)nChunks[i]) return;
    
            // load message words (already little-endian)
            int chunkBaseIdx = KernelProgramming.MatrixIndexOf(i, currentChunk * 16, maxChunks * 16);
            for (int j = 0; j < 16; j++) 
                m[(int)i * 16 + j] = allChunks[chunkBaseIdx + j];
    
            int hvIdx = i * 4;
            var a = Interop.FloatAsInt(hv[hvIdx]);
            var b = Interop.FloatAsInt(hv[hvIdx + 1]);
            var c = Interop.FloatAsInt(hv[hvIdx + 2]);
            var d = Interop.FloatAsInt(hv[hvIdx + 3]);

            for (int j = 0; j < 64; j++)
            {
                uint f;
                if (j < 16) f = F(b, c, d);
                else if (j < 32) f = G(b, c, d);
                else if (j < 48) f = H(b, c, d);
                else f = I(b, c, d);

                var kVal = Interop.FloatAsInt(k[j]);
                var mVal = Interop.FloatAsInt(m[i * 16 + (int)messageIndex[j]]);
                f = f + a + kVal + mVal;
        
                a = d;
                d = c;
                c = b;
                b = b + LeftRotate(f, (int)shifts[j]);
            }

            hv[hvIdx] = Interop.IntAsFloat(Interop.FloatAsInt(hv[hvIdx]) + a);
            hv[hvIdx + 1] = Interop.IntAsFloat(Interop.FloatAsInt(hv[hvIdx + 1]) + b);
            hv[hvIdx + 2] = Interop.IntAsFloat(Interop.FloatAsInt(hv[hvIdx + 2]) + c);
            hv[hvIdx + 3] = Interop.IntAsFloat(Interop.FloatAsInt(hv[hvIdx + 3]) + d);
        });
    #endregion
}