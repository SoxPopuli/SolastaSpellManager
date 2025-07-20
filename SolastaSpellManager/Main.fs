module SolastaSpellManager.Main

open HarmonyLib
open UnityEngine
open UnityModManagerNet

#if DEBUG
let () = Harmony.DEBUG <- true
#endif

let harmony = Harmony("com.soxpopuli.solastasavemanager")

let onToggle (modEntry: UnityModManager.ModEntry) (value: bool) =
    true

let load (modEntry: UnityModManager.ModEntry) =
    modEntry.OnToggle <- onToggle


    harmony.PatchAll()

    true


[<HarmonyPatch(typeof<RulesetCharacter>)>]
[<HarmonyPatch("UseDeviceFunction")>]
type MetamagicForItemSpells() =
    static member Prefix(__instance: RulesetCharacter, usableDevice: RulesetItemDevice, ``function``: RulesetDeviceFunction, additionalCharges: int) =
        let isSpell = ``function``.DeviceFunctionDescription.Type = DeviceFunctionDescription.FunctionType.Spell

        if isSpell then
            let service = ServiceRepository.GetService<IRulesetImplementationService>()
            let spellDef = ``function``.DeviceFunctionDescription.SpellDefinition
            let spellEffect = RulesetEffectSpell(__instance, spellDef)

            if service.IsAnyMetamagicOptionAvailable(spellEffect, __instance) then
                let meta = Gui.GuiService.GetScreen<MetamagicSelectionPanel>()
                let character = GameLocationCharacter.GetFromActor(__instance)
                meta.Bind(character, spellEffect, meta.MetamagicOptionSelected, meta.MetamagicOptionIgnored)
                meta.Show()

        ()
