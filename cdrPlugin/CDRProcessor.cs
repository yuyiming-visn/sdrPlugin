using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


using SDRSharp.Common;
using SDRSharp.PanView;
using SDRSharp.Radio;


namespace SDRSharp.CDR
{
    public class CDRProcessor : IIQProcessor, IDisposable
    {
        private readonly ISharpControl _control;
        private Timer _processorTimer;

        private const int ProcessorInterval = 100;
        private const int FifoLength = 10000000;
        private readonly ComplexFifoStream _iqStream;

        private double _currentSampleRate = 0;
        private long _currentFrequency = 0;

        private const double SubFrameTime = 0.16;
        private const double CDRSampleRate = 816000;

        private bool _reset_Demod;

        private IntPtr _iqWriter;

        private IntPtr cdrDemod;
        private IntPtr[] draDecoder = new IntPtr[3];
        private IntPtr[] draWave = new IntPtr[3];
        private string[] waveFile = new string[3];

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                for (int i = 0; i < draWave.Length; i++)
                {
                    if (draWave[i] != IntPtr.Zero && draWave[i] != null)
                    {
                        CDRDemodCaller.wav_CloseFile(draWave[i]);
                        draWave[i] = IntPtr.Zero;
                    }
                }

                if (_iqWriter != IntPtr.Zero && _iqWriter != null)
                {
                    CDRDemodCaller.wav_CloseFile(_iqWriter);
                    _iqWriter = IntPtr.Zero;
                }

                for (int i = 0; i < draDecoder.Length; i++)
                {
                    CDRDemodCaller.draDecoder_Release(draDecoder[i]);
                }

                CDRDemodCaller.CDRDemodulation_Release(cdrDemod);
            }
        }

        public CDRProcessor(ISharpControl control)
        {
            _control = control;

            _processorTimer = new Timer();
            _processorTimer.Tick += processorTimer_Tick;
            _processorTimer.Interval = ProcessorInterval;
            _processorTimer.Enabled = false;

            _iqStream = new ComplexFifoStream(BlockMode.None);

            cdrDemod = CDRDemodCaller.CDRDemodulation_Init();
            for (int i = 0; i < draDecoder.Length; i++)
            {
                draDecoder[i] = CDRDemodCaller.draDecoder_Init();
                draWave [i] = IntPtr.Zero;
            }

            _control.RegisterStreamHook(this, ProcessorType.RawIQ);
        }

        private bool _iqRecordEnable;
        public bool IQRecordEnable
        {
            get
            {
                return _iqRecordEnable;
            }
            set
            {
                _iqRecordEnable = value;
                if (_iqWriter != IntPtr.Zero && _iqWriter != null)
                {
                    CDRDemodCaller.wav_CloseFile(_iqWriter);
                    _iqWriter = IntPtr.Zero;
                }

                if (_iqRecordEnable && _processorEnable)
                {
                    // 获取当前时间戳
                    string timestamp = DateTime.Now.ToString("yyyy_MMdd_HHmmss");

                    // 获取当前目录
                    string currentDirectory = Directory.GetCurrentDirectory();

                    // 构造文件名
                    string fileName = $"iq_{timestamp}_fr_{_currentFrequency}_sa_{CDRSampleRate}.wav";

                    // 组合当前目录和文件名
                    string fullPath = Path.Combine(currentDirectory, fileName);

                    _iqWriter = CDRDemodCaller.wav_CreateFile(fullPath, 2, (int)CDRSampleRate, 16);
                }
            }
        }

        private bool _wavRecordEnable;
        public bool WavRecordEnable
        {
            get
            {
                return _wavRecordEnable;
            }
            set
            {
                _wavRecordEnable = value;

                for (int i = 0; i < draWave.Length; i++)
                {
                    if (draWave[i] != IntPtr.Zero && draWave[i] != null)
                    {
                        CDRDemodCaller.wav_CloseFile(draWave[i]);
                        draWave[i] = IntPtr.Zero;
                    }
                }

                if (_wavRecordEnable && _processorEnable)
                {
                    // 获取当前时间戳
                    string timestamp = DateTime.Now.ToString("yyyy_MMdd_HHmmss");
                    // 获取当前目录
                    string currentDirectory = Directory.GetCurrentDirectory();
                    for (int i = 0; i < draWave.Length; i++)
                    {
                        // 构造文件名
                        string fileName = $"audio_{timestamp}_CH{i}.wav";
                        // 组合当前目录和文件名
                        string fullPath = Path.Combine(currentDirectory, fileName);
                        draWave[i] = CDRDemodCaller.wav_CreateFile(fullPath, 1, 24000, 16);
                    }
                }
            }
        }

        private bool _processorEnable;
        public bool Enabled
        {
            get
            {
                return _processorEnable;
            }
            set
            {
                _processorEnable = value;
                _processorTimer.Enabled = _processorEnable;
                _reset_Demod |= _processorEnable;
            }
        }

        public double SampleRate { get; set; }

        public unsafe void Process(Complex* buffer, int length)
        {
            if (!_processorEnable)
            {
                return;
            }

            if (_currentSampleRate != SampleRate || _currentFrequency != _control.Frequency)
            {
                _currentSampleRate = SampleRate;
                _currentFrequency = _control.Frequency;
                _reset_Demod = true;

                _iqStream.Flush();

                if (_wavRecordEnable)
                {
                    WavRecordEnable = false;
                    WavRecordEnable = true;
                }

                if (_iqRecordEnable)
                {
                    IQRecordEnable = false;
                    IQRecordEnable = true;
                }

                CDRDemodCaller.CDRDemodulation_Reset(cdrDemod);
            }

            _iqStream.Write(buffer, length);
        }


        private Complex[] cdrframe = new Complex[(int)(SubFrameTime * CDRSampleRate + 100 )];
        public unsafe void processorTimer_Tick(object sender, EventArgs e)
        {
            int subFrameLength = (int)(SubFrameTime * SampleRate);
            int cdrFrameLength;
            int readLength;
            int error;
            if (_iqStream.Length > subFrameLength)
            {
                Complex[] subFrame = new Complex[subFrameLength];
                fixed (Complex* buffer = subFrame, cdrBuffer = cdrframe)
                {
                    readLength = _iqStream.Read(buffer, 0, subFrameLength);
                    cdrFrameLength = Resample(buffer, readLength, cdrBuffer);
                    Debug.WriteLine("CDRProcessor: Read {0},   Reasmaple {1},    Remain {2},", readLength, cdrFrameLength, _iqStream.Length);

                    _reset_Demod = false;

                    if (_iqRecordEnable && _iqWriter != null && _iqWriter != IntPtr.Zero)
                    {
                        SaveIQFile(cdrBuffer, cdrFrameLength);
                    }

                    error = CDRDemodCaller.CDRDemodulation_Process(cdrDemod, cdrBuffer, cdrFrameLength);
                    if (error == 0)
                    {
                        int num_Programme = CDRDemodCaller.CDRDemodulation_GetNumOfPrograms(cdrDemod);
                        for (int i = 0; i < num_Programme; i++)
                        {
                            int audioLength = 0;
                            byte* tempDra = CDRDemodCaller.CDRDemodulation_GetDraStream(cdrDemod, i, ref audioLength);
                            if (audioLength > 0)
                            {
                                error = CDRDemodCaller.draDecoder_Proccess(draDecoder[i], tempDra, audioLength);
                                if (error == 0)
                                {
                                    short* tempWave = CDRDemodCaller.draDecoder_GetAudioStream(draDecoder[i], ref audioLength);
                                    
                                    if (_wavRecordEnable && draWave[i] != IntPtr.Zero && draWave[i] != null)
                                    {
                                        CDRDemodCaller.wav_WriteShort(draWave[i], tempWave, audioLength);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private short[] _cdr_IQ = new short[2 * (int)(SubFrameTime * CDRSampleRate + 100)];
        private unsafe void SaveIQFile(Complex* cdrframe, int cdrFrameLength)
        {
            for (int i = 0; i < cdrFrameLength; i++)
            {
                _cdr_IQ[2 * i] = (short)(cdrframe[i].Real * 32768);
                _cdr_IQ[2 * i + 1] = (short)(cdrframe[i].Imag * 32768);
            }

            fixed (short* iq_buffer = _cdr_IQ)
            {
                CDRDemodCaller.wav_WriteShort(_iqWriter, iq_buffer, cdrFrameLength * 2);
            }

            Debug.WriteLine("数据已成功写入文件。");
        }

        private Complex lastInputValue = 0;
        private double nextOutputIndex = 0;
        // 线性内插重采样方法
        private unsafe int Resample(Complex* input, int inputLength, Complex* output)
        {
            double ratio = (double)(_currentSampleRate / CDRSampleRate);
            double currentInputIndex = nextOutputIndex;

            int outputLength = (int)Math.Floor((inputLength - currentInputIndex) / ratio);

            for (int i = 0; i < outputLength; i++)
            {
                int leftIndex = (int)Math.Floor(currentInputIndex);
                int rightIndex = leftIndex + 1;

                float fraction = (float)(currentInputIndex - leftIndex);
                Complex leftValue = leftIndex == -1 ? lastInputValue : input[leftIndex];
                Complex rightValue = rightIndex < inputLength ? input[rightIndex] : 0;

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
        public static extern void CDRDemodulation_Release(IntPtr handle);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern int CDRDemodulation_Process(IntPtr handle, Complex* iq, int length);

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
        public static extern int wav_WriteShort(IntPtr handle, short *data, int samples);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern void wav_SetChannels(IntPtr handle, int channels);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern void wav_SetSampleRate(IntPtr handle, int samplerate);
    }

}
