namespace SolastaSpellManager

open HarmonyLib

module Global =
    let () =
#if DEBUG
        Harmony.DEBUG <- true
#endif
        ()

    let harmony = Harmony("com.soxpopuli.solastasavemanager")


module Main =
    let load () =
        ()
