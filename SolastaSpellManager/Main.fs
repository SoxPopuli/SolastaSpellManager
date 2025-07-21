module SolastaSpellManager.Main

open System
open HarmonyLib
open UnityEngine
open UnityModManagerNet

// #if DEBUG
// #endif

let mutable logger = null

let onToggle (modEntry: UnityModManager.ModEntry) (value: bool) = true

let load (modEntry: UnityModManager.ModEntry) =
    Harmony.DEBUG <- true
    let harmony = Harmony("com.soxpopuli.solastasavemanager")

    modEntry.OnToggle <- onToggle

    logger <- modEntry.Logger

    harmony.PatchAll()

    logger.Log("Initialized")

    true

[<HarmonyPatch(typeof<CharacterActionPanel>)>]
[<HarmonyPatch("DeviceFunctionEngaged")>]
type DeviceFunctionEngagedPatch =
    static member actionParamsRef =
        AccessTools.FieldRefAccess<CharacterActionPanel, CharacterActionParams>("actionParams")

    static member tmpUsableDeviceFunctionRef =
        AccessTools.FieldRefAccess<CharacterActionPanel, RulesetDeviceFunction>("tmpUsableDeviceFunction")

    static member Prefix
        (
            __instance: CharacterActionPanel,
            guiCharacter: GuiCharacter,
            usableDevice: RulesetItemDevice,
            usableDeviceFunction: RulesetDeviceFunction,
            addedCharges: int,
            subSpellIndex: int
        ) =
        let actionParams = DeviceFunctionEngagedPatch.actionParamsRef.Invoke(__instance)

        let confirmed =
            AccessTools.Method(typeof<CharacterActionPanel>, "DeviceFunctionConfirmed")

        let canceled =
            AccessTools.Method(typeof<CharacterActionPanel>, "DeviceFunctionCancelled")

        let onConfirm = fun () -> confirmed.Invoke(__instance, [||]) |> ignore
        let onCancel = fun () -> canceled.Invoke(__instance, [||]) |> ignore

        if actionParams <> null then
            let service = ServiceRepository.GetService<IRulesetImplementationService>()
            let character = guiCharacter.RulesetCharacter

            actionParams.RulesetEffect <-
                service.InstantiateActiveDeviceFunction(
                    character,
                    usableDevice,
                    usableDeviceFunction,
                    addedCharges,
                    true,
                    subSpellIndex
                )

            DeviceFunctionEngagedPatch.tmpUsableDeviceFunctionRef.Invoke(__instance) <- usableDeviceFunction

            let overwriteConcentrationPrompt onConfirm onCancel (spell: RulesetEffectSpell) =
                let concentratedSpellDef =
                    guiCharacter.RulesetCharacter.ConcentratedSpell.SpellDefinition

                Gui.GuiService.ShowMessage(
                    MessageModal.Severity.Attention2,
                    Gui.Localize("Message/&ConcentrationLossTitle"),
                    Gui.Format(
                        "Message/&ConcentrationLossDescription",
                        spell.SpellDefinition.GuiPresentation.Title,
                        concentratedSpellDef.GuiPresentation.Title
                    ),
                    "Message/&MessageYesTitle",
                    "Message/&MessageNoTitle",
                    onConfirm,
                    onCancel
                )

            let willOverwriteConcentration (character: RulesetCharacter) (spell: RulesetEffectSpell) =
                character.ConcentratedSpell <> null
                && spell.SpellDefinition.RequiresConcentration

            match actionParams.RulesetEffect with
            | :? RulesetEffectSpell as spell ->
                if
                    character.IsMetamagicToggled
                    && service.IsAnyMetamagicOptionAvailable(spell, character)
                then
                    let meta = Gui.GuiService.GetScreen<MetamagicSelectionPanel>()

                    let metamagicSelected =
                        fun (caster: GameLocationCharacter) (spellEffect: RulesetEffectSpell) metamagicOption ->
                            spellEffect.MetamagicOption <- metamagicOption

                            if willOverwriteConcentration caster.RulesetCharacter spellEffect then
                                overwriteConcentrationPrompt onConfirm onCancel spellEffect
                            else
                                onConfirm ()

                    let metamagicIgnored =
                        fun () ->
                            if willOverwriteConcentration character spell then
                                overwriteConcentrationPrompt onConfirm onCancel spell
                            else
                                onConfirm ()

                    let character = GameLocationCharacter.GetFromActor(character)
                    meta.Bind(character, spell, metamagicSelected, metamagicIgnored)
                    meta.Show()

                else if willOverwriteConcentration character spell then
                    overwriteConcentrationPrompt onConfirm onCancel spell
                else
                    onConfirm ()
            | _ -> onConfirm ()

        __instance.DeviceSelectionPanel.Hide(true)

        false

[<HarmonyPatch(typeof<CharacterActionCastSpell>)>]
[<HarmonyPatch("SpendMagicEffectUses")>]
type MetamagicForItemSpells() =
    static member Prefix(__instance: CharacterActionCastSpell) =
        if
            __instance.ActiveSpell.OriginItem <> null
            && __instance.ActiveSpell.MetamagicOption <> null
        then
            let character = __instance.ActingCharacter.RulesetCharacter
            character.ActivateMetamagic(__instance.ActiveSpell, __instance.ActiveSpell.MetamagicOption)
