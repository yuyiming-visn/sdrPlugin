// #define cdrDemod

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private FileWriter _iqWriter;
#if cdrDemod
        private IntPtr cdrDemod;
        private IntPtr[] draDecoder = new IntPtr [3];
#endif

        public CDRProcessor(ISharpControl control)
        {
            _control = control;

            _processorTimer = new Timer();
            _processorTimer.Tick += processorTimer_Tick;
            _processorTimer.Interval = ProcessorInterval;
            _processorTimer.Enabled = false;

            _iqStream = new ComplexFifoStream(BlockMode.None);

            _iqWriter = new FileWriter();
#if cdrDemod
            cdrDemod = CDRDemodCaller.CDRDemodulation_Init();
            for (int i = 0; i < draDecoder.Length; i++)
            {
                draDecoder[i] = CDRDemodCaller.draDecoder_Init();
            }
#endif

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
                if (!_iqRecordEnable)
                {
                    _iqWriter.CloseFile();
                }
                else 
                { 
                    _iqWriter.Reset(_currentSampleRate, _currentFrequency); 
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
                if (IQRecordEnable)
                {
                    _iqWriter.Reset(_currentSampleRate, _currentFrequency);
                }
            }

            _iqStream.Write(buffer, length);


            //Debug.WriteLine("CDRProcessor: Write Remain {0} samples", _iqStream.Length);
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
                    _reset_Demod = false;
                    if (IQRecordEnable)
                    {
                        // _iqWriter.WriteData(cdrframe, cdrFrameLength);
                        _iqWriter.WriteData(subFrame, readLength);
                    }
#if cdrDemod
                    
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

#endif
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

    public class FileWriter : IDisposable
    {
        private FileStream _fileStream;
        private string _filePath;
        public static string GenerateFileName(double sampleRate, long frequency)
        {
            // 获取当前时间戳
            string timestamp = DateTime.Now.ToString("yyyy_MMdd_HHmmss");

            // 获取当前目录
            string currentDirectory = Directory.GetCurrentDirectory();

            // 构造文件名
            string fileName = $"{timestamp}_fr_{frequency}_Sa_{(int)(sampleRate)}.bin";

            // 组合当前目录和文件名
            string fullPath = Path.Combine(currentDirectory, fileName);

            return fullPath;
        }

        public FileWriter()
        {
        }

        public void Reset(double sampleRate, long frequency)
        {
            CloseFile();
            OpenFile(GenerateFileName(sampleRate, frequency));
        }

        public void OpenFile(string filePath)
        {
            try
            {
                _filePath = filePath;
                _fileStream = new FileStream(_filePath, FileMode.Create, FileAccess.Write);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"打开文件时发生错误: {ex.Message}");
            }
        }

        public unsafe void WriteData(Complex[] subFrame, int validLength)
        {
            if (_fileStream == null || !_fileStream.CanWrite)
            {
                Console.WriteLine("文件未打开或不可写，无法写入数据。");
                return;
            }

            try
            {                
                byte[] buffer = new byte[validLength * 4];
                short i_data = 0;
                short q_data = 0;
                byte* ptr_i = (byte*)(&i_data);
                byte* ptr_q = (byte*)(&q_data);
                fixed (byte* ptr_byte = buffer)
                {
                    byte* ptr = ptr_byte;
                    for (int i = 0; i < validLength; i++)
                    {
                        i_data = (short)(subFrame[i].Real * 32768);
                        q_data = (short)(subFrame[i].Imag * 32768);
                        *ptr = *ptr_i;
                        ptr++;
                        *ptr = *(ptr_i+1);
                        ptr++;
                        *ptr = *ptr_q;
                        ptr++;
                        *ptr = *(ptr_q + 1);
                        ptr++;
                    }
                }
                _fileStream.Write(buffer, 0, buffer.Length);

#if _floatFile

                int length = subFrame.Length;
                int complexSize = sizeof(Complex);

                int byteCount = length * complexSize;
                byte[] buffer = new byte[byteCount];
                fixed (Complex* ptr = subFrame)
                {
                    byte* bytePtr = (byte*)ptr;
                    for (int i = 0; i < byteCount; i++)
                    {
                        buffer[i] = *bytePtr;
                        bytePtr++;
                    }
                }


                _fileStream.Write(buffer, 0, byteCount);
#endif
                Console.WriteLine("数据已成功写入文件。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"写入文件时发生错误: {ex.Message}");
            }
        }

        public void CloseFile()
        {
            if (_fileStream != null)
            {
                try
                {
                    _fileStream.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"关闭文件时发生错误: {ex.Message}");
                }
                finally
                {
                    _fileStream.Dispose();
                    _fileStream = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                CloseFile();
            }
        }
    }




#if cdrDemod
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
#endif
}
