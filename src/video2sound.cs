using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

// ExtractAudio - drag video/audio files onto this exe to get WAV + MP3 next to them.
// Requires ffmpeg on PATH (or sitting beside this exe).
static class ExtractAudio
{
    const string Mp3Bitrate = "320k";

    static int Main(string[] args)
    {
        Console.Title = "Extract Audio - WAV + MP3";
        Console.WriteLine("Extract Audio  ->  WAV (16-bit PCM) + MP3 (" + Mp3Bitrate + ")");
        Console.WriteLine(new string('-', 58));

        string ffmpeg = FindFfmpeg();
        if (ffmpeg == null)
        {
            Console.WriteLine();
            Console.WriteLine("ERROR: ffmpeg was not found.");
            Console.WriteLine("Install it, or drop ffmpeg.exe in the same folder as this program.");
            return Pause(1);
        }

        List<string> inputs = new List<string>();
        foreach (string a in args)
        {
            if (File.Exists(a)) inputs.Add(Path.GetFullPath(a));
            else Console.WriteLine("Skipping (not found): " + a);
        }

        if (inputs.Count == 0)
        {
            Console.WriteLine();
            Console.WriteLine("Tip: drag video files onto this .exe to convert them.");
            Console.Write("Or paste a file path here and press Enter: ");
            string typed = Console.ReadLine();
            if (typed != null)
            {
                typed = typed.Trim().Trim('"');
                if (typed.Length > 0 && File.Exists(typed)) inputs.Add(Path.GetFullPath(typed));
            }
            if (inputs.Count == 0)
            {
                Console.WriteLine("Nothing to do.");
                return Pause(1);
            }
        }

        int ok = 0, failed = 0;
        foreach (string input in inputs)
        {
            Console.WriteLine();
            Console.WriteLine("== " + Path.GetFileName(input));

            string outDir = WritableOutputDir(Path.GetDirectoryName(input));
            if (outDir == null)
            {
                Console.WriteLine("   ERROR: no writable place to save the output.");
                failed++;
                continue;
            }
            if (!PathsEqual(outDir, Path.GetDirectoryName(input)))
                Console.WriteLine("   (source folder is read-only - saving to " + outDir + ")");

            string stem = Path.GetFileNameWithoutExtension(input);
            string wav = Path.Combine(outDir, stem + ".wav");
            string mp3 = Path.Combine(outDir, stem + ".mp3");

            Console.WriteLine("   WAV ...");
            bool wavOk = Run(ffmpeg, "-y -hide_banner -v error -stats -i " + Q(input)
                                     + " -vn -acodec pcm_s16le " + Q(wav));

            Console.WriteLine("   MP3 ...");
            bool mp3Ok = Run(ffmpeg, "-y -hide_banner -v error -stats -i " + Q(input)
                                     + " -vn -acodec libmp3lame -b:a " + Mp3Bitrate + " " + Q(mp3));

            if (wavOk && mp3Ok)
            {
                Console.WriteLine("   done -> " + Size(wav) + " wav, " + Size(mp3) + " mp3");
                ok++;
            }
            else
            {
                Console.WriteLine("   FAILED (does this file have an audio track?)");
                failed++;
            }
        }

        Console.WriteLine();
        Console.WriteLine(new string('-', 58));
        Console.WriteLine("Converted " + ok + " file(s)" + (failed > 0 ? ", " + failed + " failed" : "") + ".");
        return Pause(failed > 0 ? 1 : 0);
    }

    static string Q(string s) { return "\"" + s + "\""; }

    static bool PathsEqual(string a, string b)
    {
        return string.Equals(Path.GetFullPath(a).TrimEnd('\\'),
                             Path.GetFullPath(b).TrimEnd('\\'),
                             StringComparison.OrdinalIgnoreCase);
    }

    static string FindFfmpeg()
    {
        string beside = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
        if (File.Exists(beside)) return beside;

        string path = Environment.GetEnvironmentVariable("PATH");
        if (path != null)
        {
            foreach (string dir in path.Split(';'))
            {
                if (dir.Trim().Length == 0) continue;
                try
                {
                    string c = Path.Combine(dir.Trim(), "ffmpeg.exe");
                    if (File.Exists(c)) return c;
                }
                catch { }
            }
        }
        return null;
    }

    // Prefer saving beside the source; fall back to Downloads, then Desktop.
    static string WritableOutputDir(string preferred)
    {
        if (IsWritable(preferred)) return preferred;

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string downloads = Path.Combine(home, "Downloads");
        if (IsWritable(downloads)) return downloads;

        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (IsWritable(desktop)) return desktop;

        return null;
    }

    static bool IsWritable(string dir)
    {
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return false;
        string probe = Path.Combine(dir, ".writetest_" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (FileStream fs = File.Create(probe)) { }
            File.Delete(probe);
            return true;
        }
        catch { return false; }
    }

    static bool Run(string exe, string arguments)
    {
        ProcessStartInfo psi = new ProcessStartInfo(exe, arguments);
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        try
        {
            using (Process p = Process.Start(psi))
            {
                p.WaitForExit();
                return p.ExitCode == 0;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("   " + ex.Message);
            return false;
        }
    }

    static string Size(string file)
    {
        try
        {
            double mb = new FileInfo(file).Length / 1024.0 / 1024.0;
            return mb.ToString("0.0") + " MB";
        }
        catch { return "?"; }
    }

    static int Pause(int code)
    {
        Console.WriteLine();
        Console.Write("Press Enter to close...");
        try { Console.ReadLine(); } catch { }
        return code;
    }
}
