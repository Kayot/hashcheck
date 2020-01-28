﻿using System;
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
                Console.WriteLine("-sf [filename]\t\tShrinks the check file (Reversable)");

            }

            if (args.Length == 2)
            {
                if (args[0] == "-sf")
                {
                    ShrinkFile(args[1]);
                }
                if (args[0] == "-xf")
                {

                }
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

        static void ShrinkFile(string Filename)
        {
            string[] Input = System.IO.File.ReadAllLines(Filename);
            List<ShrinkData> CheckData = new List<ShrinkData>();

            foreach (string i in Input)
            {
                ShrinkData ni = new ShrinkData();
                string[] SubData = i.Split("\t");
                ni.Name = SubData[1];
                ni.Hash = SubData[0].Substring(28, 40);
                ni.Size = SubData[0].Substring(68);
                CheckData.Add(ni);
            }

            CheckData = CheckData.OrderBy(t => t.Name).ToList();

            for (int i = 0; i < CheckData.Count(); i++)
            {



            }


        }

        struct ShrinkData
        {
            public string Hash { get; set; }
            public string Size { get; set; }
            public string Name { get; set; }
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
                                //string CreateTime = CurrentFile.CreationTime.ToString("yyyyMMddHHmmss");
                                //string LastWrite = CurrentFile.LastWriteTime.ToString("yyyyMMddHHmmss");

                                string CreateTime = CurrentFile.CreationTime.ToUniversalTime().ToString("yyyyMMddHHmmss");
                                string LastWrite = CurrentFile.LastWriteTime.ToUniversalTime().ToString("yyyyMMddHHmmss");

                                long FileSize = CurrentFile.Length;
                                string HashString = GetHash(i);
                                OutPut.WriteLine(CreateTime + LastWrite + HashString + FileSize + "\t" + i.Replace(Folder + Path.DirectorySeparatorChar, ""));
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
                            x.FileName = Folder + Path.DirectorySeparatorChar + Sections[1].Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
                            x.Index = LineIndex;
                            if (("Loaded: " + x.FileName).Length >= Console.WindowWidth - 1)
                            {
                                SLC(("Loaded: " + x.FileName).Substring(0, Console.WindowWidth - 1));
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
            Console.WriteLine("Getting File List...");
            List<string> FileList = GetAllFiles(Folder);
            for (int i = 0; i < FromCheckFile.Count; i++)
            {
                bool FileExistsInFileSystem = FileList.Contains(FromCheckFile[i].FileName);
                if (!FileExistsInFileSystem)
                {
                    if (UpdateMode)
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
            foreach (string i in FileList)
            {
                FileInfo CurrentFile = new FileInfo(i);
                Data CheckFile = FromCheckFile.Where(t => t.FileName == i).FirstOrDefault();
                string CreationTime = CurrentFile.CreationTime.ToUniversalTime().ToString("yyyyMMddHHmmss");
                string LastWriteTime = CurrentFile.LastWriteTime.ToUniversalTime().ToString("yyyyMMddHHmmss");
                long FileSize = CurrentFile.Length;
                if (CheckFile.FileName != null)
                {
                    if (UpdateMode)
                    {
                        Data CurrentEntry = FromCheckFile[Convert.ToInt32(CheckFile.Index)];
                        bool FileChanged = false;
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
                        if (CheckFile.FileSize != FileSize)
                        {
                            SLC(i + " -- Wrong File Size. Updating Entry!", true);
                            CurrentEntry.FileSize = FileSize;
                            FileChanged = true;
                        }

                        string s = GetHash(i);
                        if (CheckFile.SHA1Hash != s)
                        {
                            SLC(i + " -- Hashes do not match. Updating Entry!", true);
                            CurrentEntry.SHA1Hash = s;
                            FileChanged = true;
                        }

                        if (FileChanged == true)
                        {
                            FromCheckFile[Convert.ToInt32(CheckFile.Index)] = CurrentEntry;
                        }
                    }
                    else
                    {
                        //if (CheckFile.CreateTime != CreationTime)
                        //{
                        //    Console.WriteLine(i + " -- Creation Time Changed");
                        //}
                        //if (CheckFile.LastWrite != LastWriteTime)
                        //{
                        //    Console.WriteLine(i + " -- Last Write Time Changed");
                        //}
                        if (CheckFile.FileSize != FileSize)
                        {
                            SLC(i + " -- Wrong File Size, skipping SHA1", true);
                        }
                        else
                        {
                            string s = GetHash(i);
                            if (CheckFile.SHA1Hash != s)
                            {
                                SLC(i + " -- Hashes do not match", true);
                            }
                        }
                    }
                }
                else
                {
                    if (UpdateMode)
                    {
                        SLC(i + " -- Does not exist in Check file. Adding!", true);
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
                        SLC(i + " -- Does not exist in Check file", true);
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
            Console.WriteLine("Finished.");
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
            if (Path.GetFileName(Filename).Length > Console.WindowWidth - 18)
            {
                SLC("  0.00% Hashing: " + Path.GetFileName(Filename).Substring(0, Console.WindowWidth - 18));
            }
            else
            {
                SLC("  0.00% Hashing: " + Path.GetFileName(Filename));
            }
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
                    Console.Write("\r100.00%");
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
