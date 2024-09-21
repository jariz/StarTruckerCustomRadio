using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppInterop.Runtime;
using MelonLoader;
using NAudio.Wave;
using UnityEngine;
using TagLib;

namespace StarTruckerCustomRadio
{
    public class TrackInfo
    {
        public Tag Tag;
        public string Path;

        private int sampleRate = 44100;
        private int channels = 2; // Stereo

        private WaveStream waveStream;

        public TrackInfo(string path)
        {
            var tagFile = TagLib.File.Create(path);
            Tag = tagFile.Tag;
            this.Path = path;
        }

        public AudioClip CreateAudioClip()
        {
            switch (System.IO.Path.GetExtension(Path))
            {
                case "mp3":
                    // normal audiofile reader can apparently also read mp3's but seems to crash more often in my tests?
                    waveStream = WaveFormatConversionStream.CreatePcmStream(new Mp3FileReader(Path));
                    break;
                default:
                    waveStream = WaveFormatConversionStream.CreatePcmStream(new AudioFileReader(Path));
                    break;
            }

            var readerCallback = DelegateSupport.ConvertDelegate<AudioClip.PCMReaderCallback>(this.OnAudioRead);
            var posCallback = DelegateSupport.ConvertDelegate<AudioClip.PCMSetPositionCallback>(this.OnAudioSetPosition);

            return AudioClip.Create(Path, (int)waveStream.Length / (channels * 2), channels, sampleRate, true, readerCallback, posCallback);
        }

        void OnAudioRead(Il2CppStructArray<float> data)
        {
            if (waveStream == null)
            {
                Melon<Core>.Logger.Warning($"Attempted to read from closed wave stream!");
            }

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
                    Melon<Core>.Logger.Warning($"Game requested more bytes than available! {bytesRead} < {byteBuffer.Length}");
                    for (int i = bytesRead / 2; i < data.Length; i++)
                    {
                        data[i] = 0f;
                    }
                }
            }

            if (bytesRead == 0)
            {
                CloseWaveStream();
            }
        }
        void OnAudioSetPosition(int newPosition)
        {
            waveStream.Position = newPosition * channels * 2; // 2 bytes per sample (16-bit PCM)
        }

        private void CloseWaveStream()
        {
            if (waveStream != null)
            {
                waveStream.Close();
                waveStream.Dispose();
                waveStream = null;
                return;
            }
            Melon<Core>.Logger.Warning($"Attempted to close already closed wave stream!");
        }

    }
}
