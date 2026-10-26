using System;
using System.IO;
using System.Text;

namespace CloneZ
{
    /// <summary>Writes 16-bit PCM WAV files: the sound board's export and the offline sound tools. Engine-free.</summary>
    public static class WavFile
    {
        /// <summary>Writes interleaved samples in [-1, 1] (clamped) as a 16-bit WAV.</summary>
        public static void Write(string path, float[] interleaved, int channels, int rate)
        {
            if (interleaved == null) throw new ArgumentNullException(nameof(interleaved));
            channels = Math.Max(1, channels);
            int dataBytes = interleaved.Length * 2;
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var w = new BinaryWriter(fs))
            {
                w.Write(Encoding.ASCII.GetBytes("RIFF"));
                w.Write(36 + dataBytes);
                w.Write(Encoding.ASCII.GetBytes("WAVE"));
                w.Write(Encoding.ASCII.GetBytes("fmt "));
                w.Write(16);
                w.Write((short)1);
                w.Write((short)channels);
                w.Write(rate);
                w.Write(rate * channels * 2);
                w.Write((short)(channels * 2));
                w.Write((short)16);
                w.Write(Encoding.ASCII.GetBytes("data"));
                w.Write(dataBytes);
                for (int i = 0; i < interleaved.Length; i++)
                {
                    float v = interleaved[i];
                    v = v > 1f ? 1f : v < -1f ? -1f : v;
                    w.Write((short)Math.Round(v * 32767f));
                }
            }
        }
    }
}
