using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using NAudio.Wave;
using System.Reflection.PortableExecutable;
using TagLib;
using TagLib.Riff;
using UnityEngine;

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

            audioClip = AudioClip.Create("streamy", (int)waveStream.Length / 2, channels, sampleRate, true, readerCallback, posCallback);
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

        // Callback method: Unity adjusts the playback position (e.g., seeking)
        void OnAudioSetPosition(int newPosition)
        {

            Melon<Core>.Logger.Msg("setPos " + newPosition);
            waveStream.Position = newPosition * channels * 2; // 2 bytes per sample (16-bit PCM)
        }

    }

    //public class StreamedAudioClip
    //{
    //    private int sampleRate = 44100;
    //    private int channels = 2; // Stereo
    //    private Mp3FileReader mp3Reader;
    //    private WaveStream waveStream;

    //    private AudioClip audioClip;
    //    public AudioClip AudioClip { get { return audioClip; } }

    //    public StreamedAudioClip(string path)
    //    {
    //        //// TODO: support non-mp3 formats
    //        //mp3Reader = new Mp3FileReader(path);
    //        //waveStream = WaveFormatConversionStream.CreatePcmStream(mp3Reader);

    //        //audioClip = AudioClip.Create("streamy", sampleRate, sampleRate * channels, channels, true, (AudioClip.PCMReaderCallback)OnAudioRead, (AudioClip.PCMSetPositionCallback)OnAudioSetPosition);


    //        // Use NAudio to load the MP3 and convert it to WAV
    //        mp3Reader = new Mp3FileReader(path);
    //        waveStream = WaveFormatConversionStream.CreatePcmStream(mp3Reader);
    //        byte[] buffer = new byte[waveStream.Length];
    //        waveStream.Read(buffer, 0, buffer.Length);

    //        // Create an AudioClip from WAV data
    //        audioClip = AudioClip.Create("LoadedMP3", buffer.Length / 2, 2, 44100, false);
    //        audioClip.SetData(ConvertByteToFloatArray(buffer), 0);
    //    }

    //    private float[] ConvertByteToFloatArray(byte[] input)
    //    {
    //        // Convert WAV byte data to float array
    //        int len = input.Length / 2;
    //        float[] result = new float[len];
    //        for (int i = 0; i < len; i++)
    //        {
    //            result[i] = (short)(input[i * 2] | input[i * 2 + 1] << 8) / 32768f;
    //        }
    //        return result;
    //    }

    //    // Callback method: Unity requests more audio data for the streaming clip
        
    //}

    public class Core : MelonMod
    {
        public static string customRadioNameStringId = "STR_CUSTOM_RADIO_NAME";
        public static string customRadioFreqStringId = "STR_CUSTOM_RADIO_FREQ";
        private List<TrackInfo> trackInfos = new List<TrackInfo>();

        public override void OnInitializeMelon()
        {
            try
            {
                LoggerInstance.Msg("Loading tracks...");
                LoggerInstance.WriteSpacer();
                foreach (string path in Directory.EnumerateFiles("C:\\Users\\jari2\\Documents\\radio"))
                {
                    trackInfos.Add(new TrackInfo(path));
                }
            }catch (Exception e)
            {
                LoggerInstance.Error("Unable to initialize custom radio mod.");
                LoggerInstance.BigError(e.ToString());
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