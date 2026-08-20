@echo off
REM Build video2sound.exe using the C# compiler bundled with Windows.
setlocal

set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
    echo ERROR: Could not find the .NET Framework C# compiler.
    exit /b 1
)

"%CSC%" -nologo -optimize+ -target:exe -out:"%~dp0video2sound.exe" "%~dp0src\video2sound.cs"
if errorlevel 1 (
    echo BUILD FAILED
    exit /b 1
)

echo Built %~dp0video2sound.exe
