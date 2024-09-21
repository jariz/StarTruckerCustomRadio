using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using NAudio.Wave;
using System.Reflection.PortableExecutable;
using TagLib;
using TagLib.Riff;
using UnityEngine;
using UnityEngine.Rendering.UI;

[assembly: MelonInfo(typeof(StarTruckerCustomRadio.Core), "StarTruckerCustomRadio", "1.0.0", "Jari Zwarts", null)]
[assembly: MelonGame("Monster And Monster", "Star Trucker")]

namespace StarTruckerCustomRadio
{
    public class TrackInfo
    {
        public Tag Tag;
        public string Path;

        private AudioClip audioClip;

        private int sampleRate = 44100;
        private int channels = 2; // Stereo

        private Mp3FileReader mp3Reader;
        private WaveStream waveStream;

        public TrackInfo(string path)
        {
            var tagFile = TagLib.File.Create(path);
            Tag = tagFile.Tag;
            this.Path = path;
            //streamedAudioClip = new StreamedAudioClip(path);
        }

        private float[] ConvertByteToFloatArray(byte[] input)
        {
            // Convert WAV byte data to float array
            int len = input.Length / 2;
            float[] result = new float[len];
            for (int i = 0; i < len; i++)
            {
                result[i] = (short)(input[i * 2] | input[i * 2 + 1] << 8) / 32768f;
            }
            return result;
        }

        public AudioClip GetAudioClip()
        {
            mp3Reader = new Mp3FileReader(Path);
            waveStream = WaveFormatConversionStream.CreatePcmStream(mp3Reader);

            var readerCallback = DelegateSupport.ConvertDelegate<AudioClip.PCMReaderCallback>(this.OnAudioRead);
            var posCallback = DelegateSupport.ConvertDelegate<AudioClip.PCMSetPositionCallback>(this.OnAudioSetPosition);

            audioClip = AudioClip.Create(Path, (int)waveStream.Length / (channels * 2), channels, sampleRate, true, readerCallback, posCallback);
            return audioClip;
        }

        void OnAudioRead(Il2CppStructArray<float> data)
        {
            int samplesToRead = data.Length;

            // Create a byte buffer for PCM data
            byte[] byteBuffer = new byte[samplesToRead * 2]; // 2 bytes per sample for 16-bit PCM

            // Read PCM data from the MP3 file
            int bytesRead = waveStream.Read(byteBuffer, 0, byteBuffer.Length);

            if (bytesRead > 0)
            {
                // Convert byte data into float data (PCM 16-bit to float)
                for (int i = 0; i < bytesRead / 2; i++)
                {
                    short sample = BitConverter.ToInt16(byteBuffer, i * 2);
                    data[i] = sample / 32768f; // Normalize to range -1.0 to 1.0
                }

                // If less data was read than requested, fill the rest with zeros
                if (bytesRead < byteBuffer.Length)
                {
                    Melon<Core>.Logger.Warning($"END REACHED, {bytesRead} < {byteBuffer.Length}");
                    for (int i = bytesRead / 2; i < data.Length; i++)
                    {
                        data[i] = 0f;
                    }
                }
            }
            else
            {
                // If no more data is available, stop the audio
                //audioSource.Stop();
            }
        }
        void OnAudioSetPosition(int newPosition)
        {

            Melon<Core>.Logger.Msg("setPos " + newPosition);
            waveStream.Position = newPosition * channels * 2; // 2 bytes per sample (16-bit PCM)
        }

    }

    public class Core : MelonMod
    {
        public static string customRadioNameStringId = "STR_CUSTOM_RADIO_NAME";
        public static string customRadioFreqStringId = "STR_CUSTOM_RADIO_FREQ";

        public static int minTracks = 10;
        public static int warningSeconds = 20;

        private List<TrackInfo> trackInfos = new List<TrackInfo>();

        public MelonPreferences_Entry<string> RadioTitle;
        public MelonPreferences_Entry<string> RadioFreq;
        public MelonPreferences_Entry<string> MusicDir;

        public override void OnInitializeMelon()
        {
            try
            {
                LoadSettings();
            }
            catch (Exception e)
            {
                LoggerInstance.Error("Unable to initialize options.");
                LoggerInstance.BigError(e.ToString());
                return;
            }

            try
            {
                LoadTracks();
            }
            catch (Exception e)
            {
                LoggerInstance.Error("Something went wrong while loading the tracks.");
                LoggerInstance.BigError(e.ToString());
                return;
            }

            // show warning if needed
            if (trackInfos.Count < minTracks)
            {
                MelonEvents.OnGUI.Subscribe(DrawWarning, 100);
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

        private void DrawWarning()
        {
            if (Time.realtimeSinceStartup < warningSeconds)
            {
                GUIStyle style = new GUIStyle();
                style.alignment = TextAnchor.UpperLeft;
                style.normal.textColor = Color.yellow;
                style.fontStyle = FontStyle.Bold;
                GUI.Box(new Rect(20, 50, 600, 70), $"CustomRadio mod: less than {minTracks} tracks were loaded, please add more tracks to your music folder, the game will start to act weird if you don't.\nIt can be located at: {MusicDir.Value}\nThis message will hide after {warningSeconds} secs.", style);
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