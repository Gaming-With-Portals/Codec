namespace Codec.Avalonia.Services
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using NAudio.Wave;

    public sealed class AudioPlayer : IDisposable
    {
        private WaveOutEvent? waveOut;
        private WaveStream? reader;

        public async Task PlayAsync(Stream stream, bool ownsStream = true)
        {
            this.Stop();

            await Task.Run(() =>
            {
                using var reader = new StreamMediaFoundationReader(stream);

                var ms = new MemoryStream();
                WaveFileWriter.WriteWavFileToStream(ms, reader);
                ms.Seek(0, SeekOrigin.Begin);

                if (ownsStream)
                {
                    stream.Dispose();
                }

                this.reader = new WaveFileReader(ms);
                this.waveOut = new WaveOutEvent();
                this.waveOut.Init(this.reader);
                this.waveOut.Play();
            }).ConfigureAwait(false);
        }

        public void Stop()
        {
            this.waveOut?.Stop();
            this.waveOut?.Dispose();
            this.reader?.Dispose();
            this.waveOut = null;
            this.reader = null;
        }

        public void Dispose() => this.Stop();
    }
}
