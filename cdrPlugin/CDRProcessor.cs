#define AUDIO_OUTPUT

using System;
using System.CodeDom.Compiler;
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
    public class CDRProcessor : IRealProcessor, IIQProcessor, IDisposable
    {
        private readonly ISharpControl _control;

        private const int ProcessorInterval = 100;
        private Timer _iq_Timer;
        private readonly ComplexFifoStream _iq_Stream;
        private readonly FloatFifoStream _audio_Stream;

        private const double SubFrameTime = 0.16;
        private const double CDRSampleRate = 816000;
        private const double AudioSampleRate = 24000;

        private bool _reset_Demod;

        private IntPtr _cdr_iqWriter;
        private IntPtr _cdr_Demod;
        private IntPtr[] _dra_Decoder = new IntPtr[3];
        private IntPtr[] _dra_waveFile = new IntPtr[3];
        private string[] _dra_waveName = new string[3];

        private IntPtr _iq_filter;
        private IntPtr _audio_filter;
        private float _filter_attenuation = 60.0f;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                for (int i = 0; i < _dra_waveFile.Length; i++)
                {
                    if (_dra_waveFile[i] != IntPtr.Zero && _dra_waveFile[i] != null)
                    {
                        CDRDemodCaller.wav_CloseFile(_dra_waveFile[i]);
                        _dra_waveFile[i] = IntPtr.Zero;
                    }
                }

                if (_cdr_iqWriter != IntPtr.Zero && _cdr_iqWriter != null)
                {
                    CDRDemodCaller.wav_CloseFile(_cdr_iqWriter);
                    _cdr_iqWriter = IntPtr.Zero;
                }

                for (int i = 0; i < _dra_Decoder.Length; i++)
                {
                    CDRDemodCaller.draDecoder_Release(_dra_Decoder[i]);
                }

                CDRDemodCaller.CDRDemodulation_Release(_cdr_Demod);

                if (_iq_filter != IntPtr.Zero && _iq_filter != null)
                {
                    LiquidDspCaller.msresamp_cccf_destroy(_iq_filter);
                    _iq_filter = IntPtr.Zero;
                }

                if (_audio_filter != IntPtr.Zero && _audio_filter != null)
                {
                    LiquidDspCaller.msresamp_rrrf_destroy(_audio_filter);
                    _audio_filter = IntPtr.Zero;
                }
            }
        }

        public CDRProcessor(ISharpControl control)
        {
            _iq_filter = IntPtr.Zero;
            _audio_filter = IntPtr.Zero;


            _cdr_Demod = CDRDemodCaller.CDRDemodulation_Init();
            for (int i = 0; i < _dra_Decoder.Length; i++)
            {
                _dra_Decoder[i] = CDRDemodCaller.draDecoder_Init();
                _dra_waveFile[i] = IntPtr.Zero;
            }

            _control = control;

            _iq_Timer = new Timer();
            _iq_Timer.Tick += iq_TimerTick;
            _iq_Timer.Interval = ProcessorInterval;
            _iq_Timer.Enabled = false;
            _iq_Stream = new ComplexFifoStream(BlockMode.None);

            _audio_Stream = new FloatFifoStream(BlockMode.None);

            _control.RegisterStreamHook(this, ProcessorType.FilteredAudioOutput);
            _control.RegisterStreamHook(this, ProcessorType.RawIQ);
        }


        private CDRPanel _cdr_UI;
        public CDRPanel CDR_UI
        {
            set { _cdr_UI = value; }
            get { return _cdr_UI; }
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
                if (_cdr_iqWriter != IntPtr.Zero && _cdr_iqWriter != null)
                {
                    CDRDemodCaller.wav_CloseFile(_cdr_iqWriter);
                    _cdr_iqWriter = IntPtr.Zero;
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

                    _cdr_iqWriter = CDRDemodCaller.wav_CreateFile(fullPath, 2, (int)CDRSampleRate, 16);
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

                for (int i = 0; i < _dra_waveFile.Length; i++)
                {
                    if (_dra_waveFile[i] != IntPtr.Zero && _dra_waveFile[i] != null)
                    {
                        CDRDemodCaller.wav_CloseFile(_dra_waveFile[i]);
                        _dra_waveFile[i] = IntPtr.Zero;
                    }
                }

                if (_wavRecordEnable && _processorEnable)
                {
                    // 获取当前时间戳
                    string timestamp = DateTime.Now.ToString("yyyy_MMdd_HHmmss");
                    // 获取当前目录
                    string currentDirectory = Directory.GetCurrentDirectory();
                    for (int i = 0; i < _dra_waveFile.Length; i++)
                    {
                        // 构造文件名
                        string fileName = $"audio_{timestamp}_CH{i}.wav";
                        // 组合当前目录和文件名
                        string fullPath = Path.Combine(currentDirectory, fileName);
                        _dra_waveFile[i] = CDRDemodCaller.wav_CreateFile(fullPath, 1, (int)AudioSampleRate, 16);
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
                _iq_Timer.Enabled = _processorEnable;
                _reset_Demod |= _processorEnable;
            }
        }

        private double _currentSampleRate = 0;
        private long _currentFrequency = 0;
        private double _iq_SampleRate;
        public double SampleRate
        {
            get { return _iq_SampleRate; }
            set
            {
                if (value != _control.AudioSampleRate)
                    _iq_SampleRate = value;
                Debug.WriteLine("SampleRate: {0}, audio {1}", _iq_SampleRate, _control.AudioSampleRate);
            }
        }

        private int _audioProgramID = -100;
        public int AudioProgramID
        {
            set { _audioProgramID = value; }
        }


        #region 音频处理
        double _currentAudioSa = 0;
        private float[] audioWaveTemp = new float[(int)(0.1 * 100000 * 2)];
        public unsafe void Process(float* buffer, int length)
        {
            if (!_processorEnable)
            {
                return;
            }

#if AUDIO_OUTPUT
            if (_currentAudioSa != _control.AudioSampleRate)
            {
                if (_audio_filter != IntPtr.Zero && _audio_filter != null)
                {
                    LiquidDspCaller.msresamp_rrrf_destroy(_audio_filter);
                }
                float resampleRatio = (float)(_control.AudioSampleRate / AudioSampleRate);
                _audio_filter = LiquidDspCaller.msresamp_rrrf_create(resampleRatio, _filter_attenuation);
            }


            Debug.WriteLine($"audio: {_control.AudioSampleRate}, {length}, {_audio_Stream.Length}");

            fixed (float* audioBuffer = audioWaveTemp)
            {
                int actual_audio_Length = _audio_Stream.Read(audioBuffer, 0, length/2);
                if (_audioProgramID > 0)
                {
                    
                }
            }
#endif
        }


        #endregion

        #region IQ处理
        public unsafe void Process(Complex* buffer, int length)
        {
            if (!_processorEnable)
            {
                return;
            }

            if (_currentSampleRate != _iq_SampleRate || _currentFrequency != _control.Frequency)
            {
                _currentSampleRate = _iq_SampleRate;
                _currentFrequency = _control.Frequency;
                _reset_Demod = true;

                _iq_Stream.Flush();

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

                CDRDemodCaller.CDRDemodulation_Reset(_cdr_Demod);

                if (_iq_filter != IntPtr.Zero && _iq_filter != null)
                {
                    LiquidDspCaller.msresamp_cccf_destroy(_iq_filter);
                }
                float resampleRatio = (float)( CDRSampleRate / _iq_SampleRate);
                _iq_filter = LiquidDspCaller.msresamp_cccf_create(resampleRatio, _filter_attenuation);
            }

            _iq_Stream.Write(buffer, length);
        }

        private float[] audioWave = new float[(int)(SubFrameTime * AudioSampleRate * 16 + 100)];
        private Complex[] cdrframe = new Complex[(int)(SubFrameTime * CDRSampleRate + 100)];
        int cdrFrameLength;
        private float[] audioOutput = new float[(int)(SubFrameTime * 100000 * 16 + 100)];
        public unsafe void iq_TimerTick(object sender, EventArgs e)
        {
            int subFrameLength = (int)(SubFrameTime * _currentSampleRate);
           
            int actual_iq_Length;
            int error;
            if (_iq_Stream.Length > subFrameLength)
            {
                Complex[] subFrame = new Complex[subFrameLength];
                fixed (Complex* iqBuffer = subFrame, cdrBuffer = cdrframe)
                {
                    actual_iq_Length = _iq_Stream.Read(iqBuffer, 0, subFrameLength);
                    // cdrFrameLength = Resample(iqBuffer, actual_iq_Length, cdrBuffer);
                    error = LiquidDspCaller.msresamp_cccf_execute(_iq_filter, iqBuffer, actual_iq_Length, cdrBuffer, ref cdrFrameLength);                    

                    _reset_Demod = false;

                    if (_iqRecordEnable && _cdr_iqWriter != null && _cdr_iqWriter != IntPtr.Zero)
                    {
                        SaveIQFile(cdrBuffer, cdrFrameLength);
                    }

                    int bufferSize = CDRDemodCaller.CDRDemodulation_GetBufferSize(_cdr_Demod);
                    error = CDRDemodCaller.CDRDemodulation_Process(_cdr_Demod, cdrBuffer, cdrFrameLength);

                    DateTime currentTime = DateTime.Now;
                    Debug.WriteLine($"{currentTime}: Read {actual_iq_Length},   Reasmaple {cdrFrameLength},    Remain {_iq_Stream.Length},  Buffer {bufferSize}");

                    _cdr_UI.Sync_Done = (error == 0 || error == 10021) ? true : false;

                    if (error == 0)
                    {
                        int nameLength = 0;
                        int freq = 0;
                        byte[] Name = new byte[256];
                        int netID = CDRDemodCaller.CDRDemodulation_GetNetID(_cdr_Demod, Name, ref nameLength, ref freq);
                        string netName = Encoding.UTF8.GetString(Name, 0, nameLength);
                        _cdr_UI.NetID = $"{netID}::{netName}::{freq / 1000}kHz";

                        int num_Programme = CDRDemodCaller.CDRDemodulation_GetNumOfPrograms(_cdr_Demod);
                        for (int i = 0; i < num_Programme; i++)
                        {
                            int draLength = 0;
                            byte* tempDra = CDRDemodCaller.CDRDemodulation_GetDraStream(_cdr_Demod, i, ref draLength);
                            if (draLength > 0)
                            {
                                error = CDRDemodCaller.draDecoder_Proccess(_dra_Decoder[i], tempDra, draLength);
                                if (error == 0)
                                {
                                    int audioLength = 0;
                                    short* tempWave = CDRDemodCaller.draDecoder_GetAudioStream(_dra_Decoder[i], ref audioLength);

                                    if (_wavRecordEnable && _dra_waveFile[i] != IntPtr.Zero && _dra_waveFile[i] != null)
                                    {
                                        CDRDemodCaller.wav_WriteShort(_dra_waveFile[i], tempWave, audioLength);
                                    }
#if AUDIO_OUTPUT
                                    if (_audioProgramID == i)
                                    {
                                        fixed (float* audioBuffer = audioWave, auBuffer = audioOutput)
                                        {
                                            int audioOutputLength = 0;
                                            for (int j = 0; j < audioLength; j++)
                                            {
                                                audioWave[j] = tempWave[j] / 32768;
                                            }
                                            error = LiquidDspCaller.msresamp_rrrf_execute(_audio_filter, audioBuffer, audioLength, auBuffer, ref audioOutputLength);
                                            _audio_Stream.Write(auBuffer, audioOutputLength);
                                            Debug.WriteLine($"Resample: {_control.AudioSampleRate}, {_audio_Stream.Length}");
                                        }
                                    }
#endif
                                }
                            }
                        }
                    }
                }
            }
        }

        #region IQ文件保存
        private short[] _record_iqArray = new short[2 * (int)(SubFrameTime * CDRSampleRate + 100)];
        private unsafe void SaveIQFile(Complex* cdrframe, int cdrFrameLength)
        {
            for (int i = 0; i < cdrFrameLength; i++)
            {
                _record_iqArray[2 * i] = (short)(cdrframe[i].Real * 32768);
                _record_iqArray[2 * i + 1] = (short)(cdrframe[i].Imag * 32768);
            }

            fixed (short* iq_buffer = _record_iqArray)
            {
                CDRDemodCaller.wav_WriteShort(_cdr_iqWriter, iq_buffer, cdrFrameLength * 2);
            }

            Debug.WriteLine("数据已成功写入文件。");
        }
        #endregion

        #region 线性内插重采样方法
        private Complex _resample_lastInputValue = 0;
        private double _resample_nextOutputIndex = 0;
        private unsafe int Resample(Complex* input, int inputLength, Complex* output)
        {
            double ratio = (double)(_currentSampleRate / CDRSampleRate);
            double currentInputIndex = _resample_nextOutputIndex;

            int outputLength = (int)Math.Floor((inputLength - currentInputIndex) / ratio);

            for (int i = 0; i < outputLength; i++)
            {
                int leftIndex = (int)Math.Floor(currentInputIndex);
                int rightIndex = leftIndex + 1;

                float fraction = (float)(currentInputIndex - leftIndex);
                Complex leftValue = leftIndex == -1 ? _resample_lastInputValue : input[leftIndex];
                Complex rightValue = rightIndex < inputLength ? input[rightIndex] : 0;

                // 线性内插公式
                output[i].Real = (1 - fraction) * leftValue.Real + fraction * rightValue.Real;
                output[i].Imag = (1 - fraction) * leftValue.Imag + fraction * rightValue.Imag;

                currentInputIndex += ratio;
            }

            _resample_lastInputValue = input[inputLength - 1];
            _resample_nextOutputIndex = currentInputIndex - inputLength;

            return outputLength;
        }
        #endregion

        #endregion
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
        public static extern int CDRDemodulation_GetBufferSize(IntPtr handle);

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
        public static extern int CDRDemodulation_GetNetID(IntPtr handle, [Out] byte[] Name, ref int nameLength, ref int freq);


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

    public unsafe class LiquidDspCaller
    {
        private const string dllPath = @"libLiquid.dll";

        const CallingConvention callingConvertion = CallingConvention.Cdecl;

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern IntPtr msresamp_cccf_create(float _r, float _as);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern int msresamp_cccf_destroy(IntPtr _q);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern int msresamp_cccf_execute(IntPtr _q, Complex* _x, int _nx, Complex* _y, ref int _ny);


        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern IntPtr msresamp_rrrf_create(float _r, float _as);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern int msresamp_rrrf_destroy(IntPtr _q);

        [DllImport(dllPath, CallingConvention = callingConvertion, SetLastError = false)]
        public static extern int msresamp_rrrf_execute(IntPtr _q, float* _x, int _nx, float* _y, ref int _ny);
    }
}