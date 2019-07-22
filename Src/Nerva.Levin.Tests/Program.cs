using System;
using System.Net.Sockets;
using AngryWasp.Logger;
using Nerva.Levin.Requests;
using System.Threading;
using AngryWasp.Helpers;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Nerva.Levin
{
    public static class MainClass
    {
        [STAThread]
        public static void Main(string[] args)
        {
            CommandLineParser cmd = CommandLineParser.Parse(args);
            Log.CreateInstance(true);

            string host = "127.0.0.1";

            if (cmd["host"] != null)
                host = cmd["host"].Value;

            if (cmd["version"] != null)
                Globals.Version = cmd["version"].Value;
            else
            {
                Log.Instance.Write(Log_Severity.Error, "Argument 'version' missing");
                return;
            }

            using (TcpClient tcp = new TcpClient())
            {
                tcp.Connect(host, 17565);
                NetworkStream ns = tcp.GetStream();

                byte[] handshake = new Handshake().Create(17565);

                Log.Instance.Write("Sending handshake");
                ns.Write(handshake, 0, handshake.Length);

                Log.Instance.Write("Waiting for reply");

                Dictionary<Header, Section> data = new Dictionary<Header, Section>();
                Header header;
                Section section;
                while (Read(tcp, out header, out section))
                    data.Add(header, section);
                
                Log.Instance.Write("Closing connection");
                tcp.GetStream().Close();
                tcp.Close();
            }
        }

        private static bool Read(TcpClient tcp, out Header header, out Section section)
        {
            header = null;
            section = null;

            NetworkStream ns = tcp.GetStream();

            byte[] headerBuffer = new byte[33];

            int offset = 0;
            int i = ns.Read(headerBuffer, 0, headerBuffer.Length);
            header = Header.FromBytes(headerBuffer, ref offset);

            if (BitShifter.ToULong(headerBuffer) != Constants.LEVIN_SIGNATURE)
            {
                Log.Instance.Write(Log_Severity.Error, "Invalid response from remote node");
                return false;
            }

            if (i < headerBuffer.Length)
            {
                Log.Instance.Write(Log_Severity.Error, "Invalid response from remote node");
                return false;
            }
            
            offset = 0;
            byte[] buffer = new byte[header.Cb];
            i = ns.Read(buffer, 0, buffer.Length);

            if (i < buffer.Length)
            {
                Log.Instance.Write(Log_Severity.Error, "Invalid response from remote node");
                return false;
            }

            section = null;

            switch (header.Command)
            {
                case Constants.P2P_COMMAND_HANDSHAKE:
                    section = new Handshake().Read(header, buffer, ref offset);
                    break;
                case Constants.P2P_COMMAND_REQUEST_SUPPORT_FLAGS:
                    section = new SupportFlags().Read(header, buffer, ref offset);
                    break;
                default:
                    Log.Instance.Write(Log_Severity.Error, $"Command {header.Command} is not yet supported");
                    return false;
            }

            Log.Instance.Write($"Read data package {header.Command}");

            if (!ns.DataAvailable)
            {
                Log.Instance.Write("Network stream ended");
                return false;
            }

            return true;
        }
    }
}
