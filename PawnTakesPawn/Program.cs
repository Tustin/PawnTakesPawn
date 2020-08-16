using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Xabe.FFmpeg;

namespace PawnTakesPawn
{
    class Program
    {
        static void Main(string[] args)
        {
            using var client = new WebClient
            {
                Encoding = Encoding.UTF8
            };

            var tapesDirPath = Path.Combine(Environment.CurrentDirectory, "tapes");

            var contents = client.DownloadString("https://pawntakespawn.com/tv");
            
            var existingTapes = new List<string>();

            var tapesFile = new FileStream("tapes.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            using (var reader = new StreamReader(tapesFile))
            {
                while (!reader.EndOfStream)
                {
                    existingTapes.Add(reader.ReadLine());
                }
            }

            if (!Directory.Exists(tapesDirPath))
            {
                Directory.CreateDirectory(tapesDirPath);
            }


            var doc = new HtmlDocument();
            doc.LoadHtml(contents);

            foreach (var tape in doc.DocumentNode.SelectNodes("//a[contains(@class, 'tape-fade-in')]"))
            {
                var tapeName = tape.GetAttributeValue("phx-click", string.Empty);

                if (tapeName == default)
                {
                    // Not a tape.
                    continue;
                }

                if (existingTapes.Contains(tapeName))
                {
                    // Already saved this tape.
                    continue;
                }

                var streamUrlHref = tape.GetAttributeValue("xlink:href", string.Empty);

                if (streamUrlHref == default)
                {
                    continue;
                }
                
                using (var writer = File.AppendText("tapes.txt"))
                {
                    writer.WriteLine(tapeName);
                }

                var match = Regex.Match(streamUrlHref, @"\(&#39;(.*)&#39;\)");

                if (match.Groups.Count != 2)
                {
                    continue;
                }

                var cleanedName = tapeName;

                if (cleanedName.Contains(":"))
                {
                    cleanedName = cleanedName.Split(":")[1];
                }


                var streamUrl = WebUtility.HtmlDecode(match.Groups[1].ToString());

                var aafdf = Environment.GetEnvironmentVariable("ffmpeg");

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    CreateNoWindow = false,
                    UseShellExecute = false,
                    WorkingDirectory = tapesDirPath,
                    FileName = "ffmpeg.exe",
                    Arguments = $@"-i ""{streamUrl}"" -c copy -t 1230 -bsf:a aac_adtstoasc {cleanedName}.mp4",
                    RedirectStandardOutput = true
                };

                try
                {
                    using (Process process = Process.Start(startInfo))
                    {
                        while (!process.StandardOutput.EndOfStream)
                        {
                            string line = process.StandardOutput.ReadLine();
                            Console.WriteLine(line);
                        }

                        process.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }
        }
    }
}
