using Il2Cpp;
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

        private List<TrackInfo> trackInfos = new List<TrackInfo>();

        public Exception initError = null;

        public MelonPreferences_Entry<string> RadioTitle;
        public MelonPreferences_Entry<string> RadioFreq;
        public MelonPreferences_Entry<string> MusicDir;

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
                initError =  new Exception("Error while loading custom radio tracks.", e);
                LoggerInstance.BigError(initError.ToString());
                return;
            }

            // show warning if needed
            if (trackInfos.Count < minTracks)
            {
                MelonEvents.OnGUI.Subscribe(DrawInitWarning, 100);
            }

            MelonPreferences.Save();
        }

        private void LoadSettings ()
        {
            var prefs = MelonPreferences.CreateCategory("CustomRadio");

            RadioTitle = prefs.CreateEntry<string>("RadioTitle", "CustomRadio");
            RadioFreq = prefs.CreateEntry<string>("RadioFrequency", "Mod by: JariZ");
            var defaultBaseDir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "StarTruckerCustomRadio");
            MusicDir = prefs.CreateEntry<string>("RadioSongsDir", Path.Join(defaultBaseDir, "Songs"));

            if (!Directory.Exists(MusicDir.Value))
            {
                Directory.CreateDirectory(MusicDir.Value);
                LoggerInstance.Warning($"Created new music directory:");
                LoggerInstance.Warning($"   {MusicDir.Value}");
            }

            prefs.SaveToFile();
        }

        private void LoadTracks()
        {
            LoggerInstance.Msg("Loading tracks...");
            LoggerInstance.WriteSpacer();
            foreach (string path in Directory.EnumerateFiles(MusicDir.Value))
            {
                trackInfos.Add(new TrackInfo(path));
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
                GUI.Box(new Rect(20, 50, 600, 70), $"CustomRadio mod: less than {minTracks} tracks were loaded, please add more tracks to the music folder, the game will start to act weird if you don't.\nIt can be located at: {MusicDir.Value}\nThis message will hide after {messageOnScreenSecs} secs.", style);
            }
        }

        private void DrawInitError()
        {
            if (initError != null && Time.realtimeSinceStartup < messageOnScreenSecs)
            {
                GUIStyle style = new GUIStyle();
                style.alignment = TextAnchor.UpperLeft;
                style.normal.textColor = Color.red;
                style.fontStyle = FontStyle.Bold;
                GUI.Box(new Rect(Screen.width - 600, 50, 600, 70), $"CustomRadio mod: {initError}\nThis message will hide after {messageOnScreenSecs} secs.", style);
            }
        }

        public Il2CppSystem.Collections.Generic.List<SongDescription> GetSongDescriptions()
        {
            var list = new Il2CppSystem.Collections.Generic.List<SongDescription>();
            foreach (var (item, index) in trackInfos.WithIndex())
            {
                var song = new SongDescription();
                song.name = $"{item.Tag.Title} - {item.Tag.JoinedPerformers}";
                if (item.Tag.IsEmpty)
                {
                    song.name = Path.GetFileName(item.Path);
                }

                song.artistNameStringId = $"STR_CUSTOMTRACK_{index}_TITLE";
                song.songNameStringId = $"STR_CUSTOMTRACK_{index}_ARTIST";
                StringTable.stringTable.TryAdd(song.artistNameStringId, item.Tag.Title);
                StringTable.stringTable.TryAdd(song.songNameStringId, item.Tag.JoinedPerformers);
                song.audioClip = item.GetAudioClip();
                song.audioClipInstrumental = item.GetAudioClip(); // No idea if this is used but better be sure
                list.Add(song);
            }
            return list;
        }
    }
}