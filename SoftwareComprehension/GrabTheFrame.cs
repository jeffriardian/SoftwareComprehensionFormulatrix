using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace GrabTheFrame
{
    public interface IFrameCallback
    {
        public void FrameReceived(IntPtr pFrame, int pixelWidth, int pixelHeight);
    }

    public interface IValueReporter
    {
        public void Report(double value);
    }

    public class FrameCalculateAndStream
    {
        private IValueReporter _reporter;
        private Queue<Frame> _receivedFrames = new Queue<Frame>();
        private Timer _timer;

        public FrameCalculateAndStream(FrameGrabber fg, IValueReporter vr)
        {
            fg.OnFrameUpdated += HandleFrameUpdated;
            _timer = new Timer(1000 / 30);
            _timer.Elapsed += OnTimerElapsed;
            _reporter = vr;
        }

        private void HandleFrameUpdated(Frame frame)
        {
            _receivedFrames.Enqueue(frame);
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (_receivedFrames.Count > 0)
            {
                Frame frame = _receivedFrames.Dequeue();
                byte[] raw = frame.GetRawData();

                // https://stackoverflow.com/questions/29312223/finding-the-arithmetic-mean-of-an-array-c-sharp
                int sum = 0;

                for (int i = 0; i < raw.Length; i++)
                    sum += raw[i];

                int result = sum / raw.Length; // result now has the average of those numbers.
                _reporter.Report(result);
            }
        }

        public void StartStreaming()
        {
            _timer.Enabled = true;
        }
    }

    public class FrameGrabber : IFrameCallback
    {
        private byte[] _buffer;
        private byte[] _buffer2;

        public delegate void FrameUpdateHandler(Frame rawFrame);
        public event FrameUpdateHandler OnFrameUpdated;

        public void FrameReceived(IntPtr frame, int width, int height)
        {
            //Based on references of https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.marshal.copy?view=net-7.0#system-runtime-interopservices-marshal-copy(system-intptr-system-byte()-system-int32-system-int32)
            // Copy the array to unmanaged memory.
            Marshal.Copy(_buffer, 0, frame, width * height);

            // Copy the unmanaged array back to another managed array.
            if (_buffer == null)
                _buffer2 = new byte[width * height];

            // https://stackoverflow.com/questions/5486938/c-sharp-how-to-get-byte-from-intptr
            Marshal.Copy(frame, _buffer2, 0, width * height);
            Frame bufferedFrame = new Frame(_buffer2);
            OnFrameUpdated(bufferedFrame);
            bufferedFrame.Dispose();
        }
    }

    public class Frame : IDisposable
    {
        private bool _disposed;
        private byte[] _rawBuffer;

        public Frame(byte[] raw)
        {
            _rawBuffer = raw;
        }

        public byte[] GetRawData()
        {
            if (_disposed)
                throw new ObjectDisposedException("underlying buffer has changed, should not be used anymore");
            return _rawBuffer;
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
