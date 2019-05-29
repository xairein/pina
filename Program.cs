using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Pina
{
    class Program
    {
        static void Main(string[] args)
        {
            LoadFile("input.txt");
            LoadFolder("Root");
            StartServer();

            while (true)
            {
                string[] command = Console.ReadLine().Split('#');
                if (command[0] == "Q") break;
                if (dict.ContainsKey(command[0]))
                {
                    if (dict[command[0]].Contains(int.Parse(command[1])))
                    {
                        Debug.WriteLine("{0} has perm of {1}", command[0], command[1]);
                        Task extrun = Task.Run(() => { ExternalRun(command[0], command[1]); });
                        //Task.WaitAll(extrun);
                    }
                    else
                    {
                        Debug.WriteLine("{0} has no perm of {1}", command[0], command[1]);
                    }
                }
                else
                {
                    Debug.WriteLine("{0} has no perm", command[0]);
                }
            }
            //Task task = Task.Delay(num * 1000).ContinueWith(task2 => { Console.WriteLine("OK"); });
            
        }

        public static Dictionary<string, HashSet<int>> dict = new Dictionary<string, HashSet<int>>();

        private static void StartServer()
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 1234);
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            serverSocket.Bind(endPoint);
            while (true)
            {
                serverSocket.Listen(10);
                Socket clientSocket = serverSocket.Accept();
                Thread receiveThread = new Thread(() => ReceiveTcpThread(clientSocket)) { IsBackground = true };
                receiveThread.Start();
            }
        }

        private static void ReceiveTcpThread(Socket clientSocket)
        {
            while (true)
            {
                try
                {
                    byte[] data = new byte[1024];
                    int length = clientSocket.Receive(data);
                    if (length == 0)
                    {
                        Debug.WriteLine("Tcp Clinet Socket received null");
                        clientSocket = null;
                        break;
                    }
                    string robotMessage = Encoding.ASCII.GetString(data).TrimEnd('\0');
                    Debug.WriteLine("READ MESSAGE : " + robotMessage);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                    break;
                }
            }
        }

        private static HashSet<int> AddNumbers(string id, string str)
        {
            string[] gates = str.Split(',');
            HashSet<int> gate = new HashSet<int>();
            foreach (string g in gates)
            {
                if (g.Contains("~"))
                {
                    string[] fromto = g.Split('~');
                    int from = int.Parse(fromto[0]);
                    int to = int.Parse(fromto[1]);
                    gate.UnionWith(Enumerable.Range(from, to - from + 1));
                }
                else
                {
                    gate.Add(int.Parse(g));
                }
            }

            if (!dict.ContainsKey(id))
            {
                dict[id] = gate;
            }
            else
            {
                dict[id].UnionWith(gate);
            }

            return gate;
        }

        public static void LoadFile(string path)
        {
            string[] lines = File.ReadAllLines(path);
            foreach(string line in lines)
            {
                string[] perm = line.Split('#');
                AddNumbers(perm[0], perm[1]);
            }
        }

        public static void LoadFolder(string root)
        {
            string[] allpath = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
            foreach(string path in allpath)
            {
                if(path.EndsWith("File.txt"))
                {
                    continue; 
                }
                string fn = Path.GetFileNameWithoutExtension(path);
                string[] trace = path.Split(Path.DirectorySeparatorChar);
                string parent = "";
                for (int i = 0; i < trace.Length - 1; i++)
                {
                    parent += trace[i] + "\\";
                    Debug.WriteLine("File [" + fn + "] From parent : " + parent);
                    string[] lines = File.ReadAllLines(parent + "File.txt");
                    foreach (string line in lines)
                    {
                        string[] perm = line.Split('#');
                        if (perm[0] == "Y")
                        {
                            AddNumbers(fn, perm[1]);
                        }
                        else if (perm[0] == "N")
                        {
                            if (i == trace.Length - 2)
                            {
                                HashSet<int> added = AddNumbers(fn, perm[1]);
                                Debug.WriteLine("Added " + added.ToString());
                            }
                        }
                        else
                        {

                        }

                    }

                }
            }
        }


        public static void ExternalRun(string id, string arg)
        {
            using (Process pProcess = new Process())
            {
                pProcess.StartInfo.FileName = @"Demo.exe";
                pProcess.StartInfo.Arguments = arg; 
                pProcess.StartInfo.UseShellExecute = false;
                pProcess.StartInfo.RedirectStandardOutput = true;
                pProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                pProcess.StartInfo.CreateNoWindow = true; 
                pProcess.Start();
                string output = pProcess.StandardOutput.ReadToEnd(); //The output result
                pProcess.WaitForExit();
                Console.WriteLine("output = " + output);
            }
        }
        
    }
}
