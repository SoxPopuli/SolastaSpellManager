module SolastaSpellManager.Settings

open System.IO
open UnityEngine
open UnityModManagerNet
open type UnityModManagerNet.UnityModManager
open System
open System.Collections.Generic

module SettingsTab =
    type Tab =
        | Patches = 0
        | SpellManager = 1

    let labels = [| "Patches"; "Spell Manager" |]

module SpellManager =
    type RepertoireState =
        { expanded: bool
          spellLevelSelected: int }

        static member Default =
            { expanded = false
              spellLevelSelected = 0 }

    type PlayerState =
        { repertoireStates: Dictionary<RulesetSpellRepertoire, RepertoireState> }

    type State =
        { playerStates: Dictionary<string, PlayerState> }

    let addSpell (repertoire: RulesetSpellRepertoire) (spell: SpellDefinition) =
        if spell.SpellLevel = 0 then
            repertoire.KnownCantrips.Add(spell)
        else
            repertoire.KnownSpells.Add(spell)

    let removeSpell (repertoire: RulesetSpellRepertoire) (spell: SpellDefinition) =
        if spell.SpellLevel = 0 then
            repertoire.KnownCantrips.Remove(spell)
        else
            repertoire.KnownSpells.Remove(spell)

    let drawSpells (repertoire: RulesetSpellRepertoire) (spells: List<SpellDefinition>) (spellLevel: int) =
        for s in spells do
            GUILayout.BeginHorizontal()

            let knowsSpell = repertoire.HasKnowledgeOfSpell(s)

            let nameStyle = GUIStyle()
            nameStyle.normal.textColor <- if knowsSpell then Color.green else Color.white
            nameStyle.fontStyle <- FontStyle.Bold
            nameStyle.wordWrap <- true

            let labelStyle = GUIStyle()
            labelStyle.wordWrap <- true
            labelStyle.alignment <- TextAnchor.MiddleLeft
            labelStyle.normal.textColor <- Color.white

            GUILayout.Label(s.FormatTitle(), nameStyle, [| GUILayout.Width(160.0f) |])

            GUILayout.Space(40.0f)

            GUILayout.Label(s.FormatDescription(), labelStyle)

            let buttonWidth = GUILayout.Width(160.0f)

            GUILayout.Space(40.0f)

            if knowsSpell then
                if GUILayout.Button("Remove Spell", [| buttonWidth |]) then
                    removeSpell repertoire s |> ignore
            else if GUILayout.Button("Add Spell", [| buttonWidth |]) then
                addSpell repertoire s |> ignore

            GUILayout.EndHorizontal()

            GUILayout.Space(40.0f)

    let drawRepertoires
        (state: PlayerState)
        (player: GameLocationCharacter)
        (repertoires: List<RulesetSpellRepertoire>)
        =
        GUILayout.BeginVertical()

        for r in repertoires do
            let repState = state.repertoireStates.GetOrAdd(r, RepertoireState.Default)

            let feature = r.SpellCastingFeature

            let label = sprintf "%s" (feature.FormatTitle())
            let expanded = GUILayout.Toggle(repState.expanded, label)

            let mutable spellLevelSelected = repState.spellLevelSelected

            if expanded then
                let spellList = feature.SpellListDefinition

                let minLevel = if spellList.HasCantrips then 0 else 1
                let maxLevel = spellList.MaxSpellLevel

                let labels = seq { minLevel..maxLevel } |> Seq.map (fun i -> sprintf "L%i" i)
                spellLevelSelected <- GUILayout.Toolbar(repState.spellLevelSelected, labels |> Seq.toArray)

                let spellLevel = spellLevelSelected + minLevel
                let spells = spellList.GetSpellsOfLevel(spellLevel)

                drawSpells r spells spellLevel

            state.repertoireStates.AddOrReplace(
                r,
                { repState with
                    expanded = expanded
                    spellLevelSelected = spellLevelSelected }
            )


        GUILayout.EndVertical()

    let drawPlayer (state: State) (player: GameLocationCharacter) =
        GUILayout.BeginHorizontal()

        GUILayout.Label(player.Name, [| GUILayout.Width(120.0f) |])

        let playerState =
            state.playerStates.GetOrAdd(player.Name, { repertoireStates = Dictionary() })

        drawRepertoires playerState player player.RulesetCharacter.SpellRepertoires

        GUILayout.EndHorizontal()

    let draw state =
        let characterService =
            ServiceRepository.GetService<IGameLocationCharacterService>() |> Option.ofObj

        let partyChars =
            characterService
            |> Option.map _.PartyCharacters
            |> Option.map Seq.cast<GameLocationCharacter>
            |> Option.defaultValue Seq.empty

        if
            partyChars
            |> Seq.forall (fun c -> state.playerStates.ContainsKey(c.Name))
            |> not
        then
            state.playerStates.Clear()

        for p in partyChars do
            drawPlayer state p

[<Serializable>]
type Settings() =
    inherit ModSettings()

    member this.ApplyPatches() =
        if this.metamagicForItemSpells then
            Harmony.run MetamagicForItemSpells.patch
        else
            Harmony.run MetamagicForItemSpells.unpatch

    [<field: Draw("Enable Metamagic for Item Spells")>]
    [<field: SerializeField>]
    member val metamagicForItemSpells = true with get, set

    member val selectedTab = SettingsTab.Tab.Patches with get, set
    member val spellManagerState = { SpellManager.playerStates = Dictionary() } with get, set

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

        let settings =
            if File.Exists filePath then
                let file = File.OpenText(filePath)
                let content = file.ReadToEnd()

                modEntry.Logger.Log(sprintf "Loaded settings from %s" filePath)

                JsonUtility.FromJson<Settings>(content)
            else
                Settings()

        settings.ApplyPatches()
        settings

    member this.Draw modEntry =
        this.selectedTab <- GUILayout.Toolbar((int) this.selectedTab, SettingsTab.labels) |> enum

        match this.selectedTab with
        | SettingsTab.Tab.Patches -> this.Draw<Settings>(modEntry)
        | SettingsTab.Tab.SpellManager -> SpellManager.draw this.spellManagerState
        | x -> failwithf "Unexpected tab value %A" x

    interface IDrawable with
        member this.OnChange() = this.ApplyPatches()
