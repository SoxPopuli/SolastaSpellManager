module Settings

open System.IO
open UnityEngine
open UnityModManagerNet
open type UnityModManagerNet.UnityModManager
open System

[<Serializable>]
type Settings() =
    inherit ModSettings()
    let onChange () = Logger.log "OnChange called"

    do onChange ()

    [<field: Draw("Enable Metamagic for Item Spells")>]
    [<field: SerializeField>]
    member val metamagicForItemSpells = true with get, set

    static member settingsFilePath(modEntry: ModEntry) =
        Path.Combine([| modEntry.Path; "Settings.json" |])

    override this.Save(modEntry: ModEntry) =
        let filePath = Settings.settingsFilePath modEntry
        let json = JsonUtility.ToJson(this)

        let file = File.Create(filePath)
        use writer = new StreamWriter(file)

        writer.Write(json)

        modEntry.Logger.Log(sprintf "Saved settings to %s" filePath)

    static member Load(modEntry: ModEntry) : Settings =
        let filePath = Settings.settingsFilePath modEntry

        if File.Exists filePath then
            let file = File.OpenText(filePath)
            let content = file.ReadToEnd()

            modEntry.Logger.Log(sprintf "Loaded settings from %s" filePath)

            JsonUtility.FromJson(content)
        else
            Settings()

    member this.Draw modEntry = this.Draw<Settings>(modEntry)

    interface IDrawable with
        member this.OnChange() = onChange ()
