using System;
using System.Net;
using System.IO;
using System.Threading;
using System.Diagnostics;
using Newtonsoft.Json;
using NeoSmart.PrettySize;

namespace VideoCompressor
{
    internal class Program
    {
        static void Main()
        {
            InitTemp();
            InitDist();

            if (!FFmpegIsDownloaded())
            {
                Console.WriteLine("[LOG/CRITICAL] FFmpeg couldn't be found! Downloading...");
                DownloadFFmpeg();
            }
            else
            {
                Console.WriteLine("[LOG/INFO] FFmpeg was found! Ready to compress.");

                Console.Write("\nPlease enter the name of the file to be compressed: ");
                string filename = Console.ReadLine();

                if (!File.Exists(filename))
                {
                    Console.Clear();
                    Console.WriteLine("[LOG/ERROR] This file is not exist.");
                    InitClose();
                }

                Console.Write("\nPlease enter the compression preset (only veryLow/low/medium/high/veryHigh/ultra): ");
                string preset = Console.ReadLine();

                if (!IsPreset(preset))
                {
                    Console.Clear();
                    Console.WriteLine("[LOG/ERROR] Invalid preset was entered.");
                    InitClose();
                }

                Console.Write("\nPlease enter the compression preset (only x264/x265): ");
                string codec = Console.ReadLine();

                if (!IsCodec(codec))
                {
                    Console.Clear();
                    Console.WriteLine("[LOG/ERROR] Invalid codec was entered.");
                    InitClose();
                }

                Console.Clear();
                Console.WriteLine("[LOG/SUCCESS] Compressing...");
                Console.WriteLine("[LOG/INFO] At the end of the process, you can find the compressed file in the dist folder.");

                StartCompress(filename, preset, codec);
            }

        }

        private static bool IsPreset(string preset)
        {
            string[] IPresets = {
                "veryLow",
                "low",
                "medium",
                "high",
                "veryHigh",
                "ultra",
            };

            foreach (string IPreset in IPresets)
            {
                if (preset.Equals(IPreset))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsCodec(string codec)
        {
            string[] ICodecs = {
                "x264",
                "x265"
            };

            foreach (string ICodec in ICodecs)
            {
                if (codec.Equals(ICodec))
                {
                    return true;
                }
            }

            return false;
        }

        private static long GetNewBitrate(string bitrate, string preset)
        {
            switch (preset)
            {
                case "veryLow":
                    return (long)(Convert.ToInt64(bitrate) / 1.3);
                case "low":
                    return (long)(Convert.ToInt64(bitrate) / 1.6);
                case "medium":
                    return (long)(Convert.ToInt64(bitrate) / 2.4);
                case "high":
                    return (long)(Convert.ToInt64(bitrate) / 3.2);
                case "veryHigh":
                    return (long)(Convert.ToInt64(bitrate) / 4.4);
                case "ultra":
                    return (long)(Convert.ToInt64(bitrate) / 5.6);
                default:
                    return Convert.ToInt64(bitrate);
            }
        }

        private static void GenerateMetadata(string videoPath, string metadataFilename) {
            Process MetadataProcess = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    WorkingDirectory = $"{FileUtils.GetCurrentDirectory()}\\bin",
                    Arguments = $"/C ffprobe -v quiet -print_format json -show_format -hide_banner \"{FileUtils.GetCurrentDirectory()}\\{videoPath}\" > \"{FileUtils.GetCurrentDirectory()}\\temp\\{metadataFilename}.json\"",
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            MetadataProcess.Start();
            MetadataProcess.WaitForExit();
        }

        private static IFormat ParseMetadata(string filename) {
            return JsonConvert.DeserializeObject<IMetadata>(File.ReadAllText($"{FileUtils.GetCurrentDirectory()}\\temp\\{filename}.json")).format;
        }

        private static void StartCompress(string filename, string preset, string codec)
        {
            Console.WriteLine("[LOG/INFO] Generating metadata...");
            GenerateMetadata(filename, $"old-metadata-{filename}");
            Console.WriteLine("[LOG/SUCCESS] Metadata was generated. Contiuning the compress...");
            
            IFormat OldMetadata = ParseMetadata($"old-metadata-{filename}");

            if (File.Exists($"{FileUtils.GetCurrentDirectory()}\\dist\\{filename}")) {
                File.Delete($"{FileUtils.GetCurrentDirectory()}\\dist\\{filename}");
            }

            Process CompressProcess = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    WorkingDirectory = $"{FileUtils.GetCurrentDirectory()}\\bin",
                    Arguments = $"/C ffmpeg -i ../{filename} -preset slow -hide_banner -c:v lib{codec} -b:v {GetNewBitrate(OldMetadata.bit_rate, preset)} ../dist/{filename}",
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            CompressProcess.Start();
            CompressProcess.WaitForExit();

            Console.WriteLine("[LOG/SUCCESS] Video was compressed. Generating new metadata to finish...");
            GenerateMetadata($"dist\\{filename}", $"new-metadata-{filename}");
            Console.WriteLine("[LOG/SUCCESS] New metadata was generated. Creating the finish info...");
            
            IFormat NewMetadata = ParseMetadata($"new-metadata-{filename}");

            string CompressRatio = ((float)((float.Parse(OldMetadata.size) - float.Parse(NewMetadata.size)) / (float.Parse(NewMetadata.size)) * 100)).ToString("0.00");

            Console.Clear();
            Console.WriteLine("[LOG/SUCCESS] Finish!");
            Console.WriteLine($"[LOG/INFO] Old size: {PrettySize.Format(Convert.ToInt64(OldMetadata.size))}");
            Console.WriteLine($"[LOG/INFO] New size: {PrettySize.Format(Convert.ToInt64(NewMetadata.size))}");
            Console.WriteLine($"[LOG/INFO] Compression ratio: %{CompressRatio}");

            InitClose();
        }

        private static void InitClose()
        {
            Console.WriteLine("[LOG/INFO] The program will be automatically closed in 5 seconds.");
            Thread.Sleep(5000);
            Environment.Exit(1);
        }

        private static void InitTemp()
        {
            if (!Directory.Exists($"{FileUtils.GetCurrentDirectory()}\\temp"))
            {
                Console.WriteLine("[LOG/CRITICAL] Temp folder couldn't be found! Creating...");
                FileUtils.CreateFolder($"{FileUtils.GetCurrentDirectory()}\\temp");
                Console.WriteLine("[LOG/SUCCESS] Temp folder created!");
            }
        }
        private static void InitDist()
        {
            if (!Directory.Exists($"{FileUtils.GetCurrentDirectory()}\\dist"))
            {
                Console.WriteLine("[LOG/CRITICAL] Dist folder couldn't be found! Creating...");
                FileUtils.CreateFolder($"{FileUtils.GetCurrentDirectory()}\\dist");
                Console.WriteLine("[LOG/SUCCESS] Dist folder created!");
            }
        }


        private static void DownloadFFmpeg()
        {


            using (WebClient client = new WebClient())
            {
                client.DownloadFile(
                   new Uri("https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip"),
                   $"{FileUtils.GetCurrentDirectory()}\\temp\\ffmpeg.zip"
               );
            }

            Console.WriteLine("[LOG/SUCCESS] FFmpeg downloaded! Extracting...");
            FileUtils.ExtractZipFile(
                $"{FileUtils.GetCurrentDirectory()}\\temp\\ffmpeg.zip",
                $"{FileUtils.GetCurrentDirectory()}\\temp\\"
            );

            if (Directory.Exists($"{FileUtils.GetCurrentDirectory()}\\bin"))
            {
                FileUtils.Delete($"{FileUtils.GetCurrentDirectory()}\\bin");
            }

            Console.WriteLine("[LOG/SUCCESS] FFmpeg extracted! Resolving FFmpeg...");
            FileUtils.CreateFolder($"{FileUtils.GetCurrentDirectory()}\\bin");
            Directory.Move(
                $"{FileUtils.GetCurrentDirectory()}\\temp\\ffmpeg-master-latest-win64-gpl\\bin\\ffmpeg.exe",
                $"{FileUtils.GetCurrentDirectory()}\\bin\\ffmpeg.exe"
            );
            Directory.Move(
                $"{FileUtils.GetCurrentDirectory()}\\temp\\ffmpeg-master-latest-win64-gpl\\bin\\ffprobe.exe",
                $"{FileUtils.GetCurrentDirectory()}\\bin\\ffprobe.exe"
            );
            File.Delete($"{FileUtils.GetCurrentDirectory()}\\temp\\ffmpeg.zip");
            FileUtils.Delete($"{FileUtils.GetCurrentDirectory()}\\temp\\ffmpeg-master-latest-win64-gpl");

            Console.WriteLine("[LOG/SUCCESS] FFmpeg resolved! Restarting...");

            Thread.Sleep(3000);
            Console.Clear();
            Main();
        }

        private static bool FFmpegIsDownloaded()
        {
            if (!File.Exists($"{FileUtils.GetCurrentDirectory()}\\bin\\ffmpeg.exe"))
            {
                return false;
            }
            else if (!File.Exists($"{FileUtils.GetCurrentDirectory()}\\bin\\ffprobe.exe"))
            {
                return false;
            }

            return true;
        }
    }
}
