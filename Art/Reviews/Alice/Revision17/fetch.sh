#!/usr/bin/env bash
# Downloads every frame of a PixelLab animation job: fetch.sh <job_id> <out_dir> <frame_count>
# Frame 0 is the input frame; 1..N are generated.
set -e
job="$1"; out="$2"; count="$3"
mkdir -p "$out"
for ((i = 0; i <= count; i++)); do
    curl -sS -f -H "Authorization: Bearer $PIXELLAB_API_KEY" -o "$out/$(printf '%02d' "$i").png" \
        "https://api.pixellab.ai/mcp/images/$job/download?index=$i"
done
ls "$out" | wc -l
