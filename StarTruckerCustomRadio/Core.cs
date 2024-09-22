using Harmony;
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

        public static int minSongs = 10;
        public static int messageOnScreenSecs = 20;

        private List<TrackSource> loadedSongs = new List<TrackSource>();
        private List<TrackSource> loadedAdverts = new List<TrackSource>();
        private List<TrackSource> loadedStings = new List<TrackSource>();

        public Exception initError = null;

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
            // should probably also unsubscribe but in a world where shipping things like electron is the norm? who cares

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
            if (loadedSongs.Count < minSongs)
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
                    LoggerInstance.Warning($"   {MusicDir.Value}");
                }
            }

            prefs.SaveToFile();
        }

        private void LoadTracks()
        {
            LoggerInstance.WriteSpacer();
            //LoggerInstance.Msg("Loading tracks...");
            //foreach (string path in Directory.EnumerateFiles(MusicDir.Value))
            //{
            //    loadedSongs.Add(new FileTrackSource(path));
            //}

            loadedSongs.Add(new UriTrackSource("https://intenseradio.live-streams.nl:18000/live"));

            LoggerInstance.Msg($"- Loaded {loadedSongs.Count} songs");

            foreach (string path in Directory.EnumerateFiles(AdvertsDir.Value))
            {
                loadedAdverts.Add(new FileTrackSource(path));
            }
            LoggerInstance.Msg($"- Loaded {loadedAdverts.Count} adverts!");

            foreach (string path in Directory.EnumerateFiles(StingsDir.Value))
            {
                loadedStings.Add(new FileTrackSource(path));
            }
            LoggerInstance.Msg($"- Loaded {loadedStings.Count} stings!");
            LoggerInstance.WriteSpacer();
        }

        private void DrawInitWarning()
        {
            if (Time.realtimeSinceStartup < messageOnScreenSecs)
            {
                GUIStyle style = new GUIStyle();
                style.alignment = TextAnchor.UpperLeft;
                style.normal.textColor = Color.yellow;
                style.fontStyle = FontStyle.Bold;
                GUI.Box(new Rect(20, 50, 600, 70), $"CustomRadio mod: less than {minSongs} songs were loaded, please add more songs to the music folder, the game will start to act weird if you don't.\nIt can be located at: {MusicDir.Value}\nThis message will hide after {messageOnScreenSecs} secs.", style);
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
            foreach (var (item, index) in loadedSongs.WithIndex())
            {
                var song = new SongDescription();
                song.artistNameStringId = $"STR_CUSTOMTRACK_{index}_TITLE";
                song.songNameStringId = $"STR_CUSTOMTRACK_{index}_ARTIST";
                var metaData = item.GetMetadata();
                StringTable.stringTable.TryAdd(song.artistNameStringId, metaData.Title);
                StringTable.stringTable.TryAdd(song.songNameStringId, metaData.Artist);
                song.audioClip = item.CreateAudioClip();
                song.audioClipInstrumental = item.CreateAudioClip(); // No idea if this is used but better be sure
                list.Add(song);
            }
            return list;
        }

        public Il2CppSystem.Collections.Generic.List<RadioStingDescription> GetStingDescriptions()
        {
            var list = new Il2CppSystem.Collections.Generic.List<RadioStingDescription>();
            foreach (var (item, index) in loadedStings.WithIndex())
            {
                var sting = new RadioStingDescription();
                var metaData = item.GetMetadata();
                sting.name = Path.GetFileName(metaData.Title);
                sting.audioClip = item.CreateAudioClip();
                list.Add(sting);
            }
            return list;
        }

        public Il2CppSystem.Collections.Generic.List<RadioAdvertDescription> GetAdvertDescriptions()
        {
            var list = new Il2CppSystem.Collections.Generic.List<RadioAdvertDescription>();
            foreach (var (item, index) in loadedStings.WithIndex())
            {
                var sting = new RadioAdvertDescription();
                var metaData = item.GetMetadata();
                sting.name = Path.GetFileName(metaData.Title);
                sting.audioClip = item.CreateAudioClip();
                list.Add(sting);
            }
            return list;
        }
    }
}