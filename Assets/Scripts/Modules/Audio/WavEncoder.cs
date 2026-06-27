using System;
using System.Text;
using UnityEngine;

namespace Ludocore
{
    /// <summary>Encodes a float sample buffer to an in-memory 16-bit PCM WAV byte array. No file I/O.</summary>
    public static class WavEncoder
    {
        public static byte[] EncodePcm16(float[] samples, int channels, int sampleRate)
        {
            if (samples == null) samples = Array.Empty<float>();
            int dataSize = samples.Length * 2;
            int byteRate = sampleRate * channels * 2;
            byte[] wav = new byte[44 + dataSize];

            // RIFF header
            Encoding.ASCII.GetBytes("RIFF").CopyTo(wav, 0);
            WriteInt(wav, 4, 36 + dataSize);
            Encoding.ASCII.GetBytes("WAVE").CopyTo(wav, 8);

            // fmt subchunk (PCM)
            Encoding.ASCII.GetBytes("fmt ").CopyTo(wav, 12);
            WriteInt(wav, 16, 16);
            WriteShort(wav, 20, 1);                          // audio format = PCM
            WriteShort(wav, 22, (short)channels);
            WriteInt(wav, 24, sampleRate);
            WriteInt(wav, 28, byteRate);
            WriteShort(wav, 32, (short)(channels * 2));      // block align
            WriteShort(wav, 34, 16);                         // bits per sample

            // data subchunk
            Encoding.ASCII.GetBytes("data").CopyTo(wav, 36);
            WriteInt(wav, 40, dataSize);

            int offset = 44;
            for (int i = 0; i < samples.Length; i++)
            {
                float v = Mathf.Clamp(samples[i], -1f, 1f);
                short s = (short)Mathf.RoundToInt(v * 32767f);
                wav[offset++] = (byte)(s & 0xff);
                wav[offset++] = (byte)((s >> 8) & 0xff);
            }
            return wav;
        }

        private static void WriteInt(byte[] buf, int o, int v)
        {
            buf[o]   = (byte)(v & 0xff);
            buf[o+1] = (byte)((v >> 8) & 0xff);
            buf[o+2] = (byte)((v >> 16) & 0xff);
            buf[o+3] = (byte)((v >> 24) & 0xff);
        }

        private static void WriteShort(byte[] buf, int o, short v)
        {
            buf[o]   = (byte)(v & 0xff);
            buf[o+1] = (byte)((v >> 8) & 0xff);
        }
    }
}
