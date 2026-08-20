using System;
using System.Collections.Generic;
using System.IO;

namespace VideoToSound
{
    /// <summary>One selectable quality level within a format.</summary>
    public class QualityOption
    {
        public string Label;
        public string FfmpegArgs;

        public QualityOption(string label, string ffmpegArgs)
        {
            Label = label;
            FfmpegArgs = ffmpegArgs;
        }

        public override string ToString() { return Label; }
    }

    /// <summary>An output format offered in the formats checklist.</summary>
    public class FormatSpec
    {
        public string Label;
        public string Extension;
        public QualityOption[] Qualities;
        public int DefaultQualityIndex;
        public bool CheckedByDefault;

        public FormatSpec(string label, string extension, int defaultQualityIndex,
                          bool checkedByDefault, QualityOption[] qualities)
        {
            Label = label;
            Extension = extension;
            DefaultQualityIndex = defaultQualityIndex;
            CheckedByDefault = checkedByDefault;
            Qualities = qualities;
        }

        /// <summary>
        /// Every codec below is present in the bundled ffmpeg "essentials" build.
        /// </summary>
        public static FormatSpec[] All()
        {
            return new FormatSpec[]
            {
                new FormatSpec("WAV", ".wav", 0, true, new QualityOption[]
                {
                    new QualityOption("16-bit PCM",    "-acodec pcm_s16le"),
                    new QualityOption("24-bit PCM",    "-acodec pcm_s24le"),
                    new QualityOption("32-bit float",  "-acodec pcm_f32le"),
                }),

                new FormatSpec("MP3", ".mp3", 0, true, new QualityOption[]
                {
                    new QualityOption("320 kbps", "-acodec libmp3lame -b:a 320k"),
                    new QualityOption("256 kbps", "-acodec libmp3lame -b:a 256k"),
                    new QualityOption("192 kbps", "-acodec libmp3lame -b:a 192k"),
                    new QualityOption("128 kbps", "-acodec libmp3lame -b:a 128k"),
                }),

                new FormatSpec("FLAC", ".flac", 0, false, new QualityOption[]
                {
                    new QualityOption("Balanced", "-acodec flac -compression_level 5"),
                    new QualityOption("Smallest", "-acodec flac -compression_level 12"),
                    new QualityOption("Fastest",  "-acodec flac -compression_level 0"),
                }),

                new FormatSpec("M4A", ".m4a", 0, false, new QualityOption[]
                {
                    new QualityOption("256 kbps", "-acodec aac -b:a 256k"),
                    new QualityOption("192 kbps", "-acodec aac -b:a 192k"),
                    new QualityOption("128 kbps", "-acodec aac -b:a 128k"),
                }),

                new FormatSpec("OGG", ".ogg", 1, false, new QualityOption[]
                {
                    new QualityOption("320 kbps", "-acodec libvorbis -b:a 320k"),
                    new QualityOption("192 kbps", "-acodec libvorbis -b:a 192k"),
                    new QualityOption("128 kbps", "-acodec libvorbis -b:a 128k"),
                }),
            };
        }
    }

    public static class Ffmpeg
    {
        /// <summary>
        /// Prefer an ffmpeg.exe sitting beside this program (the release bundle
        /// ships one) so the bundled version always wins over whatever else is
        /// installed. Fall back to PATH.
        /// </summary>
        public static string Locate()
        {
            try
            {
                string beside = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
                if (File.Exists(beside)) return beside;
            }
            catch { }

            string path = Environment.GetEnvironmentVariable("PATH");
            if (path != null)
            {
                foreach (string dir in path.Split(';'))
                {
                    string trimmed = dir.Trim();
                    if (trimmed.Length == 0) continue;
                    try
                    {
                        string candidate = Path.Combine(trimmed, "ffmpeg.exe");
                        if (File.Exists(candidate)) return candidate;
                    }
                    catch { }
                }
            }
            return null;
        }
    }

    public static class Paths
    {
        /// <summary>
        /// Windows Controlled Folder Access protects Videos, Pictures and
        /// Documents by default, which is exactly where source videos live.
        /// Degrade to somewhere writable instead of failing.
        /// </summary>
        public static string FirstWritable(string preferred)
        {
            if (IsWritable(preferred)) return preferred;

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string downloads = Path.Combine(home, "Downloads");
            if (IsWritable(downloads)) return downloads;

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (IsWritable(desktop)) return desktop;

            return null;
        }

        public static bool IsWritable(string dir)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return false;
            string probe = Path.Combine(dir, ".v2s_" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (FileStream fs = File.Create(probe)) { }
                File.Delete(probe);
                return true;
            }
            catch { return false; }
        }

        public static bool SameDirectory(string a, string b)
        {
            try
            {
                return string.Equals(Path.GetFullPath(a).TrimEnd('\\'),
                                     Path.GetFullPath(b).TrimEnd('\\'),
                                     StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        public static string HumanSize(string file)
        {
            try
            {
                long bytes = new FileInfo(file).Length;
                if (bytes < 1024L * 1024L)
                    return (bytes / 1024.0).ToString("0") + " KB";
                return (bytes / 1024.0 / 1024.0).ToString("0.0") + " MB";
            }
            catch { return "?"; }
        }

        /// <summary>Common containers, for the Add Files dialog filter.</summary>
        public static string OpenFilter()
        {
            List<string> exts = new List<string>(new string[]
            {
                "*.mp4", "*.mkv", "*.mov", "*.webm", "*.avi", "*.wmv", "*.flv",
                "*.m4v", "*.mpg", "*.mpeg", "*.ts", "*.m2ts", "*.3gp",
                "*.mp3", "*.wav", "*.m4a", "*.flac", "*.ogg", "*.aac", "*.wma"
            });
            string joined = string.Join(";", exts.ToArray());
            return "Media files|" + joined + "|All files|*.*";
        }
    }
}
