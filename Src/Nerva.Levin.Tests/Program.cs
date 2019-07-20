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

                Log.Instance.SetColor(ConsoleColor.DarkCyan);

                Log.Instance.Write("Sending handshake");
                ns.Write(handshake, 0, handshake.Length);

                Log.Instance.SetColor(ConsoleColor.Yellow);
                Log.Instance.Write("Waiting for reply");
                Log.Instance.SetColor(ConsoleColor.Green);

                byte[] buffer = new byte[1024*1024];

                Dictionary<Header, Section> results = new Dictionary<Header, Section>();

                while (true)
                {
                    if (!ns.DataAvailable)
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    int i = ns.Read(buffer, 0, 1024*1024);

                    Log.Instance.Write($"Received {i} bytes");

                    if (BitShifter.ToULong(buffer) != Constants.LEVIN_SIGNATURE)
                    {
                        Log.Instance.Write(Log_Severity.Error, "Invalid response from remote node");
                        break;
                    }

                    bool exit = false;
                    int offset = 0;

                    while (!exit)
                    {
                        Header h = Header.FromBytes(buffer, ref offset);
                        Section s = null;

                        switch (h.Command)
                        {
                            case Constants.P2P_COMMAND_HANDSHAKE:
                                s = new Handshake().Read(h, buffer, ref offset);
                                break;
                            case Constants.P2P_COMMAND_REQUEST_SUPPORT_FLAGS:
                                s = new SupportFlags().Read(h, buffer, ref offset);
                                break;
                            default:
                                throw new NotSupportedException($"Command {h.Command} is not yet supported");
                        }

                        results.Add(h, s);

                        if (offset >= i)
                        {
                            exit = true;
                            break;
                        }
                    }
                    
                    if (exit)
                        break;
                }

                Log.Instance.SetColor(ConsoleColor.DarkCyan);
                Log.Instance.Write("Closing connection");
                tcp.GetStream().Close();
                tcp.Close();

                foreach (var r in results)
                {
                    if (r.Key.Command == 1001)
                        Console.WriteLine(JsonConvert.SerializeObject(r.Value.Entries["local_peerlist_new"]));
                }
            }
        }
    }
}
