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
    class Commit
    {
        public int Version;
        public string[] Files;
        public string Comment;
    }

    class Entry
    {
        public string Type; // A,C,D
        public string FilePath;
    }
    class Program
    {
        public static List<Commit> commits = new List<Commit>();
        public const string Local = @"A\";
        public const string Repo = @"R\";
        public const string Meta = @"R\META.TXT";
        // Current state
        public static Dictionary<string, int> VersionMap = new Dictionary<string, int>();
        public static int CurVersion = 0;

        static void ReadMeta()
        {
            using (StreamReader reader = new StreamReader(Meta))
            {
                while(!reader.EndOfStream)
                {
                    int version = int.Parse(reader.ReadLine().Split(':')[1]);
                    string[] files = reader.ReadLine().Split(':')[1].Split(',');
                    string comment = reader.ReadLine();

                    Commit c = new Commit
                    {
                        Version = version,
                        Files = files,
                        Comment = comment
                    };
                    commits.Add(c);
                    CurVersion = version;
                    foreach (string file in files)
                    {
                        string[] filecom = file.Split('#');
                        string type = filecom[0];
                        string path = filecom[1];
                        if (type == "A" || type == "C")
                        {
                            if(VersionMap.ContainsKey(path))
                            {
                                VersionMap[path] = version;
                            }
                            else
                            {
                                VersionMap.Add(path, version);
                            }
                          
                        }
                        else if (type == "D")
                        {
                            VersionMap.Remove(path);
                        }
                    }
                }
                
            }

        }

        static void Main(string[] args)
        {
            ReadMeta();
            //ExternalRun2("AA","DFDF");
            //StartServer();

            while (true)
            {
                string[] command = Console.ReadLine().Split(' ');
                if (command[0] == "Q") break;
                if (command[0] == "IN")
                {
                    In(command[1], command[2]);   
                }
                else if (command[0] == "OUT")
                {
                    Out();
                }
                else if (command[0] == "M")
                {

                }



            }
            //Task task = Task.Delay(num * 1000).ContinueWith(task2 => { Console.WriteLine("OK"); });
            
        }

        // sync
        static void Out()
        {
            Directory.CreateDirectory("TEMP");
            foreach(Commit commit in commits)
            {
                foreach(string file in commit.Files)
                {
                    string[] filecom = file.Split('#');
                    if(filecom[0]=="A" || filecom[0] == "C")
                    {
                        Directory.CreateDirectory(@"TEMP\"+Path.GetDirectoryName(filecom[1]));
                        File.Copy(Repo + commit.Version.ToString().PadLeft(2, '0') + @"\" + filecom[1], @"TEMP\" + filecom[1], true);
                    }
                    else if(filecom[0] == "D")
                    {
                        File.Delete(@"TEMP\" + filecom[1]);
                    }
                }
            }
        }

        static Entry AddFile(string path)
        {
            Console.WriteLine("AddFile : " + path);
            return new Entry { Type = "A", FilePath = path };
        }

        static Entry ChangeFile(string path)
        {
            Console.WriteLine("ChangeFile : " + path);
            bool equal = FileEquals(Local + path, Repo + VersionMap[path].ToString().PadLeft(2, '0')+@"\"+path);
            if(!equal)
            {
                return new Entry { Type = "C", FilePath = path };
            }
            return null;
        }

        static bool FileEquals(string path1, string path2)
        {
            byte[] file1 = File.ReadAllBytes(path1);
            byte[] file2 = File.ReadAllBytes(path2);
            if (file1.Length == file2.Length)
            {
                for (int i = 0; i < file1.Length; i++)
                {
                    if (file1[i] != file2[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        static Entry InFile(string path)
        {
            path = path.Remove(0, Local.Length);
            
            if(VersionMap.ContainsKey(path))
            {
                return ChangeFile(path);
            }
            else
            {
                return AddFile(path);
            }
        }
        

        static List<Entry> InDirectory(string path)
        {
            string underpath = path.Remove(0, Local.Length);
            string[] subfiles = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            List<string> localfiles = subfiles.Select(x => x.Remove(0, Local.Length)).ToList();
            List<string> repofiles = VersionMap.Keys.ToList().FindAll(x => x.StartsWith(underpath));

            List<string> onlylocals = localfiles.Except(repofiles).ToList();
            List<string> onlyrepos = repofiles.Except(localfiles).ToList();
            List<string> intersects = repofiles.Intersect(localfiles).ToList();
            List<Entry> entries = new List<Entry>();
            // Add
            foreach (string onlylocal in onlylocals)
            {
                Entry entry = AddFile(onlylocal);
                entries.Add(entry);
            }

            // Delete
            foreach (string onlyrepo in onlyrepos)
            {
                Entry entry = new Entry { Type = "D", FilePath = onlyrepo };
                entries.Add(entry);
            }

            // Change
            foreach (string intersect in intersects)
            {
                Entry entry = ChangeFile(intersect);
                if(entry != null)
                {
                    entries.Add(entry);
                }
            }
            return entries;
        }

        static void In(string path, string comment)
        {
            string[] files = null;
            if (path == "*")
            {
                files = new string[] { Local };
            }
            else
            {
                files = path.Split(',').Select(x=>Local + x).ToArray();
            }

            List<Entry> entries = new List<Entry>();
            foreach (string file in files)
            {
                if(Directory.Exists(file))
                {
                    // Directory 
                    List<Entry> dirent = InDirectory(file);
                    entries.AddRange(dirent);
                }
                else if(File.Exists(file))
                {
                    // File
                    Entry ent = InFile( file);
                    if(ent != null)
                    {
                        entries.Add(ent);
                    }
                }
                else
                {
                    // not exist
                }

            }
            entries.Sort((Entry x, Entry y) => x.FilePath.CompareTo(y.FilePath));

            // Make
            string[] commitfiles = entries.Select(x => x.Type + "#" + x.FilePath).ToArray();
            int newversion = CurVersion + 1;
            string newversiondir = Repo + newversion.ToString().PadLeft(2, '0');
            Directory.CreateDirectory(newversiondir);

            foreach(Entry entry in entries)
            {
                if (entry.Type == "A" || entry.Type == "C")
                {
                    if (VersionMap.ContainsKey(entry.FilePath))
                    {
                        VersionMap[entry.FilePath] = newversion;
                    }
                    else
                    {
                        VersionMap.Add(entry.FilePath, newversion);
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(newversiondir + @"\" + entry.FilePath));
                    File.Copy(Local + entry.FilePath, newversiondir + @"\" + entry.FilePath);

                }
                else if (entry.Type == "D")
                {
                    VersionMap.Remove(path);
                }
            }
            using (StreamWriter writer = new StreamWriter(Meta, true))
            {
                writer.WriteLine("VER:" + newversion.ToString().PadLeft(2, '0'));
                writer.WriteLine("FILES:" + string.Join(",", commitfiles));
                writer.WriteLine(comment);
            }

            CurVersion = newversion;

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

        public static void ExternalRun2(string id, string arg)
        {
            using (Process pProcess = new Process())
            {
                pProcess.StartInfo.FileName = @"ConsoleApp.exe";
                pProcess.StartInfo.Arguments = "";
                pProcess.StartInfo.UseShellExecute = false;
                pProcess.StartInfo.RedirectStandardOutput = true;
                pProcess.StartInfo.RedirectStandardInput = true;
                pProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                pProcess.StartInfo.CreateNoWindow = true;
                pProcess.Start();
                pProcess.StandardInput.WriteLine(arg);
                string output = pProcess.StandardOutput.ReadLine(); //The output result
                Console.WriteLine("output = " + output);
                pProcess.StandardInput.WriteLine("Q");
                
                

                
                pProcess.WaitForExit();
                
            }
        }
    }
}
