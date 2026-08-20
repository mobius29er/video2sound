# video2sound — GUI design

## What it is

A single-window Windows app for pulling audio out of video files. Drop files in,
tick the formats you want, hit Convert. One pass over each file can produce
several formats at once.

## Decisions

### Toolkit: WinForms on the .NET Framework

Chosen over the alternatives because it needs **no toolchain to build and no
runtime to install**. The C# compiler ships inside Windows
(`%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe`), and .NET Framework 4.x
is present on every supported Windows install. Native drag-and-drop, real
progress bars, and standard file dialogs come for free.

Rejected:

- **Python + tkinter + PyInstaller** — drag-and-drop needs a third-party package
  (`tkinterdnd2`), the bundle balloons past 10 MB, and startup is visibly slow.
- **WPF** — better looking, but compiling XAML without the full SDK is painful.
  Not worth the build fragility for a utility this size.
- **Electron / Tauri** — absurd overkill; the whole app is a form and a subprocess.

### Conversion: shell out to ffmpeg, one process per output

No audio library is linked in. Each requested format is a separate `ffmpeg`
invocation reading the source video and writing one file. That keeps the code
trivial, keeps ffmpeg at arm's length as a separate program (which is what keeps
this project MIT rather than GPL), and means a failure in one format doesn't
take down the others.

The cost is that a 3-format job reads the source 3 times. For audio extraction
this is I/O-cheap and not worth optimising away with a filter-graph.

### ffmpeg discovery

Beside the exe first, then `PATH`. The release bundle ships `ffmpeg.exe` next to
`video2sound.exe`, so the bundled copy always wins over whatever else is on the
machine — no version surprises.

### Output location

Defaults to the source file's own folder. If that folder is not writable, fall
back to `Downloads`, then `Desktop`, and say so in the UI rather than failing.

This is not a rare edge case: Windows Controlled Folder Access protects `Videos`,
`Pictures` and `Documents` by default, which is exactly where video files live.

### Formats offered

| Format | Codec | Quality choices |
| ------ | ----- | --------------- |
| WAV | `pcm_s16le` / `pcm_s24le` / `pcm_f32le` | 16-bit, 24-bit, 32-bit float |
| MP3 | `libmp3lame` | 320 / 256 / 192 / 128 kbps |
| FLAC | `flac` | Balanced, Smallest, Fastest |
| M4A | `aac` | 256 / 192 / 128 kbps |
| OGG | `libvorbis` | 320 / 192 / 128 kbps |

Every codec above is present in the bundled `essentials` build — verified
against its `--enable-` configuration.

Quality is a dropdown per format with a sensible default preselected, so it can
be ignored entirely but is there when it matters.

### Threading

Conversion runs on a background thread; the UI thread only ever receives
marshalled status updates. Cancel sets a flag and kills the running ffmpeg
process, so it takes effect immediately rather than after the current file.

## Layout

```
+-----------------------------------------------------------+
|  File list (drag & drop target)      |  Output formats     |
|  Name          Status                |  [x] WAV   [16-bit] |
|  clip1.mp4     Done                  |  [x] MP3   [320   ] |
|  clip2.mkv     Converting (MP3)      |  [ ] FLAC  [Balanc] |
|                                      |  [ ] M4A   [256   ] |
|  [Add Files] [Remove] [Clear]        |  [ ] OGG   [192   ] |
|                                      |                     |
|                                      |  Save to            |
|                                      |  (o) Same as source |
|                                      |  ( ) [path] [...]   |
+-----------------------------------------------------------+
|  [=========------------]  2 of 5     [Convert]  [Cancel]   |
+-----------------------------------------------------------+
```

## Command line

Files passed as arguments are pre-loaded into the list rather than converted
headlessly, so dragging videos onto the exe still works and lands you in the
window with the queue ready.

## Deliberately out of scope

Settings persistence between launches, per-file progress bars, and a
drag-to-reorder queue. They add state and complexity for little gain in a tool
whose whole job takes about fifteen seconds.
