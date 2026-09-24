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

    function Show-RemediationBox ($title, $failurePoint, $cause, $steps, $quickCommand) {
        Write-Host
        Write-Host "╔══════════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Red
        Write-Host "║ ❌ ҚАТЕ АНЫҚТАЛҒАН ОРЫН (ТОЧКА СБОЯ): $failurePoint" -ForegroundColor Red
        Write-Host "╠══════════════════════════════════════════════════════════════════════════════╣" -ForegroundColor Red
        Write-Host "║ ⚠️  Атауы:   $title" -ForegroundColor Yellow
        Write-Host "║ 🔍 Не болды: $cause" -ForegroundColor Gray
        Write-Host "║"
        Write-Host "║ 💡 АЛДЫМЕН МЫНАНЫ ДҰРЫСТАҢЫЗ (СНАЧАЛА ИСПРАВЬТЕ ЭТО):" -ForegroundColor Cyan
        $i = 1
        foreach ($step in $steps) {
            Write-Host "║    [$i] $step" -ForegroundColor White
            $i++
        }
        if ($quickCommand) {
            Write-Host "║"
            Write-Host "║ ⌨️  Шұғыл пәрмен (Быстрая команда):" -ForegroundColor Green
            Write-Host "║    $quickCommand" -ForegroundColor White
        }
        Write-Host "╚══════════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Red
        Write-Host
    }

    # 1. Check PowerShell Language Mode
    if ($ExecutionContext.SessionState.LanguageMode.value__ -ne 0) {
        Show-RemediationBox `
            -title "PowerShell Constrained Language Mode белсенді" `
            -failurePoint "PowerShell қауіпсіздік саясаты (Language Mode)" `
            -cause "Жүйелік саясат немесе AppLocker сценарийлерді шектеулі режимде орындауды талап етуде." `
            -steps @(
                "PowerShell терезесін қарапайым пайдаланушы ретінде ашып қайталап көріңіз.",
                "Немесе әкімшіден осы терминал сессиясы үшін ConstrainedLanguage шектеуін алуды сұраңыз."
            )
        return
    }

    # 2. Test .NET runtime accessibility
    try {
        [void][System.AppDomain]::CurrentDomain.GetAssemblies()
        [void][System.Math]::Sqrt(144)
    }
    catch {
        Show-RemediationBox `
            -title "PowerShell .NET ортасына қол жеткізе алмады" `
            -failurePoint "PowerShell .NET CLR ортасы" `
            -cause $_.Exception.Message `
            -steps @(
                "PowerShell нұсқасын тексеріңіз (Windows PowerShell 5.1 немесе PowerShell 7+ ұсынылады).",
                "Компьютерді қайта қосып көріңіз."
            )
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
        [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
    } catch {}

    # 5. Pinned SHA-256 Hash and Mirrors
    $expectedHash = '87705F9BA7E4FBD60690B9B4B4C1C0AA4737F1CF17F0C45EA545547AF5597D33'

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
        Show-RemediationBox `
            -title "Пакетті серверден жүктеу мүмкін болмады" `
            -failurePoint "Интернет байланысы / Репозиторий" `
            -cause "Қолжетімді mirror-серверлерге қосылу сәтсіз аяқталды." `
            -steps @(
                "Интернет байланысын тексеріңіз.",
                "GitHub-тың қолжетімді екенін шолғыштан тексеріңіз (https://github.com/Azamaperdeev05/lan-share-manager).",
                "Сыртқы антивирустың жүктеуді бұғаттамағанына көз жеткізіңіз."
            )
        return
    }

    # 6. Verify SHA-256 Hash Integrity (Hash Pinning)
    Write-Host "Verifying SHA-256 package integrity... " -NoNewline
    $fileBytes = [IO.File]::ReadAllBytes($tempZip)
    $sha256 = [Security.Cryptography.SHA256]::Create()
    $computedHash = [BitConverter]::ToString($sha256.ComputeHash($fileBytes)) -replace '-'

    if ($computedHash -ne $expectedHash) {
        Write-Host "FAILED!" -ForegroundColor Red
        Show-RemediationBox `
            -title "Қауіпсіздік хэші сәйкес келмеді (Hash Mismatch)" `
            -failurePoint "Пакеттің тұтастығын тексеру (SHA-256)" `
            -cause "Күтілген хэш: $expectedHash`nАлынған хэш: $computedHash" `
            -steps @(
                "Жүктелген файл бүлінген болуы мүмкін. Пәрменді қайта іске қосыңыз.",
                "Прокси немесе жергілікті желідегі антивирусты тексеріңіз."
            )
        Remove-Item -Path $tempZip -Force -ErrorAction SilentlyContinue
        return
    }
    Write-Host "OK (Hash Verified ✓)" -ForegroundColor Green

    # 7. Unpack and Execute
    try {
        Write-Host "Extracting application to temporary workspace... " -NoNewline
        New-Item -ItemType Directory -Path $tempExtract -Force | Out-Null
        
        # Unzip with robust 3-tier fallback
        $unzipped = $false
        try {
            Add-Type -AssemblyName 'System.IO.Compression.FileSystem' -ErrorAction Stop
            [System.IO.Compression.ZipFile]::ExtractToDirectory($tempZip, $tempExtract)
            $unzipped = $true
        } catch { }

        if (-not $unzipped) {
            try {
                Expand-Archive -Path $tempZip -DestinationPath $tempExtract -Force -ErrorAction Stop
                $unzipped = $true
            } catch { }
        }

        if (-not $unzipped) {
            try {
                tar -xf $tempZip -C $tempExtract
                $unzipped = $true
            } catch { }
        }

        if (-not $unzipped) {
            Write-Host "FAILED!" -ForegroundColor Red
            Show-RemediationBox `
                -title "ZIP мұрағатын ашу мүмкін болмады" `
                -failurePoint "Файлдық жүйе / Декомпрессия" `
                -cause "Windows ортасындағы барлық 3 декомпрессия әдісі сәтсіз аяқталды." `
                -steps @(
                    "Дискіде бос орын бар екенін тексеріңіз.",
                    "%TEMP% папкасына жазу рұқсатын тексеріңіз."
                )
            return
        }
        Write-Host "OK" -ForegroundColor Green

        $guiExe = "$tempExtract\LANShareManagerGUI.exe"
        $cliExe = "$tempExtract\LANShareManager.exe"

        if (-not (Test-Path $guiExe)) {
            Write-Host "Executable LANShareManagerGUI.exe not found in package." -ForegroundColor Red
            return
        }

        # Check if .NET 8 Desktop Runtime is installed
        $hasDesktopRuntime = $false
        try {
            $runtimes = & dotnet --list-runtimes 2>$null
            if ($runtimes -match 'Microsoft\.WindowsDesktop\.App\s+8\.') {
                $hasDesktopRuntime = $true
            }
        } catch { }

        if (-not $hasDesktopRuntime) {
            $regKeys = @(
                'HKLM:\SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App',
                'HKLM:\SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App'
            )
            foreach ($key in $regKeys) {
                if (Test-Path $key) {
                    $sub = Get-ItemProperty $key -ErrorAction SilentlyContinue
                    if ($sub -and ($sub.PSObject.Properties.Name -match '^8\.')) {
                        $hasDesktopRuntime = $true
                        break
                    }
                }
            }
        }

        if (-not $hasDesktopRuntime) {
            $desktopDir = "$env:ProgramFiles\dotnet\shared\Microsoft.WindowsDesktop.App"
            if (Test-Path $desktopDir) {
                $versions = Get-ChildItem -Path $desktopDir -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -like '8.*' }
                if ($versions) {
                    $hasDesktopRuntime = $true
                }
            }
        }

        if (-not $hasDesktopRuntime) {
            Write-Host "Notice: Microsoft .NET 8 Desktop Runtime is required for GUI." -ForegroundColor Yellow
            Write-Host "Downloading official Microsoft .NET 8 Desktop Runtime (x64)... " -NoNewline
            $dotnetUrl = 'https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe'
            $dotnetInstaller = "$tempExtract\dotnet8-desktop-runtime-installer.exe"

            try {
                $webClient = New-Object System.Net.WebClient
                $webClient.DownloadFile($dotnetUrl, $dotnetInstaller)
                Write-Host "OK (Downloaded)" -ForegroundColor Green

                Write-Host "Installing .NET 8 Desktop Runtime (please wait)... " -NoNewline
                $isAdmin = [bool]([Security.Principal.WindowsIdentity]::GetCurrent().Groups -match 'S-1-5-32-544')
                if ($isAdmin) {
                    $installProc = Start-Process -FilePath $dotnetInstaller -ArgumentList '/install /quiet /norestart' -PassThru -Wait
                } else {
                    $installProc = Start-Process -FilePath $dotnetInstaller -ArgumentList '/install /passive /norestart' -Verb RunAs -PassThru -Wait
                }

                if ($installProc.ExitCode -eq 0 -or $installProc.ExitCode -eq 3010) {
                    Write-Host "OK (Installed ✓)" -ForegroundColor Green
                } else {
                    Write-Host "ExitCode: $($installProc.ExitCode)" -ForegroundColor Yellow
                }
            }
            catch {
                Write-Warning "Could not automatically install .NET 8 runtime: $($_.Exception.Message)"
                Write-Host "Please install .NET 8 Desktop Runtime manually from: $dotnetUrl" -ForegroundColor Cyan
            }
        }

        # 8. Interactive Mode Selector or Direct Launch
        $isAdmin = [bool]([Security.Principal.WindowsIdentity]::GetCurrent().Groups -match 'S-1-5-32-544')
        $launchMode = 'GUI'

        if ($args -contains '-cli' -or $args -contains '--cli' -or $args -contains '-menu' -or $args -contains '--menu') {
            $launchMode = 'CLI'
        } elseif (-not [Console]::IsInputRedirected) {
            Write-Host
            Write-Host " МӘЗІР: Қай режимде іске қосамыз? (Выберите режим):" -ForegroundColor White
            Write-Host " [1] Графикалық терезе (WPF GUI) [Enter - Әдепкі]" -ForegroundColor Green
            Write-Host " [2] Терминалдағы интерактивті шебер (CLI Wizard)" -ForegroundColor Cyan
            Write-Host
            $choice = Read-Host " Таңдауыңыз [1/2, Әдепкі 1]"
            if ($choice -eq '2') {
                $launchMode = 'CLI'
            }
        }

        if ($launchMode -eq 'CLI' -and (Test-Path $cliExe)) {
            Write-Host "Терминалда интерактивті шебер іске қосылуда..." -ForegroundColor Cyan
            if ($isAdmin) {
                & $cliExe
            } else {
                Write-Host "Әкімші құқықтарымен жаңа консоль терезесі ашылуда..." -ForegroundColor Yellow
                $proc = Start-Process -FilePath $cliExe -Verb RunAs -PassThru
                $proc.WaitForExit()
            }
        } else {
            Write-Host "Launching LAN Share Manager with Administrator privileges..." -ForegroundColor Cyan
            if ($isAdmin) {
                $proc = Start-Process -FilePath $guiExe -PassThru
                $proc.WaitForExit()
            } else {
                $proc = Start-Process -FilePath $guiExe -Verb RunAs -PassThru
                $proc.WaitForExit()
            }
        }
    }
    finally {
        # 9. Clean up temporary files completely
        Write-Host "Cleaning up temporary files... " -NoNewline
        Remove-Item -Path $tempZip -Force -ErrorAction SilentlyContinue
        Remove-Item -Path $tempExtract -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "Done ✓" -ForegroundColor Green
    }
} @args
