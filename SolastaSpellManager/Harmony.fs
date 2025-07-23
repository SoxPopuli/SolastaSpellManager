module SolastaSpellManager.Harmony

open HarmonyLib

let harmony = Harmony("com.soxpopuli.solastasavemanager")

let run f = f harmony

let unpatchAll () = harmony.UnpatchAll harmony.Id
