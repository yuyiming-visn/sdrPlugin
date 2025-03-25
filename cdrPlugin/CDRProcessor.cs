using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using SDRSharp.Common;
using SDRSharp.PanView;
using SDRSharp.Radio;

namespace SDRSharp.CDR
{
    public class CDRProcessor : IIQProcessor
    {
        private const int ProcessorInterval = 100;
        private const int FifoLength = 10000000;

        private const double SubFrameTime = 0.16;
        private double _currentSampleRate = 0;
        private const double CDRSampleRate = 816000;

        private readonly ISharpControl _control;
        private Timer _processorTimer;

        private readonly ComplexFifoStream _iqStream;

        private IntPtr cdrDemod;
        private IntPtr[] draDecoder = new IntPtr [3];


        public CDRProcessor(ISharpControl control)
        {
            _control = control;

            cdrDemod = CDRDemodCaller.CDRDemodulation_Init();
            for (int i = 0; i < draDecoder.Length; i++)
            {
                draDecoder[i] = CDRDemodCaller.draDecoder_Init();
            }


            #region FFT Timer

            _processorTimer = new Timer();
            _processorTimer.Tick += processorTimer_Tick;
            _processorTimer.Interval = ProcessorInterval;

            #endregion

            #region Buffers

            _iqStream = new ComplexFifoStream(BlockMode.None);

            // InitFFTBuffers();
            // BuildFFTWindow();

            #endregion

            _control.RegisterStreamHook(this, ProcessorType.RawIQ);
            _processorTimer.Enabled = true;

        }

        public bool Enabled { get; set; }

        public double SampleRate { get; set; }

        public unsafe void Process(Complex* buffer, int length)
        {
            if (_currentSampleRate != SampleRate)
            {
                _iqStream.Flush();
                _currentSampleRate = SampleRate;
            }

            _iqStream.Write(buffer, length);

            //Debug.WriteLine("CDRProcessor: Write Remain {0} samples", _iqStream.Length);
            //Debug.WriteLine("CDRProcessor: SampleRate {0}", SampleRate);
        }

        public unsafe void processorTimer_Tick(object sender, EventArgs e)
        {
            int subFrameLength = (int)(SubFrameTime * SampleRate);
            int cdrFrameLength = (int)(SubFrameTime * CDRSampleRate);
            int readLength;
            int error;
            if (_iqStream.Length > subFrameLength)
            {
                Complex [] subFrame = new Complex[subFrameLength];
                Complex [] cdrframe = new Complex[cdrFrameLength+100];
                fixed (Complex* buffer = subFrame, cdrBuffer = cdrframe)
                {
                    readLength = _iqStream.Read(buffer, 0, subFrameLength);
                    cdrFrameLength = Resample(buffer, readLength, cdrBuffer);

                    Debug.WriteLine("CDRProcessor: Read {0},   Reasmaple {1},    Remain {2},", readLength, cdrFrameLength, _iqStream.Length);

                    error = CDRDemodCaller.CDRDemodulation_Process(cdrDemod, cdrBuffer, cdrFrameLength);
                    if (error != 0)
                    {
                        int num_Programme = CDRDemodCaller.CDRDemodulation_GetNumOfPrograms(cdrDemod);
                        for (int i = 0; i < num_Programme; i++)
                        {
                            int tempLength = 0;
                            IntPtr tempStream = CDRDemodCaller.CDRDemodulation_GetDraStream(cdrDemod, i, ref tempLength);
                            if (tempLength > 0)
                            {
                                error = CDRDemodCaller.draDecoder_Proccess(draDecoder[i], tempStream, tempLength);
                                if (error != 0)
                                {
                                    IntPtr tempWave = CDRDemodCaller.draDecoder_GetAudioStream(draDecoder[i], ref tempLength);
                                }
                            }
                        }
                    }
                }
            }
        }

        private Complex lastInputValue  = 0;
        private double nextOutputIndex = 0;
        // 线性内插重采样方法
        public unsafe int Resample(Complex* input, int inputLength, Complex* output)
        {
            double ratio = (double)(_currentSampleRate / CDRSampleRate);
            double currentInputIndex = nextOutputIndex;

            int outputLength = (int)Math.Floor((inputLength - currentInputIndex) / ratio);
        
            for (int i = 0; i < outputLength; i++)
            {
                int leftIndex = (int)Math.Floor(currentInputIndex);
                int rightIndex = leftIndex + 1;

                float fraction = (float)(currentInputIndex - leftIndex);
                Complex leftValue  = leftIndex == -1 ? lastInputValue : input[leftIndex];
                Complex rightValue = rightIndex < inputLength ? input[rightIndex]:0;

                // 线性内插公式
                output[i].Real = (1 - fraction) * leftValue.Real + fraction * rightValue.Real;
                output[i].Imag = (1 - fraction) * leftValue.Imag + fraction * rightValue.Imag;

                currentInputIndex += ratio;
            }

            lastInputValue = input[inputLength - 1];
            nextOutputIndex = currentInputIndex - inputLength;

            return outputLength;
        }
    }

    public unsafe class CDRDemodCaller
    {
        private const string dllPath = @"libcdrRelease.dll";

        const CallingConvention callingConvertion = CallingConvention.Cdecl;

        /* LDPCCodeRate */
        public enum LDPCRate { Rate1_4 = 0, Rate1_3 = 1, Rate1_2 = 2, Rate3_4 = 3 };

        /* 传输模式 */
        public enum TransmissionMode { TransMode1 = 1, TransMode2 = 2, TransMode3 = 3 };

        /* 频谱模式 */
        public enum SpectrumType { SpecMode1 = 1, SpecMode2 = 2, SpecMode9 = 9, SpecMode10 = 10, SpecMode22 = 22, SpecMode23 = 23 };

        /* QAM */
        public enum QamType { QPSK = 0, QAM16 = 1, QAM64 = 2, };

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern IntPtr CDRDemodulation_Init();

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern int CDRDemodulation_Process(IntPtr handle, Complex *iq, int length);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern IntPtr CDRDemodulation_GetDraStream(IntPtr handle, int channel, ref int length);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern void CDRDemodulation_Release(IntPtr handle);


        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern int CDRDemodulation_GetNumOfPrograms(IntPtr handle);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern QamType CDRDemodulation_GetSDISModType(IntPtr handle);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern QamType CDRDemodulation_GetMSDSModType(IntPtr handle);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern LDPCRate CDRDemodulation_GetLDPCRate1(IntPtr handle);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern void CDRDemodulation_SetTransferMode(IntPtr handle, TransmissionMode transferMode);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern void CDRDemodulation_SetSpectrumMode(IntPtr handle, SpectrumType spectrumMode);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern double CDRDemodulation_GetFrequencyOffset(IntPtr handle);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern double CDRDemodulation_GetSampleRateOffset(IntPtr handle);



        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern IntPtr draDecoder_Init();

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern void draDecoder_Release(IntPtr pDraHandle);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern int draDecoder_Proccess(IntPtr pDraHandle, IntPtr inputBuffer, int inputLength);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern IntPtr draDecoder_GetAudioStream(IntPtr handle, ref int streamLength);

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
        public static extern int wav_WriteShort(IntPtr handle, IntPtr data, int samples);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern void wav_SetChannels(IntPtr handle, int channels);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern void wav_SetSampleRate(IntPtr handle, int samplerate);
    }

}
