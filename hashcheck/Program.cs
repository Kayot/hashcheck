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
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Hash Check v0.1");
                Console.WriteLine("Usage: hashcheck [action] [target]");
                Console.WriteLine("");
                Console.WriteLine("-cf [filename]\t\tCreates a check file");
                Console.WriteLine("-vf [filename]\t\tChecks a check file");
                Console.WriteLine("-uf [filename]\t\tUpdates a check file (Removes missing, adds new)");
            }
            if (args.Length == 3)
            {
                if (args[0] == "-cf")
                {
                    GetAllHashes(args[2], args[1]);
                }
                if (args[0] == "-vf")
                {
                    VerifyAllHashes(args[2], args[1], false);
                }

                if (args[0] == "-uf")
                {
                    VerifyAllHashes(args[2], args[1], true);
                }
            }
        }

        static void GetAllHashes(string Folder, string OutputFile)
        {
            Console.WriteLine("Getting File List...");
            List<string> FileList = GetAllFiles(Folder);
            Console.WriteLine("Creating SHA1 Hashes...");
            if (!OutputFile.Contains(Path.DirectorySeparatorChar))
            {
                OutputFile = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + OutputFile;
            }
            using (FileStream OutFile = new FileStream(OutputFile, mode: FileMode.Create))
            {
                using (StreamWriter OutPut = new StreamWriter(OutFile))
                {
                    foreach (string i in FileList)
                    {
                        FileInfo CurrentFile = new FileInfo(i);
                        if (CurrentFile.FullName != OutputFile)
                        {
                            try
                            {
                                string x = CurrentFile.CreationTime.ToString("yyyyMMddHHmmss");
                                string z = CurrentFile.LastWriteTime.ToString("yyyyMMddHHmmss");
                                long a = CurrentFile.Length;
                                string s = GetHash(i);
                                OutPut.WriteLine(x + z + s + a + "\t" + i.Replace(Folder + Path.DirectorySeparatorChar, ""));
                                OutPut.Flush();
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("Unable to open file: " + i);
                            }
                        }
                    }
                }
            }
        }

        static void VerifyAllHashes(string Folder, string InputFile, bool UpdateMode)
        {
            List<Data> FromCheckFile = new List<Data>();
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
                            x.FileName = Folder + Path.DirectorySeparatorChar + Sections[1];
                            x.Index = LineIndex;
                            FromCheckFile.Add(x);
                            LineIndex++;
                        }
                    }
                }
            }
            Console.WriteLine("Getting File List...");
            List<string> FileList = GetAllFiles(Folder);
            for (int i = 0; i < FromCheckFile.Count; i++)
            {
                bool FileExistsInFileSystem = FileList.Contains(FromCheckFile[i].FileName);
                if (!FileExistsInFileSystem)
                {
                    if (UpdateMode)
                    {
                        Console.WriteLine(FromCheckFile[i].FileName + " -- Does not exist in file system, removing entry!");
                        Data x = FromCheckFile[i];
                        x.FileName = "--DELETED--";
                        FromCheckFile[i] = x;
                    }
                    else
                    {
                        Console.WriteLine(FromCheckFile[i].FileName + " -- Does not exist in file system");
                    }
                }
            }
            foreach (string i in FileList)
            {
                FileInfo CurrentFile = new FileInfo(i);
                Data CheckFile = FromCheckFile.Where(t => t.FileName == i).FirstOrDefault();
                string CreationTime = CurrentFile.CreationTime.ToString("yyyyMMddHHmmss");
                string LastWriteTime = CurrentFile.LastWriteTime.ToString("yyyyMMddHHmmss");
                long FileSize = CurrentFile.Length;
                if (CheckFile.FileName != null)
                {
                    if (UpdateMode)
                    {
                        //Data CurrentEntry = FromCheckFile[Convert.ToInt32(CheckFile.Index)];
                        //bool FileChanged = false;
                        //if (CheckFile.CreateTime != CreationTime)
                        //{
                        //    Console.WriteLine(i + " -- Creation Time Changed. Updating Entry!");
                        //    CurrentEntry.CreateTime = CreationTime;
                        //    FileChanged = true;
                        //}
                        //if (CheckFile.LastWrite != LastWriteTime)
                        //{
                        //    Console.WriteLine(i + " -- Last Write Time Changed. Updating Entry!");
                        //    CurrentEntry.LastWrite = LastWriteTime;
                        //    FileChanged = true;
                        //}
                        //if (CheckFile.FileSize != FileSize)
                        //{
                        //    Console.WriteLine(i + " -- Wrong File Size. Updating Entry!");
                        //    CurrentEntry.FileSize = FileSize;
                        //    FileChanged = true;
                        //}

                        //string s = GetHash(i);
                        //if (CheckFile.SHA1Hash != s)
                        //{
                        //    Console.WriteLine(i + " -- Hashes do not match. Updating Entry!");
                        //    CurrentEntry.SHA1Hash = s;
                        //    FileChanged = true;
                        //}

                        //if (FileChanged == true)
                        //{
                        //    FromCheckFile[Convert.ToInt32(CheckFile.Index)] = CurrentEntry;
                        //}
                    }
                    else
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
                            Console.WriteLine(i + " -- Wrong File Size, skipping SHA1");
                        }
                        else
                        {
                            string s = GetHash(i);
                            if (CheckFile.SHA1Hash != s)
                            {
                                Console.WriteLine(i + " -- Hashes do not match");
                            }
                        }
                    }
                }
                else
                {
                    if (UpdateMode)
                    {
                        Console.WriteLine(i + " -- Does not exist in Check file. Adding!");
                        Data PushtoArray = new Data();
                        PushtoArray.CreateTime = CreationTime;
                        PushtoArray.FileName = i;
                        PushtoArray.FileSize = FileSize;
                        PushtoArray.LastWrite = LastWriteTime;
                        PushtoArray.SHA1Hash = GetHash(i);
                        FromCheckFile.Add(PushtoArray);
                    }
                    else
                    {
                        Console.WriteLine(i + " -- Does not exist in Check file");
                    }
                }
            }

            if (UpdateMode)
            {
                Console.WriteLine("Rebuilding Check File");
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
                File.Move(InputFile, InputFile + ".bak");
                File.Move(InputFile + ".temp", InputFile);
            }
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
            }
            return Files;
        }


        static string GetHash(string Filename)
        {
            StringBuilder s = new StringBuilder();
            byte[] ResultBytes;
            using (FileStream SourceStream = File.Open(Filename, FileMode.Open))
            {
                SHA1 x = new SHA1Managed();
                ResultBytes = ComputeHash(SourceStream, x);
            }
            foreach (byte b in ResultBytes)
            {
                s.Append(b.ToString("x2").ToUpper());
            }
            return s.ToString();
        }

        static byte[] ComputeHash(Stream stream, HashAlgorithm hashAlgorithm)
        {
            const int bufferLength = 0x1000;
            byte[] buffer = new byte[bufferLength * 2 + 1];
            int readAheadBytesRead, offset;
            if (stream.Position != 0 && stream.CanSeek) { stream.Seek(0, SeekOrigin.Begin); }
            hashAlgorithm.Initialize();
            for (int i = 0; ; i++)
            {
                offset = bufferLength * (i % 2);
                readAheadBytesRead = stream.Read(buffer, offset, bufferLength);
                if (readAheadBytesRead == 0)
                {
                    hashAlgorithm.TransformFinalBlock(buffer, offset, readAheadBytesRead);
                    return hashAlgorithm.Hash;
                }
                else
                {
                    hashAlgorithm.TransformBlock(buffer, offset, readAheadBytesRead, buffer, offset);
                }
            }
        }
    }
}
