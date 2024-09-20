using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using System.Text;
using UnityEngine;

namespace StarTruckerCustomRadio
{

    [HarmonyPatch(typeof(HiFi), nameof(HiFi.Init))]
    public static class HiFiPatch
    {
        private static void Prefix(HiFi __instance)
        {
            try
            {
                StringTable.stringTable.Add(Core.customRadioNameStringId, "CustomRadio");
                StringTable.stringTable.Add(Core.customRadioFreqStringId, "Your harddrive");

                var radioStation = new RadioStationDescription
                {
                    stationNameStringId = Core.customRadioNameStringId,
                    stationFreqStringId = Core.customRadioFreqStringId,
                    songs = Melon<Core>.Instance.GetSongDescriptions(),
                    adverts = new Il2CppSystem.Collections.Generic.List<RadioAdvertDescription>(),
                    stings = new Il2CppSystem.Collections.Generic.List<RadioStingDescription>()
                };

                __instance.m_radioStationDef = radioStation;
            }
            catch (Exception ex)
            {
                Melon<StarTruckerCustomRadio.Core>.Logger.Error(ex);
            }


        }
    }
}