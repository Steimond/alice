#Requires -Version 7.0
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$documents = @(
    @{ Id = 'gdd'; Name = 'Дизайн игры'; File = 'GameDesignDocument.md'; Icon = '01' },
    @{ Id = 'story'; Name = 'Основная история'; File = 'Story.md'; Icon = '02' },
    @{ Id = 'roadmap'; Name = 'План разработки'; File = 'DevelopmentRoadmap.md'; Icon = '03' },
    @{ Id = 'sources'; Name = 'Источники и права'; File = 'ContentProvenance.md'; Icon = '04' },
    @{ Id = 'technical'; Name = 'Техническая основа'; File = 'TechnicalSetup.md'; Icon = '05' }
)

function Escape-Html([string]$Text) {
    return [System.Net.WebUtility]::HtmlEncode($Text)
}

function Convert-RouteDiagram([string]$Source) {
    $edgePattern = '(?<from>\w+)(?:\[(?<fromText>[^\]]+)\])?\s*-->\s*(?:\|(?<condition>[^|]+)\|)?\s*(?<to>\w+)(?:\[(?<toText>[^\]]+)\])?'
    $edges = [regex]::Matches($Source, $edgePattern)
    if ($edges.Count -eq 0) { throw 'Route diagram contains no supported edges.' }
    $labels = @{}
    foreach ($edge in $edges) {
        foreach ($side in @('from', 'to')) {
            if ($edge.Groups[$side + 'Text'].Success) {
                $labels[$edge.Groups[$side].Value] = $edge.Groups[$side + 'Text'].Value
            }
        }
    }
    $rows = foreach ($edge in $edges) {
        $from = Escape-Html $labels[$edge.Groups['from'].Value]
        $to = Escape-Html $labels[$edge.Groups['to'].Value]
        $condition = Escape-Html $edge.Groups['condition'].Value
        "<li><span class='route-place'>$from</span><span class='route-arrow'><small>$condition</small><span aria-label='ведёт в'>→</span></span><span class='route-place'>$to</span></li>"
    }
    return "<figure class='route-map'><figcaption>Связи областей <span>Переходы и условия открытия</span></figcaption><ol>$($rows -join "`n")</ol></figure>"
}

$rendered = foreach ($document in $documents) {
    $markdown = Get-Content -LiteralPath (Join-Path $PSScriptRoot $document.File) -Raw -Encoding utf8
    $markdown = [regex]::Replace($markdown, '(?s)```mermaid\s*\r?\n(.*?)```',
        [System.Text.RegularExpressions.MatchEvaluator]{ param($match) Convert-RouteDiagram $match.Groups[1].Value })
    $html = (ConvertFrom-Markdown -InputObject $markdown).Html

    # Prefix every heading with its document ID, including duplicate headings.
    $headingCounts = @{}
    $prefix = $document.Id
    $html = [regex]::Replace($html, '(?s)<h([1-6])(?:\s+[^>]*)?>(.*?)</h\1>',
        [System.Text.RegularExpressions.MatchEvaluator]{
            param($match)
            $plain = [System.Net.WebUtility]::HtmlDecode([regex]::Replace($match.Groups[2].Value, '<[^>]+>', ''))
            $slug = [regex]::Replace($plain.ToLowerInvariant(), '[^\p{L}\p{Nd}]+', '-').Trim('-')
            if (-not $slug) { $slug = 'section' }
            if ($headingCounts.ContainsKey($slug)) { $headingCounts[$slug]++ } else { $headingCounts[$slug] = 1 }
            $suffix = if ($headingCounts[$slug] -gt 1) { '-' + $headingCounts[$slug] } else { '' }
            $level = $match.Groups[1].Value
            "<h$level id='$prefix-$slug$suffix'>$($match.Groups[2].Value)</h$level>"
        })

    # Links to bundled Markdown documents become navigation within this HTML file.
    foreach ($target in $documents) {
        $filePattern = [regex]::Escape($target.File)
        $html = [regex]::Replace($html, "href=`"$filePattern`"", "href=`"#$($target.Id)`"")
    }
    $html = [regex]::Replace($html, '(?s)<table>(.*?)</table>', '<div class="table-scroll" tabindex="0" role="region" aria-label="Таблица с горизонтальной прокруткой"><table>$1</table></div>')
    "<article class='document' id='$($document.Id)' data-title='$($document.Name)' data-icon='$($document.Icon)'>$html<footer class='document-footer'><span>Рабочий документ · Alice</span><a href='$($document.File)'>Открыть исходный текст ↗</a></footer></article>"
}

$gdd = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'GameDesignDocument.md') -Raw -Encoding utf8
$version = [regex]::Match($gdd, 'Версия:\s*\*\*([^*]+)\*\*').Groups[1].Value
if (-not $version) { throw 'Cannot read the GDD version.' }
$template = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Reader.template.html') -Raw -Encoding utf8
$output = $template.Replace('{{VERSION}}', (Escape-Html $version)).Replace('{{DOCUMENTS}}', ($rendered -join "`n"))
if ($output.Contains('{{DOCUMENTS}}') -or $output.Contains('{{VERSION}}')) { throw 'Unexpanded HTML template.' }
$outputPath = Join-Path $PSScriptRoot 'GameDesignDocument.html'
[System.IO.File]::WriteAllText($outputPath, $output, [System.Text.UTF8Encoding]::new($false))
Write-Output "Built $outputPath ($($documents.Count) documents; no network dependencies)."
