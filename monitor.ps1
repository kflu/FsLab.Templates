param($file)

$lastWrite = [datetime]::MinValue
$firstRun = $true
while ($true)
{
    $ts = (ls $file).LastWriteTimeUtc
    if ($ts -gt $lastWrite)
    {
        $lastWrite = $ts
        Write-Host "File changed: $file" -ForegroundColor Green
        if ($firstRun) {
            fsi "$PSScriptRoot\fslab.fsx" $file @args
        } else {
            fsi "$PSScriptRoot\fslab.fsx" $file --no-preview @args
        }
    }

    $firstRun = $false
    sleep 0.5 # second
}
