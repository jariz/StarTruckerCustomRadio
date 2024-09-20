using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace StarTruckerCustomRadio
{
    [HarmonyPatch(typeof(RadioStationInstance), nameof(RadioStationInstance.EnqueueNextPlaylistItem))]
    public static class RadioStationInstancePatch
    {
        static void Postfix(RadioStationInstance __instance)
        {
            Melon<Core>.Logger.Msg($"-------PLAYLIST---------");
            Melon<Core>.Logger.Msg($"CURR INDEX: {__instance.CurrentPlaylistEntryIdx}");
            foreach (var entry in __instance.Playlist)
            {
                
                Melon<Core>.Logger.Msg($"-- DESC: {entry.itemDesc.name}");
                Melon<Core>.Logger.Msg($"-- TYPE: {entry.itemType}");
                Melon<Core>.Logger.Msg($"-- DRTN: {entry.duration}");
                Melon<Core>.Logger.WriteSpacer();
            }
            Melon<Core>.Logger.Msg($"------------------------");
        }
    }


    [HarmonyPatch(typeof(HiFi), nameof(HiFi.Init))]
    public static class HiFiPatch
    {
        static void Prefix(HiFi __instance)
        {
            try
            {
                StringTable.stringTable.TryAdd(Core.customRadioNameStringId, "CustomRadio");
                StringTable.stringTable.TryAdd(Core.customRadioFreqStringId, "Your harddrive");

                var goldRock = __instance.m_radioStationDef;

                var radioStation = new RadioStationDescription
                {
                    stationNameStringId = Core.customRadioNameStringId,
                    stationFreqStringId = Core.customRadioFreqStringId,
                    songs = Melon<Core>.Instance.GetSongDescriptions(),
                    adverts = new Il2CppSystem.Collections.Generic.List<RadioAdvertDescription>(),
                    stings = new Il2CppSystem.Collections.Generic.List<RadioStingDescription>(),
                    //adverts = goldRock.adverts,
                    //stings = goldRock.stings,
                };

                __instance.m_radioStationDef = radioStation;
            }
            catch (Exception ex)
            {
                Melon<Core>.Logger.Error(ex);
            }


        }
    }
}