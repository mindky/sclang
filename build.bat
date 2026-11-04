@echo off
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo csc.exe not found. Install .NET Framework 4.x.
  pause
  exit /b 1
)
"%CSC%" /nologo /codepage:65001 /target:winexe /win32icon:sclang.ico /out:sclang.exe /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll sclang.cs AppSettings.cs Theme.cs CustomPresets.cs SettingsForm.cs LangLocator.cs WelcomeForm.cs
if errorlevel 1 (
  echo Build failed.
  pause
  exit /b 1
)
echo Built: %CD%\sclang.exe
