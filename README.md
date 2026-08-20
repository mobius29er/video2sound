# video2sound

Drag a video onto it. Get a WAV and an MP3 back. That's the whole program.

A tiny (8 KB) Windows console app that pulls the audio track out of any video
file and writes it to two formats at once:

| Output | Format |
| ------ | ------ |
| `.wav` | 16-bit PCM, source sample rate and channels preserved |
| `.mp3` | LAME 320 kbps CBR |

## Install

**Easiest — download the bundle.** Grab `video2sound-1.0.0-win64.zip` from the
[latest release](https://github.com/mobius29er/video2sound/releases/latest),
unzip it anywhere, and run `video2sound.exe`. ffmpeg is included; nothing to
install and nothing to configure. Keep `ffmpeg.exe` next to `video2sound.exe`.

**Or bring your own ffmpeg.** Download just `video2sound.exe` from this repo and
make sure [ffmpeg](https://ffmpeg.org/download.html) is on your `PATH`. The
program checks for `ffmpeg.exe` beside itself first, then falls back to `PATH`.

Either way there is no runtime to install: it targets the .NET Framework that
ships with Windows.

## Use

**Drag and drop** — select one or more video files and drop them onto
`video2sound.exe`. Works with any format ffmpeg can read (`.mp4`, `.mkv`,
`.mov`, `.webm`, `.avi`, …).

**Command line:**

```
video2sound.exe "C:\path\to\video.mp4"
video2sound.exe *.mp4
```

**Double-click** — it prompts for a file path.

Outputs are written next to the source file. If that folder is read-only —
Windows' Controlled Folder Access protects `Videos`, `Pictures` and friends by
default — it falls back to `Downloads`, then `Desktop`, and tells you where the
files went.

## Build from source

Needs nothing but Windows:

```
build.bat
```

That calls the C# compiler bundled with the .NET Framework
(`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`), so there is no SDK
or toolchain to install.

## Notes

- Files with no audio track fail loudly rather than writing an empty file.
- Existing outputs are overwritten without asking.
- Multiple files are processed in sequence, with a summary at the end.

## License

video2sound is MIT licensed - see [LICENSE](LICENSE).

The release bundle also ships `ffmpeg.exe`, which is separate software under the
**GNU GPL v3** (this is a `--enable-gpl --enable-version3` build). Its full
license text is included in the zip as `LICENSE-ffmpeg.txt`. video2sound runs
ffmpeg as a separate process and is neither derived from it nor linked against
it, so the two are merely aggregated and this project stays MIT.

Bundled build: ffmpeg 7.1.1-essentials_build from <https://www.gyan.dev/ffmpeg/builds/>.
FFmpeg source is available from <https://ffmpeg.org/download.html> and
<https://ffmpeg.org/releases/>.
