using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace Sharpduino.SerialProviders
{
    sealed class TcpPortProvider : ISerialProvider
    {
        private CancellationTokenSource cte = new CancellationTokenSource();

        private StreamWriter clientWriter = null;

        void ISerialProvider.Close()
        {
            cte.Cancel();
        }

        void ISerialProvider.Open()
        {
            Trace.WriteLine("Start");
            int iport;
            var sport = ConfigurationManager.AppSettings["Tcp Provider Port"] ?? "8090";
            if (!int.TryParse(sport, out iport))
                Trace.WriteLine("Unable to read Tcp Provider Port setting: " + sport);

            var tcpListener = new TcpListener(IPAddress.Any, iport);
            tcpListener.Start();
            Task.Factory.StartNew(() =>
            {
                Trace.WriteLine(String.Format("Waiting for remote connections on {0}", tcpListener.LocalEndpoint));

                Task src = null;
                while (!cte.Token.WaitHandle.WaitOne(100))
                {
                    if (tcpListener.Pending())
                    {
                        if (src == null)
                        {
                            Trace.WriteLine("New client");
                            src = Task.Factory.StartNew(() => { ClientHandler(tcpListener.AcceptTcpClient()); })
                                .ContinueWith((t) => { src = null; });
                        }
                        else
                        {
                            // already have a source, reject
                            Trace.WriteLine("rejecting incoming socket, already have a client");
                            var skt = tcpListener.AcceptSocket();
                            skt.Close();
                        }

                    }
                }
                Trace.WriteLine("closing listening socket");
                tcpListener.Stop();
            }, cte.Token);
        }

        private void ClientHandler(TcpClient tcpClient)
        {
            try
            {
                Trace.WriteLine("Remote client connected");

                var clientStream = tcpClient.GetStream();
                using (var reader = new StreamReader(clientStream, System.Text.Encoding.ASCII))
                using (var writer = new StreamWriter(clientStream))
                {
                    writer.AutoFlush = true;

                    // flush anything the client sent. Telnet sends setup data, ignore it.
                    //while (reader.Peek() > -1)
                    //    reader.Read();
                    
                    var buffer = new char[128];
                    while (!cte.Token.IsCancellationRequested && tcpClient.Connected)
                    {
                        try
                        {
                            var count = reader.ReadBlock(buffer, 0, 32);
                            if (count != 0)
                                DoDataReceived(buffer);
                        }
                        catch (IOException e)
                        {
                            Trace.WriteLine(e.Message);
                            break;
                        }
                    }

                    Trace.WriteLine("Client disconnected");
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            finally
            {
                tcpClient.Close();
            }
        }

        private void DoDataReceived(char[] block)
        {
            var dre = DataReceived;
            if (dre != null)
            {
                IEnumerable<Byte> bytes = from b in block select Convert.ToByte(b);
                dre(this, new Sharpduino.EventArguments.DataReceivedEventArgs(bytes));
            }
        }

        public event EventHandler<EventArguments.DataReceivedEventArgs> DataReceived;

        void ISerialProvider.Send(IEnumerable<byte> bytes)
        {
            if (bytes == null)
                return;
            if (clientWriter == null)
                return; // throw away data

            clientWriter.Write(bytes);
        }

        void IDisposable.Dispose()
        {
            //
        }
    }
}
