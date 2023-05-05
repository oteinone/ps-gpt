$releases = "win-x64","win-x86","linux-x64","osx-x64"

foreach($rid in $releases) {
    dotnet publish `
        -c Release `
        -r $rid `
        -p:PublishSingleFile=true `
        -p:PublishTrimmed=true `
        --self-contained true `
        --output "releases/$rid"

    $executable_path = ".\releases\${rid}\ps-gpt";
    if ($rid.StartsWith("win")) {
        $executable_path += ".exe";
    }

    Compress-Archive -Path $executable_path -DestinationPath ".\releases\$rid.zip" -Force
}