module SolastaSpellManager.Logger

open type UnityModManagerNet.UnityModManager.ModEntry

let mutable staticLogger: ModLogger = null

let log msg = staticLogger.Log msg
let error msg = staticLogger.Error msg
let logException e = staticLogger.LogException e
