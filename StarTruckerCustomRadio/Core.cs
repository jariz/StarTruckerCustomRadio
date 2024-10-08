﻿using Il2Cpp;
using Il2CppPlatformLayer;
using MelonLoader;
using System.Data;
using UnityEngine;

[assembly: MelonInfo(typeof(StarTruckerCustomRadio.Core), "StarTruckerCustomRadio", "1.0.0", "JariZ", null)]
[assembly: MelonGame("Monster And Monster", "Star Trucker")]

namespace StarTruckerCustomRadio
{
    public class Core : MelonMod
    {
        public static string customRadioNameStringId = "STR_CUSTOM_RADIO_NAME";
        public static string customRadioFreqStringId = "STR_CUSTOM_RADIO_FREQ";

        public static int minTracks = 10;
        public static int messageOnScreenSecs = 20;

        private List<TrackInfo> loadedSongs = new List<TrackInfo>();
        private List<TrackInfo> loadedAdverts = new List<TrackInfo>();
        private List<TrackInfo> loadedStings = new List<TrackInfo>();

        public Exception initError = null;
        public string initWarning = "";

        public MelonPreferences_Entry<string> RadioTitle;
        public MelonPreferences_Entry<string> RadioFreq;

        public MelonPreferences_Entry<string> MusicDir;
        public MelonPreferences_Entry<string> AdvertsDir;
        public MelonPreferences_Entry<string> StingsDir;

        public MelonPreferences_Entry<bool> DisableSongs;
        public MelonPreferences_Entry<bool> DisableAdverts;
        public MelonPreferences_Entry<bool> DisableStings;

        public override void OnInitializeMelon()
        {
            MelonEvents.OnGUI.Subscribe(DrawInitError, 100);

            try
            {
                LoadSettings();
            }
            catch (Exception e)
            {
                initError = new Exception("Error while initializing melon preferences.", e);
                LoggerInstance.BigError(initError.ToString());
                return;
            }

            try
            {
                LoadTracks();
            }
            catch (Exception e)
            {
                initError = new Exception("Error while loading custom radio tracks.", e);
                LoggerInstance.BigError(initError.ToString());
                return;
            }

            // show warning if needed
            if (initWarning.IsNotNullOrEmpty())
            {
                MelonEvents.OnGUI.Subscribe(DrawInitWarning, 100);
            }

            MelonPreferences.Save();
        }

        private void LoadSettings()
        {
            var prefs = MelonPreferences.CreateCategory("CustomRadio");

            RadioTitle = prefs.CreateEntry<string>("RadioTitle", "CustomRadio");
            RadioFreq = prefs.CreateEntry<string>("RadioFrequency", "Mod by: JariZ");

            var defaultBaseDir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "StarTruckerCustomRadio");
            MusicDir = prefs.CreateEntry<string>("RadioSongsDir", Path.Join(defaultBaseDir, "Songs"));
            AdvertsDir = prefs.CreateEntry<string>("RadioAdvertsDir", Path.Join(defaultBaseDir, "Adverts"));
            StingsDir = prefs.CreateEntry<string>("RadioStingsDir", Path.Join(defaultBaseDir, "Stings"));

            DisableSongs = prefs.CreateEntry<bool>("RadioDisableCustomSongs", false);
            DisableAdverts = prefs.CreateEntry<bool>("RadioDisableCustomAdverts", false);
            DisableStings = prefs.CreateEntry<bool>("RadioDisableCustomStings", false);

            foreach (var path in new List<string> { MusicDir.Value, AdvertsDir.Value, StingsDir.Value })
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    LoggerInstance.Warning($"Created new directory:");
                    LoggerInstance.Warning($"   {path}");
                }
            }

            prefs.SaveToFile();
        }

        private void LoadTracks()
        {
            var failedTracks = new List<string>();
            LoggerInstance.WriteSpacer();
            LoggerInstance.Msg("Loading tracks...");
            foreach (string path in Directory.EnumerateFiles(MusicDir.Value))
            {
                try
                {
                    loadedSongs.Add(new TrackInfo(path));
                }
                catch (Exception ex)
                {
                    LoggerInstance.Warning($"Failed to load '{Path.GetFileName(path)}' from Songs dir:\n{ex}");
                    failedTracks.Add(path);
                }
            }
            LoggerInstance.Msg($"- Loaded {loadedSongs.Count} songs");

            foreach (string path in Directory.EnumerateFiles(AdvertsDir.Value))
            {
                try
                {
                    loadedAdverts.Add(new TrackInfo(path));
                }
                catch (Exception ex)
                {
                    LoggerInstance.Warning($"Failed to load '{Path.GetFileName(path)}' from Songs dir:\n{ex}");
                    failedTracks.Add(path);
                }
            }
            LoggerInstance.Msg($"- Loaded {loadedAdverts.Count} adverts!");

            foreach (string path in Directory.EnumerateFiles(StingsDir.Value))
            {
                try
                {
                    loadedStings.Add(new TrackInfo(path));
                }
                catch (Exception ex)
                {
                    LoggerInstance.Warning($"Failed to load '{Path.GetFileName(path)}' from Songs dir:\n{ex}");
                    failedTracks.Add(path);
                }
            }
            LoggerInstance.Msg($"- Loaded {loadedStings.Count} stings!");
            LoggerInstance.WriteSpacer();

            if ((loadedSongs.Count + loadedAdverts.Count + loadedStings.Count) < minTracks)
            {
                initWarning += $"Less than {minTracks} total tracks were loaded, please add more tracks to the folders, the game will start to act weird if you don't.\nThe songs directory can found at: {MusicDir.Value}\n\n";
            }

            if (failedTracks.Count > 0)
            {
                var displayedTracks = failedTracks
                    .Select(path =>
                        Path.Combine(Directory.GetParent(path).Name, Path.GetFileName(path))
                    );
                initWarning += $"The following files failed to load and were skipped:\n- {string.Join("\n - ", displayedTracks)}\nRead the console/log for more information.\n\n";
            }
        }

        private void DrawInitWarning()
        {
            if (Time.realtimeSinceStartup < messageOnScreenSecs)
            {
                GUIStyle style = new GUIStyle();
                style.alignment = TextAnchor.UpperLeft;
                style.normal.textColor = Color.yellow;
                style.fontStyle = FontStyle.Bold;
                GUI.Box(new Rect(20, 50, 600, 600), $"CustomRadio mod: {initWarning}\nThis message will hide after {messageOnScreenSecs} secs.", style);
            }
        }

        private void DrawInitError()
        {
            if (initError != null && Time.realtimeSinceStartup < messageOnScreenSecs)
            {
                GUIStyle style = new GUIStyle();
                style.alignment = TextAnchor.UpperRight;
                style.normal.textColor = Color.red;
                style.fontStyle = FontStyle.Bold;
                GUI.Box(new Rect(Screen.width - 600, 50, 600, 600), $"CustomRadio mod: {initError}\nThis message will hide after {messageOnScreenSecs} secs.", style);
            }
        }

        public Il2CppSystem.Collections.Generic.List<SongDescription> GetSongDescriptions()
        {
            var list = new Il2CppSystem.Collections.Generic.List<SongDescription>();
            foreach (var (item, index) in loadedSongs.WithIndex())
            {
                var song = new SongDescription();

                var title = item.Tag.Title.IsNullOrEmpty() ? Path.GetFileNameWithoutExtension(item.Path) : item.Tag.Title;
                var artists = item.Tag.JoinedPerformers.IsNullOrEmpty() ? "Unknown" : item.Tag.JoinedPerformers;

                song.name = $"{title} - {artists}";
                song.artistNameStringId = $"STR_CUSTOMTRACK_{index}_TITLE";
                song.songNameStringId = $"STR_CUSTOMTRACK_{index}_ARTIST";
                StringTable.stringTable.TryAdd(song.artistNameStringId, title);
                StringTable.stringTable.TryAdd(song.songNameStringId, artists);
                song.audioClip = item.CreateAudioClip();
                song.audioClipInstrumental = item.CreateAudioClip(); // No idea if this is used but better be sure
                list.Add(song);
            }
            return list;
        }

        public Il2CppSystem.Collections.Generic.List<RadioStingDescription> GetStingDescriptions()
        {
            var list = new Il2CppSystem.Collections.Generic.List<RadioStingDescription>();
            foreach (var item in loadedStings)
            {
                var sting = new RadioStingDescription();
                sting.name = Path.GetFileName(item.Path);
                sting.audioClip = item.CreateAudioClip();
                list.Add(sting);
            }
            return list;
        }

        public Il2CppSystem.Collections.Generic.List<RadioAdvertDescription> GetAdvertDescriptions()
        {
            var list = new Il2CppSystem.Collections.Generic.List<RadioAdvertDescription>();
            foreach (var item in loadedAdverts)
            {
                var advert = new RadioAdvertDescription();

                advert.name = Path.GetFileName(item.Path);
                advert.audioClip = item.CreateAudioClip();
                list.Add(advert);
            }
            return list;
        }
    }
}