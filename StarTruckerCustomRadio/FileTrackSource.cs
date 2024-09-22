using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppInterop.Runtime;
using MelonLoader;
using UnityEngine;
using TagLib;
using CSCore;
using CSCore.Codecs;
using UnityEngine.Networking;

namespace StarTruckerCustomRadio
{
    public class SourceMetadata
    {
        public string Artist;
        public string Title;
    }

    public abstract class TrackSource
    {
        public abstract IWaveSource GetWaveSource();
        public abstract SourceMetadata GetMetadata();

        public abstract string GetName();

        private int sampleRate = 44100;
        private int channels = 2; // Stereo

        private IWaveSource waveSource;

        public AudioClip CreateAudioClip()
        {
            try
            {
                waveSource = GetWaveSource();

                var readerCallback = DelegateSupport.ConvertDelegate<AudioClip.PCMReaderCallback>(this.OnAudioRead);
                var posCallback = DelegateSupport.ConvertDelegate<AudioClip.PCMSetPositionCallback>(this.OnAudioSetPosition);

                return AudioClip.Create(GetName(), (int)waveSource.Length / (channels * 2), channels, sampleRate, true, readerCallback, posCallback);
            }

            catch (Exception innerEx)
            {
                Melon<Core>.Logger.BigError(new Exception($"Error while attempting to decode '{GetName()}', skipping it!", innerEx).ToString());

                // Create empty clip so the show can go on.
                return AudioClip.Create(GetName(), 1, channels, sampleRate, false);
            }
        }

        void OnAudioRead(Il2CppStructArray<float> data)
        {
            if (waveSource == null)
            {
                Melon<Core>.Logger.Warning($"Attempted to read from closed wave stream! ('{GetName()}')");
            }

            int samplesToRead = data.Length;

            // Create a byte buffer for PCM data
            byte[] byteBuffer = new byte[samplesToRead * 2]; // 2 bytes per sample for 16-bit PCM

            // Read PCM data from the MP3 file
            int bytesRead = waveSource.Read(byteBuffer, 0, byteBuffer.Length);

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
                    Melon<Core>.Logger.Warning($"Game requested more bytes than available! {bytesRead} < {byteBuffer.Length} ({GetName()})");
                    for (int i = bytesRead / 2; i < data.Length; i++)
                    {
                        data[i] = 0f;
                    }
                }
            }
        }
        void OnAudioSetPosition(int newPosition)
        {
            waveSource.Position = newPosition * channels * 2; // 2 bytes per sample (16-bit PCM)
        }
    }

    public class FileTrackSource : TrackSource
    {
        public Tag Tag;
        public string Path;

        public FileTrackSource(string path)
        {
            var tagFile = TagLib.File.Create(path);
            Tag = tagFile.Tag;
            this.Path = path;
        }

        public override SourceMetadata GetMetadata() => new SourceMetadata
        {
            Artist = Tag.IsEmpty ? "Unknown" : Tag.FirstPerformer,
            Title = Tag.IsEmpty ? System.IO.Path.GetFileName(Path) : Tag.Title
        };

        public override string GetName()
        {
            return Path;
        }

        public override IWaveSource GetWaveSource() => CodecFactory.Instance.GetCodec(Path)
                 .ToSampleSource()
                 .ToWaveSource(16); // Converts the audio to 16-bit PCM

        
    }
    public class UriTrackSource : TrackSource
    {
        public Uri Uri;

        public UriTrackSource(string uri)
        {
            Uri = new Uri(uri);
        }

        public override SourceMetadata GetMetadata() => new SourceMetadata
        {
            Artist = "Unknown",
            Title = Uri.AbsolutePath
        };

        public override string GetName()
        {
            return Uri.AbsolutePath;
        }

        public override IWaveSource GetWaveSource() => CodecFactory.Instance.GetCodec(Uri)
                 .ToSampleSource()
                 .ToWaveSource(16); // Converts the audio to 16-bit PCM
    }
}
