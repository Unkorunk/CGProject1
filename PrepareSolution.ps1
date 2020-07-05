$platform = "netcoreapp3.1"

$debug_path = "CGProject1/bin/Debug/$platform"
$release_path = "CGProject1/bin/Release/$platform"

$filename = "ffmpeg-4.3-win64-shared-lgpl"
$zip_filename = "$filename.zip"
$url = "https://ffmpeg.zeranoe.com/builds/win64/shared/$zip_filename"

New-Item $debug_path -ItemType Directory -ErrorAction SilentlyContinue
New-Item $release_path -ItemType Directory -ErrorAction SilentlyContinue

Invoke-WebRequest -Uri $url -OutFile $zip_filename

Expand-Archive -Path $zip_filename -DestinationPath .

Copy-Item $filename -Destination $debug_path -Recurse
Copy-Item $filename -Destination $release_path -Recurse

Remove-Item $zip_filename
Remove-Item $filename -Recurse
