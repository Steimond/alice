# Downloads the frames of a PixMiniMax job into alice/<name>-raw/NN.png (frame 00 is the input frame).
# The API key is read from the environment and never printed.
param([Parameter(Mandatory)][string]$Name, [Parameter(Mandatory)][string]$Job, [Parameter(Mandatory)][int]$Frames)
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$dir = Join-Path $here "alice\$Name-raw"
New-Item -ItemType Directory -Force $dir | Out-Null
$bad = 0
for ($i = 0; $i -le $Frames; $i++) {
    $target = Join-Path $dir ("{0:D2}.png" -f $i)
    $code = curl.exe -sS -w "%{http_code}" -H "Authorization: Bearer $env:PIXELLAB_API_KEY" -o $target "https://api.pixellab.ai/mcp/images/$Job/download?index=$i"
    if ($code -ne '200') { $bad++; Remove-Item $target -ErrorAction SilentlyContinue }
}
"{0}: {1} frames, {2} failed" -f $Name, ($Frames + 1 - $bad), $bad
