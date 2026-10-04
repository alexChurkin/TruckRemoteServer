# Starts the server on a clean Windows (CI), dismisses the first-start dialogs and takes screenshots
# of the main window: light theme, dark theme and Russian. Also a smoke test: the app must start.
param([string]$Exe, [string]$Out)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms, System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class Win {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out RECT rect, int size);
    public delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc proc, IntPtr lParam);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder name, int max);
}
"@
New-Item -ItemType Directory -Force -Path $Out | Out-Null

function Get-Windows([int]$ProcessId) {
    $list = New-Object System.Collections.Generic.List[object]
    [Win]::EnumWindows({ param($h, $l)
        $p = 0; [void][Win]::GetWindowThreadProcessId($h, [ref]$p)
        if ($p -eq $ProcessId -and [Win]::IsWindowVisible($h)) {
            $name = New-Object System.Text.StringBuilder 256; [void][Win]::GetClassName($h, $name, 256)
            $list.Add([pscustomobject]@{ Handle = $h; Class = $name.ToString() })
        }
        return $true }, [IntPtr]::Zero) | Out-Null
    return $list
}

# Message boxes: Esc closes OK/Cancel ones, N answers No to the firewall question
function Close-Dialogs($process) {
    for ($i = 0; $i -lt 40; $i++) {
        Start-Sleep -Milliseconds 500
        $dialogs = @(Get-Windows $process.Id | Where-Object { $_.Class -eq '#32770' })
        if ($dialogs.Count -eq 0 -and $i -gt 6) { return }
        foreach ($d in $dialogs) {
            [void][Win]::SetForegroundWindow($d.Handle)
            Start-Sleep -Milliseconds 200
            [System.Windows.Forms.SendKeys]::SendWait('{ESC}')
            Start-Sleep -Milliseconds 200
            [System.Windows.Forms.SendKeys]::SendWait('n')
        }
    }
}

function Capture($process, [string]$name) {
    $process.Refresh()
    $handle = $process.MainWindowHandle
    [void][Win]::SetForegroundWindow($handle)
    Start-Sleep -Milliseconds 800
    $rect = New-Object Win+RECT
    # Window bounds without the invisible resize border
    [void][Win]::DwmGetWindowAttribute($handle, 9, [ref]$rect, 16)
    $width = $rect.Right - $rect.Left; $height = $rect.Bottom - $rect.Top
    $bitmap = New-Object System.Drawing.Bitmap $width, $height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bitmap.Size)
    $bitmap.Save((Join-Path $Out "$name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose(); $bitmap.Dispose()
    Write-Host "Saved $name ($width x $height)"
}

# The language saved by the app (user.config exists after the first start) is used on the next start
function Set-SavedLanguage([string]$code) {
    $config = Get-ChildItem -Path (Join-Path $env:LOCALAPPDATA 'TruckRemoteServer') -Filter user.config -Recurse |
        Select-Object -First 1
    if (-not $config) { throw 'user.config was not found' }
    [xml]$xml = Get-Content $config.FullName -Encoding UTF8
    $settings = $xml.configuration.userSettings.'TruckRemoteServer.Properties.Settings'
    $setting = $settings.setting | Where-Object { $_.name -eq 'Language' }
    if (-not $setting) {
        $setting = $xml.CreateElement('setting')
        $setting.SetAttribute('name', 'Language'); $setting.SetAttribute('serializeAs', 'String')
        $setting.AppendChild($xml.CreateElement('value')) | Out-Null
        $settings.AppendChild($setting) | Out-Null
    }
    $setting.value = $code
    $xml.Save($config.FullName)
    Write-Host "Language $code saved to $($config.FullName)"
}

function Run([string]$name, [int]$light) {
    Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize' AppsUseLightTheme $light -Type DWord -Force
    $process = Start-Process -FilePath $Exe -PassThru
    for ($i = 0; $i -lt 60 -and $process.MainWindowHandle -eq 0; $i++) { Start-Sleep -Milliseconds 500; $process.Refresh() }
    if ($process.HasExited) { throw "The server exited with code $($process.ExitCode)" }
    Close-Dialogs $process
    Capture $process $name
    $process.CloseMainWindow() | Out-Null
    if (-not $process.WaitForExit(10000)) { $process.Kill() }
}

New-Item -Force -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize' | Out-Null
Run 'light' 1
Run 'dark' 0
Set-SavedLanguage 'ru'
Run 'russian' 1
Run 'russian-dark' 0
