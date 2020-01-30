using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace hashcheck
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Hash Check v0.2");
                Console.WriteLine("Usage: hashcheck [action] [target]");
                Console.WriteLine("");
                Console.WriteLine("-cf\t--create-file\t[filename]\t\tCreates a check file");
                Console.WriteLine("-vf\t--verify-file\t[filename]\t\tChecks a check file");
                Console.WriteLine("-uf\t--update-file\t[filename]\t\tUpdates a check file (Removes missing, adds new)");
            }
            if (args.Length == 3)
            {
                if (args[0] == "-cf" || args[0] == "--create-file")
                {
                    if (!Directory.Exists(args[2]))
                    {
                        Console.WriteLine("Directory Not Found.");
                        return 1;
                    }
                    DoWork(args[1], args[2], Mode.Create);
                }
                if (args[0] == "-vf" || args[0] == "--verify-file")
                {
                    if (!File.Exists(args[1]))
                    {
                        Console.WriteLine("Check File Not Found.");
                        return 1;
                    }
                    if (!Directory.Exists(args[2]))
                    {
                        Console.WriteLine("Directory Not Found.");
                        return 1;
                    }
                    DoWork(args[1], args[2], Mode.Verify);
                }
                if (args[0] == "-uf" || args[0] == "--update-file")
                {
                    if (!File.Exists(args[1]))
                    {
                        Console.WriteLine("Check File Not Found.");
                        return 1;
                    }
                    if (!Directory.Exists(args[2]))
                    {
                        Console.WriteLine("Directory Not Found.");
                        return 1;
                    }
                    DoWork(args[1], args[2], Mode.Update);
                }
            }
            return 0;
        }

        enum Mode
        {
            Create,
            Update,
            Verify
        }

        static void SLC(string Output, bool EndWithNewLine = false)
        {
            Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r");
            if (EndWithNewLine)
            {
                Console.WriteLine(Output);
            }
            else
            {
                Console.Write(Output);
            }
        }

        static void DoWork(string InputFile, string Folder, Mode RunningMode)
        {
            Console.WriteLine("Getting File List...");
            List<string> FileList = GetAllFiles(Folder);
            Console.WriteLine();
            List<Data> FromCheckFile = new List<Data>();
            if (RunningMode == Mode.Update || RunningMode == Mode.Verify)
            {
                Console.WriteLine("Loading Check File...");
                using (FileStream InFile = new FileStream(InputFile, mode: FileMode.Open))
                {
                    using (StreamReader InStream = new StreamReader(InFile))
                    {
                        int LineIndex = 0;
                        while (!InStream.EndOfStream)
                        {
                            string Line = InStream.ReadLine();
                            string[] Sections = Line.Split("\t");
                            if (Sections.Length == 2)
                            {
                                Data x = new Data();
                                x.CreateTime = Sections[0].Substring(0, 14);
                                x.LastWrite = Sections[0].Substring(14, 14);
                                x.SHA1Hash = Sections[0].Substring(28, 40);
                                x.FileSize = Convert.ToInt64(Sections[0].Substring(68));
                                x.FileName = Folder + Path.DirectorySeparatorChar + Sections[1].Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
                                x.Index = LineIndex;
                                int TestLength = Encoding.UTF8.GetBytes(("Loaded: " + x.FileName)).Length;
                                if (TestLength >= Console.WindowWidth)
                                {
                                    string TestString = ("Loaded: " + x.FileName);
                                    int Diff = TestLength - Console.WindowWidth;
                                    SLC(TestString.Substring(0, TestString.Length - Diff));
                                }
                                else
                                {
                                    SLC("Loaded: " + x.FileName);
                                }
                                FromCheckFile.Add(x);
                                LineIndex++;
                            }
                        }
                        SLC("Finished Loading Check.", true);
                    }
                }
                for (int i = 0; i < FromCheckFile.Count; i++)
                {
                    bool FileExistsInFileSystem = FileList.Contains(FromCheckFile[i].FileName);
                    if (!FileExistsInFileSystem)
                    {
                        if (RunningMode == Mode.Update)
                        {
                            SLC(FromCheckFile[i].FileName + " -- Does not exist in file system, removing entry!", true);
                            Data x = FromCheckFile[i];
                            x.FileName = "--DELETED--";
                            FromCheckFile[i] = x;
                        }
                        else
                        {
                            SLC(FromCheckFile[i].FileName + " -- Does not exist in file system", true);
                        }
                    }
                }
            }
            int Padding = FileList.Count.ToString().Length;
            string RightSide = "/" + FileList.Count.ToString().PadLeft(Padding);
            for (int i = 0; i < FileList.Count; i++)
            {
                FileInfo CurrentFile = new FileInfo(FileList[i]);
                Data CheckFile = new Data();
                if (RunningMode == Mode.Update || RunningMode == Mode.Verify)
                {
                    CheckFile = FromCheckFile.Where(t => t.FileName == FileList[i]).FirstOrDefault();
                }
                string CreationTime = CurrentFile.CreationTime.ToUniversalTime().ToString("yyyyMMddHHmmss");
                string LastWriteTime = CurrentFile.LastWriteTime.ToUniversalTime().ToString("yyyyMMddHHmmss");
                long FileSize = CurrentFile.Length;
                string FileCount = i.ToString().PadLeft(Padding) + RightSide;
                if (CheckFile.FileName != null)
                {
                    if (RunningMode == Mode.Update)
                    {
                        Data CurrentEntry = FromCheckFile[Convert.ToInt32(CheckFile.Index)];
                        bool FileChanged = false;
                        if (CheckFile.CreateTime != CreationTime)
                        {
                            Console.WriteLine(i + " -- Creation Time Changed. Updating Entry!");
                            CurrentEntry.CreateTime = CreationTime;
                            FileChanged = true;
                        }
                        if (CheckFile.LastWrite != LastWriteTime)
                        {
                            Console.WriteLine(i + " -- Last Write Time Changed. Updating Entry!");
                            CurrentEntry.LastWrite = LastWriteTime;
                            FileChanged = true;
                        }
                        if (CheckFile.FileSize != FileSize)
                        {
                            SLC(i + " -- Wrong File Size. Updating Entry!", true);
                            CurrentEntry.FileSize = FileSize;
                            FileChanged = true;
                        }
                        if (FileChanged == true)
                        {
                            string s = GetHash(FileList[i], FileCount);
                            CurrentEntry.SHA1Hash = s;
                            FromCheckFile[Convert.ToInt32(CheckFile.Index)] = CurrentEntry;
                        }
                    }
                    if (RunningMode == Mode.Verify)
                    {
                        if (CheckFile.CreateTime != CreationTime)
                        {
                            Console.WriteLine(i + " -- Creation Time Changed");
                        }
                        if (CheckFile.LastWrite != LastWriteTime)
                        {
                            Console.WriteLine(i + " -- Last Write Time Changed");
                        }
                        if (CheckFile.FileSize != FileSize)
                        {
                            SLC(i + " -- Wrong File Size, skipping SHA1", true);
                        }
                        else
                        {
                            string s = GetHash(FileList[i], FileCount);
                            if (CheckFile.SHA1Hash != s)
                            {
                                SLC(i + " -- Hashes do not match", true);
                            }
                        }
                    }
                }
                else
                {
                    void CreateEntry()
                    {
                        Data PushtoArray = new Data();
                        PushtoArray.CreateTime = CreationTime;
                        PushtoArray.FileName = FileList[i];
                        PushtoArray.FileSize = FileSize;
                        PushtoArray.LastWrite = LastWriteTime;
                        PushtoArray.SHA1Hash = GetHash(FileList[i], FileCount);
                        FromCheckFile.Add(PushtoArray);
                    }
                    if (RunningMode == Mode.Create)
                    {
                        CreateEntry();
                    }
                    if (RunningMode == Mode.Update)
                    {
                        SLC(i + " -- Does not exist in Check file. Adding!", true);
                        CreateEntry();
                    }
                    if (RunningMode == Mode.Verify)
                    {
                        SLC(i + " -- Does not exist in Check file", true);
                    }
                }
            }
            void CreateCheckFile()
            {
                using (FileStream OutFile = new FileStream(InputFile + ".temp", mode: FileMode.Create))
                {
                    using (StreamWriter OutPut = new StreamWriter(OutFile))
                    {
                        foreach (Data i in FromCheckFile)
                        {
                            if (i.FileName != "--DELETED--")
                            {
                                OutPut.WriteLine($"{i.CreateTime}{i.LastWrite}{i.SHA1Hash}{i.FileSize}\t{i.FileName.Replace(Folder + Path.DirectorySeparatorChar, "")}");
                                OutPut.Flush();
                            }
                        }
                    }
                }
                if (File.Exists(InputFile + ".bak")) { File.Delete(InputFile + ".bak"); }
                if (File.Exists(InputFile + ".hash")) { File.Move(InputFile, InputFile + ".bak"); }
                File.Move(InputFile + ".temp", InputFile);
            }
            if (RunningMode == Mode.Update)
            {
                Console.WriteLine("Rebuilding Check File");
                CreateCheckFile();
            }
            if (RunningMode == Mode.Create)
            {
                Console.WriteLine("Creating Check File");
                CreateCheckFile();
            }
            Console.WriteLine("Finished.");
            Console.WriteLine();
            Console.WriteLine();
        }

        struct Data
        {
            public int Index { get; set; }
            public string FileName { get; set; }
            public string LastWrite { get; set; }
            public string CreateTime { get; set; }
            public long FileSize { get; set; }
            public string SHA1Hash { get; set; }
        }

        private static List<string> GetAllFiles(string startLocation)
        {
            List<string> Files = new List<string>();
            Files.AddRange(Directory.GetFiles(startLocation));
            foreach (string directory in Directory.GetDirectories(startLocation))
            {
                Files.AddRange(GetAllFiles(directory));
                SLC(Files.Count + " : Files Found");
            }
            return Files;
        }

        static string GetHash(string Filename, string FileStatus)
        {
            string FileStatusBase = "  0.00% " + FileStatus + " Hashing: " + Path.GetFileName(Filename);
            int TestLength = Encoding.UTF8.GetBytes(FileStatusBase).Length;
            if (TestLength >= Console.WindowWidth)
            {
                int Diff = TestLength - (Console.WindowWidth);
                SLC(FileStatusBase.Substring(0, FileStatusBase.Length - Diff));
            }
            else
            {
                SLC(FileStatusBase);
            }
            StringBuilder s = new StringBuilder();
            byte[] ResultBytes;
            using (FileStream SourceStream = File.Open(Filename, FileMode.Open))
            {
                SHA1 x = new SHA1Managed();
                ResultBytes = ComputeHash(SourceStream, x, Filename);
            }
            foreach (byte b in ResultBytes)
            {
                s.Append(b.ToString("x2").ToUpper());
            }
            return s.ToString();
        }

        static byte[] ComputeHash(Stream stream, HashAlgorithm hashAlgorithm, string Filename)
        {
            const int bufferLength = 0x1000;
            byte[] buffer = new byte[bufferLength * 2 + 1];
            int readAheadBytesRead, offset;
            long TotalLength = stream.Length;
            long CurrentPosition = 0;
            decimal PercentCheck = 0;
            if (stream.Position != 0 && stream.CanSeek) { stream.Seek(0, SeekOrigin.Begin); }
            hashAlgorithm.Initialize();
            for (int i = 0; ; i++)
            {
                offset = bufferLength * (i % 2);
                readAheadBytesRead = stream.Read(buffer, offset, bufferLength);
                if (readAheadBytesRead == 0)
                {
                    hashAlgorithm.TransformFinalBlock(buffer, offset, readAheadBytesRead);
                    Console.Write("\r100.00%\r");
                    return hashAlgorithm.Hash;
                }
                else
                {
                    hashAlgorithm.TransformBlock(buffer, offset, readAheadBytesRead, buffer, offset);
                    decimal Percent = (Convert.ToDecimal(CurrentPosition) / TotalLength * 100);
                    CurrentPosition += bufferLength;
                    if (PercentCheck != Math.Round(Percent, 2))
                    {
                        PercentCheck = Math.Round(Percent, 2);
                        Console.Write("\r" + Math.Round(Percent, 2).ToString().PadLeft(6));
                    }
                }
            }
        }
    }
}
