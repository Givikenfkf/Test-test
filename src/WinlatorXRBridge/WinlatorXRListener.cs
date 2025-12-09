using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading;

namespace WinlatorXRBridge
{
    public class WinlatorXRListener : IDisposable
    {
        private UdpClient _udp;
        private Thread _thread;
        private volatile bool _running;
        private readonly int _port;
        private readonly IPEndPoint _bindEndpoint;

        public event Action<XRFrame> OnFrame;
        public event Action<string> OnLog;
        public event Action<Exception> OnError;

        public WinlatorXRListener(int port = 7278)
        {
            _port = port;
            _bindEndpoint = new IPEndPoint(IPAddress.Loopback, _port);
            _running = false;
        }

        public void Start()
        {
            if (_running) return;
            try
            {
                // Bind to loopback explicitly to avoid conflicts with other interfaces
                _udp = new UdpClient(_bindEndpoint);
                _udp.Client.ReceiveBufferSize = 65536;
                _running = true;
                _thread = new Thread(ThreadProc) { IsBackground = true, Name = "WinlatorXRListener" };
                _thread.Start();
                SafeLog("Listener started and bound to " + _bindEndpoint);
            }
            catch (Exception ex)
            {
                SafeError(ex);
                throw;
            }
        }

        public void Stop()
        {
            _running = false;
            try { _udp?.Close(); } catch { }
            try { if (_thread != null && !_thread.Join(500)) _thread?.Abort(); } catch { }
            SafeLog("Listener stopped");
        }

        private void ThreadProc()
        {
            var remoteEP = new IPEndPoint(IPAddress.Any, 0);
            while (_running)
            {
                try
                {
                    // Blocking receive with a short timeout via Available check
                    if (_udp.Available > 0)
                    {
                        var data = _udp.Receive(ref remoteEP); // will receive from remoteEP
                        if (data == null || data.Length == 0) continue;
                        var s = Encoding.UTF8.GetString(data).Trim();
                        if (string.IsNullOrWhiteSpace(s)) continue;
                        ParsePacket(s);
                    }
                    else
                    {
                        Thread.Sleep(2);
                    }
                }
                catch (SocketException)
                {
                    // socket closed or interrupted
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    SafeError(ex);
                }
            }
        }

        private void ParsePacket(string payload)
        {
            try
            {
                // split by whitespace, robust
                var tokens = payload.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 27)
                {
                    SafeLog($"Packet too short ({tokens.Length} tokens). Raw: '{Truncate(payload, 200)}'");
                    return;
                }

                // detect trailing button token (contains T/F letters)
                string buttonStr = "";
                int numericCount = tokens.Length;
                var lastToken = tokens[tokens.Length - 1];
                bool lastLooksButtons = lastToken.Length >= 1 && lastToken.IndexOfAny(new char[] { 'T', 'F', 't', 'f' }) >= 0;
                if (lastLooksButtons)
                {
                    buttonStr = lastToken;
                    numericCount = tokens.Length - 1;
                }

                if (numericCount < 27)
                {
                    SafeLog($"Not enough numeric tokens ({numericCount}). Raw: '{Truncate(payload, 200)}'");
                    return;
                }

                float[] vals = new float[numericCount];
                for (int i = 0; i < numericCount; i++)
                {
                    if (!float.TryParse(tokens[i], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out vals[i]))
                    {
                        SafeLog($"Float parse failed at token {i} ('{tokens[i]}'). Raw: '{Truncate(payload, 200)}'");
                        return;
                    }
                }

                int idx = 0;
                var f = new XRFrame();

                f.LRot = new Quaternion(vals[idx++], vals[idx++], vals[idx++], vals[idx++]);
                f.LThumbX = vals[idx++]; f.LThumbY = vals[idx++];
                f.LPos = new Vector3(vals[idx++], vals[idx++], vals[idx++]);

                f.RRot = new Quaternion(vals[idx++], vals[idx++], vals[idx++], vals[idx++]);
                f.RThumbX = vals[idx++]; f.RThumbY = vals[idx++];
                f.RPos = new Vector3(vals[idx++], vals[idx++], vals[idx++]);

                f.HRot = new Quaternion(vals[idx++], vals[idx++], vals[idx++], vals[idx++]);
                f.HPos = new Vector3(vals[idx++], vals[idx++], vals[idx++]);

                if (idx < vals.Length) f.IPD = vals[idx++];
                if (idx < vals.Length) f.FOVX = vals[idx++];
                if (idx < vals.Length) f.FOVY = vals[idx++];
                if (idx < vals.Length) f.SYNC = vals[idx++];

                f.ButtonStr = buttonStr.Trim();

                // raise event (subscriber should be thread-aware)
                try
                {
                    OnFrame?.Invoke(f);
                }
                catch (Exception ex)
                {
                    SafeError(ex);
                }
            }
            catch (Exception ex)
            {
                SafeError(ex);
            }
        }

        private void SafeLog(string m)
        {
            try { OnLog?.Invoke(m); } catch { }
        }

        private void SafeError(Exception e)
        {
            try { OnError?.Invoke(e); } catch { }
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Length <= max ? s : s.Substring(0, max) + "...";
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
