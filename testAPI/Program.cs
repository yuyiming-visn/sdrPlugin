using SDRSharp.CDR;
using System;
using System.Runtime.InteropServices;
using SDRSharp.Radio;
using System.Diagnostics;

namespace SDRSharp.CDR
{
    class Program
    {
        static unsafe void Main(string[] args)
        {
            IntPtr cdrDemod;
            IntPtr[] draDecoder = new IntPtr[3];
            IntPtr[] draWave = new IntPtr[3];
            const double SubFrameTime = 0.16;
            const double CDRSampleRate = 816000;
            const int TestIterations = 100; // Number of test iterations
            Random rand = new Random();

            Console.WriteLine("Starting CDRDemodulation_Process robustness test...");
            cdrDemod = CDRDemodCaller.CDRDemodulation_Init();
            for (int i = 0; i < draDecoder.Length; i++)
            {
                draDecoder[i] = CDRDemodCaller.draDecoder_Init();
                draWave[i] = IntPtr.Zero;
            }

            CDRDemodCaller.CDRDemodulation_Reset(cdrDemod);

            Complex[] cdrframe = new Complex[(int)(SubFrameTime * CDRSampleRate + 100)];
            int cdrFrameLength = 130560;

            // Test loop
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int iter = 0; iter < TestIterations; iter++)
            {
                // Fill cdrframe with random complex numbers
                for (int i = 0; i < cdrframe.Length; i++)
                {
                    cdrframe[i] = new Complex(
                        (float)rand.NextDouble() * 2 - 1, // Real part: -1 to 1
                        (float)rand.NextDouble() * 2 - 1  // Imaginary part: -1 to 1
                    );
                }

                fixed (Complex* cdrBuffer = cdrframe)
                {
                    int error = CDRDemodCaller.CDRDemodulation_Process(cdrDemod, cdrBuffer, cdrFrameLength);
                    int buffer = CDRDemodCaller.CDRDemodulation_GetBufferSize(cdrDemod);
                    Console.WriteLine($"Iteration {iter + 1}/{TestIterations} - error code: {error}, cdrFrameLength: {cdrFrameLength}, buffer: {buffer}");

                }

            }
            stopwatch.Stop();
            TimeSpan elapsed = stopwatch.Elapsed;
            Console.WriteLine($"代码执行时间: {elapsed.TotalSeconds} 秒");

            // Cleanup
            for (int i = 0; i < draDecoder.Length; i++)
            {
                if (draDecoder[i] != IntPtr.Zero)
                    CDRDemodCaller.draDecoder_Release(draDecoder[i]);
            }
            if (cdrDemod != IntPtr.Zero)
                CDRDemodCaller.CDRDemodulation_Release(cdrDemod);

            Console.WriteLine("Robustness test completed.");
        }
    }

    public unsafe class CDRDemodCaller
    {
        private const string dllPath = @"libcdrRelease.dll";
        const CallingConvention callingConvertion = CallingConvention.Cdecl;

        public enum LDPCRate { Rate1_4 = 0, Rate1_3 = 1, Rate1_2 = 2, Rate3_4 = 3 }
        public enum TransmissionMode { TransMode1 = 1, TransMode2 = 2, TransMode3 = 3 }
        public enum SpectrumType { SpecMode1 = 1, SpecMode2 = 2, SpecMode9 = 9, SpecMode10 = 10, SpecMode22 = 22, SpecMode23 = 23 }
        public enum QamType { QPSK = 0, QAM16 = 1, QAM64 = 2 }

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern IntPtr CDRDemodulation_Init();

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern void CDRDemodulation_Release(IntPtr handle);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern int CDRDemodulation_Process(IntPtr handle, Complex* iq, int length);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern int CDRDemodulation_GetBufferSize(IntPtr handle);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern byte* CDRDemodulation_GetDraStream(IntPtr handle, int channel, ref int length);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern void CDRDemodulation_Reset(IntPtr handle);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern void CDRDemodulation_SetTransferMode(IntPtr handle, TransmissionMode transferMode);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern void CDRDemodulation_SetSpectrumMode(IntPtr handle, SpectrumType spectrumMode);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern int CDRDemodulation_GetNumOfPrograms(IntPtr handle);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern QamType CDRDemodulation_GetSDISModType(IntPtr handle);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern QamType CDRDemodulation_GetMSDSModType(IntPtr handle);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern LDPCRate CDRDemodulation_GetLDPCRate1(IntPtr handle);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern double CDRDemodulation_GetFrequencyOffset(IntPtr handle);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern double CDRDemodulation_GetSampleRateOffset(IntPtr handle);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern IntPtr draDecoder_Init();

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern void draDecoder_Release(IntPtr pDraHandle);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern int draDecoder_Proccess(IntPtr pDraHandle, byte* inputBuffer, int inputLength);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern short* draDecoder_GetAudioStream(IntPtr handle, ref int streamLength);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern int draDecoder_GetMaxInputDataSize(IntPtr handle);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern int draDecoder_GetAudioChannels(IntPtr pDraHandle);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern int draDecoder_GetAudioSampleRate(IntPtr pDraHandle);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern IntPtr wav_CreateFile(string filename, int channels, int samplerate, int samplebits);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern void wav_CloseFile(IntPtr handle);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern int wav_WriteShort(IntPtr handle, short* data, int samples);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern void wav_SetChannels(IntPtr handle, int channels);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern void wav_SetSampleRate(IntPtr handle, int samplerate);
    }
}