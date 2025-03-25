using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        public CDRProcessor(ISharpControl control)
        {
            _control = control;

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
            if (_iqStream.Length > subFrameLength)
            {
                Complex [] subFrame = new Complex[subFrameLength];
                Complex [] cdrframe = new Complex[cdrFrameLength+100];
                fixed (Complex* buffer = subFrame, cdrBuffer = cdrframe)
                {
                    readLength = _iqStream.Read(buffer, 0, subFrameLength);
                    cdrFrameLength = Resample (buffer, readLength, cdrBuffer);
                }
                Debug.WriteLine("CDRProcessor: Read {0},   Reasmaple {1},    Remain {2},", readLength, cdrFrameLength,  _iqStream.Length);
               // Debug.WriteLine("CDRProcessor: Read {0},   Reasmaple {1},    next {2},", readLength, cdrFrameLength, nextOutputIndex);
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
}
