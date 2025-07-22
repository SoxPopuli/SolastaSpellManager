module SolastaSpellManager.MetamagicForItemSpells

open HarmonyLib
open Utils

[<HarmonyPatch(typeof<CharacterActionPanel>, "DeviceFunctionEngaged")>]
module DeviceFunctionEngagedPatch =
    type internal Marker = interface end
    let moduleType = typeof<Marker>.DeclaringType

    let patchMethod =
        AccessTools.Method(typeof<CharacterActionPanel>, "DeviceFunctionEngaged")

    let actionParamsRef =
        AccessTools.FieldRefAccess<CharacterActionPanel, CharacterActionParams>("actionParams")

    let tmpUsableDeviceFunctionRef =
        AccessTools.FieldRefAccess<CharacterActionPanel, RulesetDeviceFunction>("tmpUsableDeviceFunction")

    let Prefix
        (
            __instance: CharacterActionPanel,
            guiCharacter: GuiCharacter,
            usableDevice: RulesetItemDevice,
            usableDeviceFunction: RulesetDeviceFunction,
            addedCharges: int,
            subSpellIndex: int
        ) =
        let actionParams = actionParamsRef.Invoke(__instance)

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

            tmpUsableDeviceFunctionRef.Invoke(__instance) <- usableDeviceFunction

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

            let executeSpellCast (caster: RulesetCharacter) spellEffect =
                if willOverwriteConcentration caster spellEffect then
                    overwriteConcentrationPrompt onConfirm onCancel spellEffect
                else
                    onConfirm ()

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

                            executeSpellCast caster.RulesetCharacter spellEffect

                    let metamagicIgnored = fun () -> executeSpellCast character spell

                    let character = GameLocationCharacter.GetFromActor(character)
                    meta.Bind(character, spell, metamagicSelected, metamagicIgnored)
                    meta.Show()
                else
                    executeSpellCast character spell
            | _ -> onConfirm ()

        __instance.DeviceSelectionPanel.Hide(true)

        false

    let patch (harmony: Harmony) =
        harmony.Patch(patchMethod, prefix = (moduleType.GetMethod("Prefix") |> HarmonyMethod))
        |> ignore

    let unpatch (harmony: Harmony) =
        harmony.Unpatch(patchMethod, HarmonyPatchType.Prefix)


[<HarmonyPatch(typeof<CharacterActionCastSpell>)>]
[<HarmonyPatch("SpendMagicEffectUses")>]
module MetamagicForItemSpells =
    type internal Marker = interface end
    let moduleType = typeof<Marker>.DeclaringType

    let patchMethod =
        AccessTools.Method(typeof<CharacterActionCastSpell>, "SpendMagicEffectUses")

    let Prefix (__instance: CharacterActionCastSpell) =
        if
            __instance.ActiveSpell.OriginItem <> null
            && __instance.ActiveSpell.MetamagicOption <> null
        then
            let character = __instance.ActingCharacter.RulesetCharacter
            character.ActivateMetamagic(__instance.ActiveSpell, __instance.ActiveSpell.MetamagicOption)

    let patch (harmony: Harmony) =
        harmony.Patch(patchMethod, prefix = (moduleType.GetMethod("Prefix") |> HarmonyMethod))
        |> ignore

    let unpatch (harmony: Harmony) =
        harmony.Unpatch(patchMethod, HarmonyPatchType.Prefix)

let patch (harmony: Harmony) =
    DeviceFunctionEngagedPatch.patch harmony
    MetamagicForItemSpells.patch harmony

let unpatch (harmony: Harmony) =
    DeviceFunctionEngagedPatch.unpatch harmony
    MetamagicForItemSpells.unpatch harmony
