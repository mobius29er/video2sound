@echo off
REM Build video2sound.exe using the C# compiler bundled with Windows.
REM No SDK, no toolchain, no downloads.
setlocal

set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
    echo ERROR: Could not find the .NET Framework C# compiler.
    exit /b 1
)

"%CSC%" -nologo -optimize+ -target:winexe ^
    -out:"%~dp0video2sound.exe" ^
    -win32icon:"%~dp0assets\video2sound.ico" ^
    -resource:"%~dp0assets\mark.png",mark.png ^
    -resource:"%~dp0assets\video2sound.ico",app.ico ^
    -r:System.dll ^
    -r:System.Core.dll ^
    -r:System.Drawing.dll ^
    -r:System.Windows.Forms.dll ^
    "%~dp0src\Program.cs" ^
    "%~dp0src\MainForm.cs" ^
    "%~dp0src\Converter.cs" ^
    "%~dp0src\Formats.cs" ^
    "%~dp0src\Skin.cs"

if errorlevel 1 (
    echo BUILD FAILED
    exit /b 1
)

echo Built %~dp0video2sound.exe
