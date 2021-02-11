using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace hashcheck
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Hash Check v0.3.3");
                Console.WriteLine("Usage: hashcheck [action] [target]");
                Console.WriteLine("");
                Console.WriteLine("-cf\t--create-file\t\t[filename]\tCreates a check file");
                Console.WriteLine("-vf\t--verify-file\t\t[filename]\tChecks a check file");
                Console.WriteLine("-uf\t--update-file\t\t[filename]\tUpdates a check file (Removes missing, adds new)");
                Console.WriteLine("-cdh\t--check-duplicates-hardlink\t[filename]\tChecks hash files for duplicates (Can add more than one, use ',' no spaces) and outputs to target");
                Console.WriteLine("-cdr\t--check-duplicates-remove\t[filename]\tChecks hash files for duplicates (Can add more than one, use ',' no spaces) and outputs to target");
                Console.WriteLine("-d\t--debug-mode\t\t\t\tMakes the program pause so the debugger can hook the process (Must be first)");
            }
            if (args.Length >= 3)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "-cf" || args[i] == "--create-file")
                    {
                        if (!Directory.Exists(args[i + 2])) { Console.WriteLine("Directory Not Found."); return 2; }
                        DoWork(args[i + 1], args[i + 2], Mode.Create);
                    }
                    if (args[i] == "-vf" || args[i] == "--verify-file")
                    {
                        if (!File.Exists(args[i + 1])) { Console.WriteLine("Check File Not Found."); return 1; }
                        if (!Directory.Exists(args[i + 2])) { Console.WriteLine("Directory Not Found."); return 2; }
                        DoWork(args[i + 1], args[i + 2], Mode.Verify);
                    }
                    if (args[i] == "-uf" || args[i] == "--update-file")
                    {
                        if (!File.Exists(args[i + 1])) { Console.WriteLine("Check File Not Found."); return 1; }
                        if (!Directory.Exists(args[i + 2])) { Console.WriteLine("Directory Not Found."); return 2; }
                        DoWork(args[i + 1], args[i + 2], Mode.Update);
                    }
                    if (args[i] == "-cdh" || args[i] == "--check-duplicates-hardlink")
                    {
                        CheckForDuplicates(args[i + 1].Split(','), args[i + 2], CheckForDuplicatesMode.HardLink);
                        //CheckForDuplicates(args[1..^1], args[^1]);
                    }
                    if (args[i] == "-cdr" || args[i] == "--check-duplicates-remove")
                    {
                        CheckForDuplicates(args[i + 1].Split(','), args[i + 2], CheckForDuplicatesMode.Remove);
                        //CheckForDuplicates(args[1..^1], args[^1]);
                    }
                    if (args[i] == "-d" || args[i] == "--debug-mode")
                    {
                        Console.WriteLine("Press any key to continue.");
                        _ = Console.ReadKey();
                    }
                }
            }
            return 0;
        }

        enum CheckForDuplicatesMode
        {
            HardLink, Remove
        }

        static void CheckForDuplicates(string[] FileList, string OutputFileName, CheckForDuplicatesMode Mode)
        {
            List<(long FileSize, string Hash, long INode, string FileName, string HashName)> FileData = new();
            foreach (string item in FileList)
            {
                string[] FileLines = File.ReadAllLines(item);
                bool IsHeader = true;
                foreach (string Line in FileLines)
                {
                    if (IsHeader)
                    {
                        IsHeader = false;
                    }
                    else
                    {
                        string[] Sections = Line.Split("\t");
                        long INode = Convert.ToInt64(Sections[4]);
                        string Hash = Sections[2];
                        long FileSize = Convert.ToInt64(Sections[3]);
                        string FileName = Sections[5];
                        if (FileSize > 0)
                        {
                            FileData.Add((FileSize, Hash, INode, FileName, item));
                        }
                    }
                }
            }
            FileData = FileData.OrderByDescending(t => t.FileSize).ThenBy(t => t.Hash).ThenBy(t => t.INode).ThenBy(t => t.FileName).ToList();
            List<(long FileSize, string Hash, long INode, string FileName, string HashName)> DupItems = new();
            StringBuilder Output = new();
            Output.AppendLine("#!/bin/bash");
            Output.AppendLine("check_dup() {");
            Output.AppendLine("    if cmp --silent \"$1\" \"$2\"; then");
            Output.AppendLine("        ln -f \"$1\" \"$2\"");
            Output.AppendLine("        #echo \"Link Created!\"");
            Output.AppendLine("    fi");
            Output.AppendLine("}");
            Output.AppendLine();


            void OutToOutput()
            {
                //Output.AppendLine($"#  -- Duplicate -- Size = '{DupItems[0].FileSize}' -- Hash = '{DupItems[0].Hash}'");
                string PrimaryFile = "";
                for (int i = 0; i < DupItems.Count; i++)
                {
                    if (Mode == CheckForDuplicatesMode.HardLink)
                    {
                        //Output.AppendLine("#  " + DupItems[i].HashName + ": " + DupItems[i].FileName);
                        if (i == 0)
                        {
                            PrimaryFile = DupItems[i].FileName.Replace(@"'", @"\'");
                            //Output.AppendLine($"# Primary File '{PrimaryFile}'");
                        }
                        if (i > 0)
                        {
                            //Output.AppendLine($"# File to Hardlink '{DupItems[i].FileName.Replace("'", "\'")}'");
                            //Output.AppendLine($"rm \"{DupItems[i].FileName.Replace("'", "\'")}\"");
                            Output.AppendLine($"check_dup $'{PrimaryFile}' $'{DupItems[i].FileName.Replace(@"'", @"\'")}'");
                        }
                    }
                    if (Mode == CheckForDuplicatesMode.Remove)
                    {
                        Output.AppendLine("#  " + DupItems[i].HashName + ": " + DupItems[i].FileName);
                        Output.AppendLine($"rm \"{DupItems[i].FileName.Replace("'", "\'")}\"");
                    }
                }
                //Output.AppendLine();
                DupItems.Clear();
            }
            for (int i = 0; i < FileData.Count; i++)
            {
                if (i < FileData.Count - 1)
                {
                    if (FileData[i].FileSize == FileData[i + 1].FileSize && FileData[i].Hash == FileData[i + 1].Hash && FileData[i].INode != FileData[i + 1].INode)
                    {
                        DupItems.Add(FileData[i]);
                    }
                    else
                    {
                        if (DupItems.Count > 0)
                        {
                            DupItems.Add(FileData[i]);
                            OutToOutput();
                        }
                    }
                }
                else
                {
                    if (DupItems.Count > 0)
                    {
                        DupItems.Add(FileData[i]);
                        OutToOutput();
                    }
                }
            }
            File.WriteAllText(OutputFileName, Output.ToString());
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
            if (EndWithNewLine) { Console.WriteLine(Output); } else { Console.Write(Output); }
        }

        static void DoWork(string InputFile, string Folder, Mode RunningMode)
        {
            List<(string CreationTime, string LastWriteTime, long INode, long FileSize, string FileName)> FileList = new();
            using (StreamWriter WriteLog = new("output.log", true))
            {
                Console.WriteLine("Getting File List...");
                FileList = GetDirList(Folder);
                Console.WriteLine();
                List<HashFileData> FromCheckFile = new();
                if (RunningMode == Mode.Update || RunningMode == Mode.Verify)
                {
                    Console.WriteLine("Loading Check File...");
                    using (FileStream InFile = new(InputFile, mode: FileMode.Open))
                    {
                        using (StreamReader InStream = new(InFile))
                        {
                            int LineIndex = 0;
                            bool IsHeader = true;
                            while (!InStream.EndOfStream)
                            {
                                if (IsHeader)
                                {
                                    string Line = InStream.ReadLine();
                                    IsHeader = false;
                                }
                                else
                                {
                                    string Line = InStream.ReadLine();
                                    string[] Sections = Line.Split("\t");
                                    if (Sections.Length == 6)
                                    {
                                        HashFileData x = new();
                                        x.CreateTime = Sections[0];
                                        x.LastWrite = Sections[1];
                                        x.SHA1Hash = Sections[2];
                                        x.FileSize = Convert.ToInt64(Sections[3]);
                                        x.INode = Convert.ToInt64(Sections[4]);
                                        //x.FileName = Sections[5];
                                        x.FileName = Folder + Path.DirectorySeparatorChar + Sections[5].Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
                                        x.Index = LineIndex;
                                        SLC(("Loaded: " + x.FileName).Truncate(Console.WindowWidth));
                                        FromCheckFile.Add(x);
                                        LineIndex++;
                                    }
                                }
                            }
                            SLC("Finished Loading Check.", true);
                        }
                    }
                    for (int i = 0; i < FromCheckFile.Count; i++)
                    {
                        //bool FileExistsInFileSystem = FileList.Contains(FromCheckFile[i].FileName);
                        bool FileExistsInFileSystem = FileList.Where(t => t.FileName == FromCheckFile[i].FileName).Any(); //This part takes forever, maybe theres a way to optimize it?
                        if (!FileExistsInFileSystem)
                        {
                            if (RunningMode == Mode.Update)
                            {
                                SLC(FromCheckFile[i].FileName + " -- Does not exist in file system, removing entry!", true);
                                WriteLog.WriteLine(FromCheckFile[i].FileName + " -- Does not exist in file system, removing entry!");
                                HashFileData x = FromCheckFile[i];
                                x.FileName = "--DELETED--";
                                FromCheckFile[i] = x;
                            }
                            else
                            {
                                SLC(FromCheckFile[i].FileName + " -- Does not exist in file system", true);
                                WriteLog.WriteLine(FromCheckFile[i].FileName + " -- Does not exist in file system");
                            }
                        }
                    }
                }
                int Padding = FileList.Count.ToString().Length;
                string RightSide = "/" + FileList.Count.ToString().PadLeft(Padding);
                bool Scanning = false;
                for (int i = 0; i < FileList.Count; i++)
                {

                    HashFileData CheckFile = new();
                    if (RunningMode == Mode.Update || RunningMode == Mode.Verify)
                    {
                        CheckFile = FromCheckFile.Where(t => t.FileName == FileList[i].FileName).FirstOrDefault();
                    }


                    string FileCount = i.ToString().PadLeft(Padding) + RightSide;
                    if (CheckFile.FileName != null)
                    {
                        if (RunningMode == Mode.Update)
                        {
                            HashFileData CurrentEntry = FromCheckFile[Convert.ToInt32(CheckFile.Index)];
                            bool FileChanged = false;
                            if (CheckFile.CreateTime != FileList[i].CreationTime)
                            {
                                Console.WriteLine(FileList[i] + " -- Creation Time Changed. Updating Entry!");
                                WriteLog.WriteLine(FileList[i] + " -- Creation Time Changed. Updating Entry!");
                                CurrentEntry.CreateTime = FileList[i].CreationTime;
                                FileChanged = true;
                            }
                            if (CheckFile.LastWrite != FileList[i].LastWriteTime)
                            {
                                Console.WriteLine(FileList[i] + " -- Last Write Time Changed. Updating Entry!");
                                WriteLog.WriteLine(FileList[i] + " -- Last Write Time Changed. Updating Entry!");
                                CurrentEntry.LastWrite = FileList[i].LastWriteTime;
                                FileChanged = true;
                            }
                            if (CheckFile.FileSize != FileList[i].FileSize)
                            {
                                SLC(FileList[i] + " -- Wrong File Size. Updating Entry!", true);
                                WriteLog.WriteLine(FileList[i] + " -- Wrong File Size. Updating Entry!");
                                CurrentEntry.FileSize = FileList[i].FileSize;
                                FileChanged = true;
                            }
                            if (CheckFile.INode != FileList[i].INode)
                            //if (CheckFile.INode <= 2)
                            {
                                //Fix Inode when -1, no reason to set it as a change(?)
                                //if (CheckFile.INode == -1) //Means it was never set, used to fix new column
                                //{
                                CurrentEntry.INode = FileList[i].INode;
                                FromCheckFile[Convert.ToInt32(CheckFile.Index)] = CurrentEntry;
                                //}
                                //else
                                //{
                                //    Console.WriteLine(FileList[i] + " -- INode Changed. Updating Entry!");
                                //    WriteLog.WriteLine(FileList[i] + " -- INode Changed. Updating Entry!");
                                //    CurrentEntry.INode = INode;
                                //    FileChanged = true;
                                //}
                            }
                            if (FileChanged == true)
                            {
                                Scanning = false;
                                string s = GetHash(FileList[i].FileName, FileCount);
                                CurrentEntry.SHA1Hash = s;
                                FromCheckFile[Convert.ToInt32(CheckFile.Index)] = CurrentEntry;
                            }
                            else
                            {
                                if (Scanning)
                                {
                                    Console.CursorLeft = 10;
                                    Console.Write(i.ToString().PadLeft(Padding));
                                }
                                else
                                {
                                    //Console.Write("Scanning: " + FileCount);
                                    SLC("Scanning: " + FileCount);
                                }
                                Scanning = true;
                            }
                        }
                        if (RunningMode == Mode.Verify)
                        {
                            if (CheckFile.CreateTime != FileList[i].CreationTime)
                            {
                                Console.WriteLine(i + " -- Creation Time Changed");
                                WriteLog.WriteLine(i + " -- Creation Time Changed");
                            }
                            if (CheckFile.LastWrite != FileList[i].LastWriteTime)
                            {
                                Console.WriteLine(i + " -- Last Write Time Changed");
                                WriteLog.WriteLine(i + " -- Last Write Time Changed");
                            }
                            if (CheckFile.FileSize != FileList[i].FileSize)
                            {
                                SLC(FileList[i].FileName + " -- Wrong File Size, skipping SHA1", true);
                                WriteLog.WriteLine(FileList[i].FileName + " -- Wrong File Size, skipping SHA1");
                            }
                            else
                            {
                                string s = GetHash(FileList[i].FileName, FileCount);
                                if (CheckFile.SHA1Hash != s)
                                {
                                    SLC(FileList[i].FileName + " -- Hashes do not match", true);
                                    WriteLog.WriteLine();
                                }
                            }
                        }
                    }
                    else
                    {
                        void CreateEntry()
                        {
                            HashFileData PushtoArray = new HashFileData();
                            PushtoArray.CreateTime = FileList[i].CreationTime;
                            PushtoArray.FileName = FileList[i].FileName;
                            PushtoArray.FileSize = FileList[i].FileSize;
                            PushtoArray.LastWrite = FileList[i].LastWriteTime;
                            PushtoArray.SHA1Hash = GetHash(FileList[i].FileName, FileCount);
                            FromCheckFile.Add(PushtoArray);
                        }
                        if (RunningMode == Mode.Create)
                        {
                            CreateEntry();
                        }
                        if (RunningMode == Mode.Update)
                        {
                            SLC(FileList[i].FileName + " -- Does not exist in Check file. Adding!", true);
                            WriteLog.WriteLine(FileList[i].FileName + " -- Does not exist in Check file. Adding!");
                            CreateEntry();
                        }
                        if (RunningMode == Mode.Verify)
                        {
                            SLC(FileList[i].FileName + " -- Does not exist in Check file", true);
                            WriteLog.WriteLine(FileList[i].FileName + " -- Does not exist in Check file");
                        }
                    }
                }
                void CreateCheckFile()
                {
                    using (FileStream OutFile = new FileStream(InputFile + ".temp", mode: FileMode.Create))
                    {
                        using (StreamWriter OutPut = new StreamWriter(OutFile))
                        {
                            OutPut.WriteLine("CreateTime\tLastWrite\tSHA1Hash\tFileSize\tINode\tFileNameAndPath");
                            foreach (HashFileData i in FromCheckFile)
                            {
                                if (i.FileName != "--DELETED--")
                                {
                                    OutPut.WriteLine($"{i.CreateTime}\t{i.LastWrite}\t{i.SHA1Hash}\t{i.FileSize}\t{i.INode}\t{i.FileName.Replace(Folder + Path.DirectorySeparatorChar, "")}");
                                    OutPut.Flush();
                                }
                            }
                        }
                    }
                    if (File.Exists(InputFile + ".bak")) { File.Delete(InputFile + ".bak"); }
                    if (File.Exists(InputFile)) { File.Move(InputFile, InputFile + ".bak"); }
                    File.Move(InputFile + ".temp", InputFile);
                }
                if (RunningMode == Mode.Update)
                {
                    Console.WriteLine();
                    Console.WriteLine("Rebuilding Check File");
                    CreateCheckFile();
                }
                if (RunningMode == Mode.Create)
                {
                    Console.WriteLine();
                    Console.WriteLine("Creating Check File");
                    CreateCheckFile();
                }
                Console.WriteLine();
                Console.WriteLine("Finished.");
                Console.WriteLine();
                Console.WriteLine();

            }

        }

        struct HashFileData
        {
            public int Index { get; set; }
            public long INode { get; set; }
            public string FileName { get; set; }
            public string LastWrite { get; set; }
            public string CreateTime { get; set; }
            public long FileSize { get; set; }
            public string SHA1Hash { get; set; }
        }

        struct FileSystemData
        {
            public int Index { get; set; }
            public long INode { get; set; }
            //public string FileName { get; set; }
            public string LastWrite { get; set; }
            public string CreateTime { get; set; }
            public long FileSize { get; set; }
            //public string SHA1Hash { get; set; }
        }

        static string GetHash(string Filename, string FileStatus)
        {
            string FileStatusBase = "  0.00%   " + FileStatus + " Hashing: " + Path.GetFileName(Filename);
            SLC(FileStatusBase.Truncate(Console.WindowWidth));
            StringBuilder s = new StringBuilder();
            byte[] ResultBytes;
            using (FileStream SourceStream = File.Open(Filename, FileMode.Open))
            {
                SHA1 x = new SHA1Managed();
                ResultBytes = ComputeHash(SourceStream, x, Filename, FileStatus);
            }
            foreach (byte b in ResultBytes)
            {
                s.Append(b.ToString("x2").ToUpper());
            }
            return s.ToString();
        }

        static byte[] ComputeHash(Stream stream, HashAlgorithm hashAlgorithm, string Filename, string FileStatus)
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
                    string FileStatusBase = "100.00%   " + FileStatus + " Hashing: " + Path.GetFileName(Filename);
                    SLC(FileStatusBase, true);
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
                        Console.Write("\r" + Math.Round(Percent, 2).ToString().PadLeft(6) + "\r");
                    }
                }
            }
        }

       private static readonly BlockingCollection<DirectoryInfo> collection = new BlockingCollection<DirectoryInfo>();

       static public List<(string CreationTime, string LastWriteTime, long INode, long FileSize, string FileName)> GetDirList(string path)
        {

            collection.Add(new DirectoryInfo(path));
            Task.Factory.StartNew(() => CollectFolders(path));

            List<(string CreationTime, string LastWriteTime, long INode, long FileSize, string FileName)> z = new List<(string CreationTime, string LastWriteTime, long INode, long FileSize, string FileName)>();

            foreach (DirectoryInfo dir in collection.GetConsumingEnumerable())
            {
                FileInfo[] x = dir.GetFiles();
                for (int i = 0; i < x.Length; i++)
                {
                    z.Add((
                        x[i].CreationTime.ToUniversalTime().ToString("yyyyMMddHHmmss"),
                        x[i].LastWriteTime.ToUniversalTime().ToString("yyyyMMddHHmmss"),
                        x[i].GetFileId(),
                        x[i].Length,
                        x[i].FullName
                    ));
                }

                SLC(z.Count + " : Files Found");
            }
            return z;
        }

       static public void CollectFolders(string path)
        {
            try
            {
                foreach (DirectoryInfo dir in new DirectoryInfo(path).EnumerateDirectories("*", SearchOption.AllDirectories))
                {
                    collection.Add(dir);
                }
            }
            finally
            {
                collection.CompleteAdding();
            }
        }




    }
}
