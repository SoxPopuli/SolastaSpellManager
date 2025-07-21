open System.Diagnostics
open System.IO
open System.IO.Compression

let buildDir = "./SolastaSpellManager/bin/Release/net452" |> Path.GetFullPath

Process.Start("dotnet", [| "build"; "-c"; "Release" |]).WaitForExit()

let zipFileDir = "SolastaSpellManager.zip"

let zipFile = File.Create(zipFileDir)
let zip = new ZipArchive(zipFile, ZipArchiveMode.Create, true)

let addFromFile (path: string) =
    let fileName = Path.GetFileName(path)
    let entry = zip.CreateEntry($"SolastaSpellManager/{fileName}")

    use stream = entry.Open()

    let file = File.OpenRead(path)
    file.CopyTo(stream)

    eprintfn "Added %s to %s" fileName zipFileDir


Path.Join(buildDir, "SolastaSpellManager.dll") |> addFromFile
Path.Join(buildDir, "FSharp.Core.dll") |> addFromFile
Path.Join(buildDir, "System.ValueTuple.dll") |> addFromFile
"./Info.json" |> Path.GetFullPath |> addFromFile

zip.Dispose()
zipFile.Dispose()

eprintfn "Finished!"
