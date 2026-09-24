# This script is hosted on <b>https://azamaperdeev05.github.io/lsm</b> for <b>LAN Share Manager</b><br><br>
# LAN Share Manager Web Launcher | https://github.com/Azamaperdeev05/lan-share-manager<hr><pre>

if (-not $args) {
    Write-Host
    Write-Host '==================================================' -ForegroundColor Cyan
    Write-Host ' LAN Share Manager — Web Launcher (Windows 10/11)' -ForegroundColor Cyan
    Write-Host ' https://github.com/Azamaperdeev05/lan-share-manager' -ForegroundColor DarkGray
    Write-Host '==================================================' -ForegroundColor Cyan
    Write-Host
}

& {
    $psv = (Get-Host).Version.Major
    $repoUrl = 'https://github.com/Azamaperdeev05/lan-share-manager'

    # 1. Check PowerShell Language Mode
    if ($ExecutionContext.SessionState.LanguageMode.value__ -ne 0) {
        Write-Host "PowerShell is not running in Full Language Mode ($($ExecutionContext.SessionState.LanguageMode))." -ForegroundColor Red
        Write-Host "Run PowerShell as a standard user or disable ConstrainedLanguage mode." -ForegroundColor Yellow
        return
    }

    # 2. Test .NET runtime accessibility
    try {
        [void][System.AppDomain]::CurrentDomain.GetAssemblies()
        [void][System.Math]::Sqrt(144)
    }
    catch {
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "PowerShell failed to execute standard .NET commands." -ForegroundColor Yellow
        return
    }

    # 3. Check for 3rd-party Antivirus that might interfere with SMB or temp execution
    function Check3rdPartyAV {
        try {
            $cmd = if ($psv -ge 3) { 'Get-CimInstance' } else { 'Get-WmiObject' }
            $avList = & $cmd -Namespace root\SecurityCenter2 -Class AntiVirusProduct -ErrorAction SilentlyContinue |
                Where-Object { $_.displayName -notlike '*windows*' } |
                Select-Object -ExpandProperty displayName

            if ($avList) {
                Write-Host '3rd-party Antivirus detected: ' -ForegroundColor Yellow -NoNewline
                Write-Host " $($avList -join ', ')" -ForegroundColor Cyan
                Write-Host 'If connection fails, check if antivirus is blocking SMB port 445.' -ForegroundColor DarkGray
            }
        } catch {}
    }
    Check3rdPartyAV

    # 4. Check for CMD AutoRun registry entries (could crash subprocesses)
    $autorunPaths = @('HKCU:\SOFTWARE\Microsoft\Command Processor', 'HKLM:\SOFTWARE\Microsoft\Command Processor')
    foreach ($path in $autorunPaths) {
        if (Get-ItemProperty -Path $path -Name 'Autorun' -ErrorAction SilentlyContinue) {
            Write-Warning "Autorun registry key found at '$path'. If issues occur, remove 'Autorun' property."
        }
    }

    # Ensure TLS 1.2 / TLS 1.3
    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    } catch {}

    # 5. Pinned SHA-256 Hash and Mirrors
    $expectedHash = 'E88D94278FA35175F21F3E430C9FDCD1E1BD4BA1FDA8851478D07EAA79A921C8'

    $mirrors = @(
        'https://github.com/Azamaperdeev05/lan-share-manager/releases/download/v1.0.0/LANShareManager-v1.0.0-win-x64.zip',
        'https://raw.githubusercontent.com/Azamaperdeev05/lan-share-manager/main/publish/LANShareManager-v1.0.0-win-x64.zip'
    )

    $rand = [Guid]::NewGuid().Guid
    $tempZip = "$env:TEMP\LSM_$rand.zip"
    $tempExtract = "$env:TEMP\LSM_$rand"

    Write-Progress -Activity "Downloading LAN Share Manager..." -Status "Connecting to repository"
    $downloadSuccess = $false
    $errors = @()

    foreach ($url in $mirrors | Sort-Object { Get-Random }) {
        try {
            Write-Host "Downloading package from mirror: " -ForegroundColor DarkGray -NoNewline
            Write-Host $url -ForegroundColor Cyan
            
            if ($psv -ge 3) {
                Invoke-WebRequest -Uri $url -OutFile $tempZip -UseBasicParsing -TimeoutSec 60
            } else {
                $wc = New-Object Net.WebClient
                $wc.DownloadFile($url, $tempZip)
            }

            if (Test-Path $tempZip) {
                $downloadSuccess = $true
                break
            }
        }
        catch {
            $errors += $_
        }
    }
    Write-Progress -Activity "Downloading LAN Share Manager..." -Status "Completed" -Completed

    if (-not $downloadSuccess -or -not (Test-Path $tempZip)) {
        Write-Host "Failed to download LAN Share Manager from available mirrors." -ForegroundColor Red
        foreach ($err in $errors) {
            Write-Host "Error: $($err.Exception.Message)" -ForegroundColor DarkRed
        }
        return
    }

    # 6. Verify SHA-256 Hash Integrity (Hash Pinning)
    Write-Host "Verifying SHA-256 package integrity... " -NoNewline
    $fileBytes = [IO.File]::ReadAllBytes($tempZip)
    $sha256 = [Security.Cryptography.SHA256]::Create()
    $computedHash = [BitConverter]::ToString($sha256.ComputeHash($fileBytes)) -replace '-'

    if ($computedHash -ne $expectedHash) {
        Write-Host "FAILED!" -ForegroundColor Red
        Write-Warning "Security hash mismatch!`nExpected: $expectedHash`nReceived: $computedHash`nAborting execution for safety."
        Remove-Item -Path $tempZip -Force -ErrorAction SilentlyContinue
        return
    }
    Write-Host "OK (Hash Verified ✓)" -ForegroundColor Green

    # 7. Unpack and Execute with Elevation (RunAs)
    try {
        Write-Host "Extracting application to temporary workspace... " -NoNewline
        New-Item -ItemType Directory -Path $tempExtract -Force | Out-Null
        
        # Unzip
        [System.IO.Compression.ZipFile]::ExtractToDirectory($tempZip, $tempExtract)
        Write-Host "OK" -ForegroundColor Green

        $guiExe = "$tempExtract\LANShareManagerGUI.exe"
        if (-not (Test-Path $guiExe)) {
            Write-Host "Executable LANShareManagerGUI.exe not found in package." -ForegroundColor Red
            return
        }

        Write-Host "Launching LAN Share Manager with Administrator privileges..." -ForegroundColor Cyan
        $isAdmin = [bool]([Security.Principal.WindowsIdentity]::GetCurrent().Groups -match 'S-1-5-32-544')

        if ($isAdmin) {
            $proc = Start-Process -FilePath $guiExe -PassThru
            $proc.WaitForExit()
        } else {
            $proc = Start-Process -FilePath $guiExe -Verb RunAs -PassThru
            $proc.WaitForExit()
        }
    }
    finally {
        # 8. Clean up temporary files completely
        Write-Host "Cleaning up temporary files... " -NoNewline
        Remove-Item -Path $tempZip -Force -ErrorAction SilentlyContinue
        Remove-Item -Path $tempExtract -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "Done ✓" -ForegroundColor Green
    }
} @args
