# video2sound

Drag a video onto it. Get a WAV and an MP3 back. That's the whole program.

A tiny (8 KB) Windows console app that pulls the audio track out of any video
file and writes it to two formats at once:

| Output | Format |
| ------ | ------ |
| `.wav` | 16-bit PCM, source sample rate and channels preserved |
| `.mp3` | LAME 320 kbps CBR |

## Install

1. Grab `video2sound.exe` from this repo.
2. Make sure [ffmpeg](https://ffmpeg.org/download.html) is installed and on your
   `PATH`. (Alternatively, drop `ffmpeg.exe` in the same folder as
   `video2sound.exe` — it looks there first.)

That's it. No runtime to install: it targets the .NET Framework that ships with
Windows.

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

MIT — see [LICENSE](LICENSE).
