using System;
using System.Diagnostics;
using System.IO;

namespace VideoToSound
{
    public class ConversionResult
    {
        public bool Success;
        public string OutputPath;
        public string Error;
    }

    /// <summary>
    /// Runs ffmpeg once per requested output. Cancellation kills the running
    /// process so it takes effect immediately rather than after the current file.
    /// </summary>
    public class Converter
    {
        private volatile bool cancelled;
        private Process current;
        private readonly object gate = new object();

        public bool Cancelled { get { return cancelled; } }

        public void Cancel()
        {
            cancelled = true;
            lock (gate)
            {
                try
                {
                    if (current != null && !current.HasExited) current.Kill();
                }
                catch { }
            }
        }

        public void Reset() { cancelled = false; }

        /// <summary>
        /// Decide where a given output should land, avoiding the trap of
        /// reading and writing the same file when the input is already audio.
        /// </summary>
        public static string BuildOutputPath(string input, string outDir, string extension)
        {
            string stem = Path.GetFileNameWithoutExtension(input);
            string candidate = Path.Combine(outDir, stem + extension);

            if (string.Equals(Path.GetFullPath(candidate), Path.GetFullPath(input),
                              StringComparison.OrdinalIgnoreCase))
            {
                candidate = Path.Combine(outDir, stem + " (converted)" + extension);
            }
            return candidate;
        }

        public ConversionResult Run(string ffmpegPath, string input, string outputPath,
                                    QualityOption quality)
        {
            ConversionResult result = new ConversionResult();
            result.OutputPath = outputPath;

            if (cancelled)
            {
                result.Success = false;
                result.Error = "Cancelled";
                return result;
            }

            string args = "-y -hide_banner -v error"
                        + " -i \"" + input + "\""
                        + " -vn " + quality.FfmpegArgs
                        + " \"" + outputPath + "\"";

            ProcessStartInfo psi = new ProcessStartInfo(ffmpegPath, args);
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardError = true;

            try
            {
                using (Process p = new Process())
                {
                    p.StartInfo = psi;
                    p.Start();

                    lock (gate) { current = p; }

                    // Only stderr is redirected, so a straight read cannot deadlock.
                    string stderr = p.StandardError.ReadToEnd();
                    p.WaitForExit();

                    lock (gate) { current = null; }

                    if (cancelled)
                    {
                        result.Success = false;
                        result.Error = "Cancelled";
                        TryDelete(outputPath);
                        return result;
                    }

                    if (p.ExitCode == 0)
                    {
                        result.Success = true;
                    }
                    else
                    {
                        result.Success = false;
                        result.Error = FirstLine(stderr);
                        TryDelete(outputPath);
                    }
                }
            }
            catch (Exception ex)
            {
                lock (gate) { current = null; }
                result.Success = false;
                result.Error = ex.Message;
            }

            return result;
        }

        /// <summary>True when the file has at least one audio stream.</summary>
        public static bool HasAudio(string ffmpegPath, string input)
        {
            // ffmpeg prints stream info to stderr; -i with no output exits non-zero
            // by design, so inspect the text rather than the exit code.
            ProcessStartInfo psi = new ProcessStartInfo(ffmpegPath,
                "-hide_banner -i \"" + input + "\"");
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardError = true;

            try
            {
                using (Process p = Process.Start(psi))
                {
                    string info = p.StandardError.ReadToEnd();
                    p.WaitForExit();
                    return info.IndexOf(": Audio:", StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }
            catch { return true; }   // if the probe fails, let the real run decide
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }

        private static string FirstLine(string text)
        {
            if (string.IsNullOrEmpty(text)) return "ffmpeg failed";
            string[] lines = text.Replace("\r", "").Split('\n');
            foreach (string line in lines)
            {
                if (line.Trim().Length > 0) return line.Trim();
            }
            return "ffmpeg failed";
        }
    }
}
