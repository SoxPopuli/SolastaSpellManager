module SolastaSpellManager.Main

open System
open HarmonyLib
open UnityEngine
open type UnityModManagerNet.UnityModManager
open Settings
open UnityModManagerNet

let mutable staticLogger: ModEntry.ModLogger = null

type Main(modEntry: ModEntry) =
    let logger = modEntry.Logger

    let settings = Settings.Load(modEntry)

#if DEBUG
    do Harmony.DEBUG <- true
#endif

    let patchAll (harmony: Harmony) = MetamagicForItemSpells.patch harmony

    let unpatchAll (harmony: Harmony) = MetamagicForItemSpells.patch harmony

    let onToggle (modEntry: ModEntry) value =
        if not value then
            Harmony.unpatchAll ()
        else
            settings.ApplyPatches()

        true

    let onGui (modEntry: ModEntry) = settings.Draw modEntry

    let onSaveGui (modEntry: ModEntry) = settings.Save modEntry

    member _.load() =
        modEntry.OnToggle <- onToggle
        modEntry.OnGUI <- onGui
        modEntry.OnSaveGUI <- onSaveGui

        logger.Log("Loaded")
        true

let load (modEntry: ModEntry) =
    try
        Logger.staticLogger <- modEntry.Logger
        Main(modEntry).load ()
    with ex ->
        modEntry.Logger.LogException(ex)
        reraise ()
