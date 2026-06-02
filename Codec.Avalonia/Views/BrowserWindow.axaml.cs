namespace Codec.Avalonia.Views
{
    using System;
    using System.IO;
    using global::Avalonia.Controls;
    using global::Avalonia.Media.Imaging;
    using Codec.Avalonia.ViewModels;
    using Codec.Services;

    public partial class BrowserWindow : Window
    {
        private readonly BrowserViewModel viewModel;
        private AudioPlayer audioPlayer;

        public BrowserWindow(BrowserViewModel viewModel)
        {
            this.InitializeComponent();
            viewModel.AudioPreviewRequested += this.OnAudioPreviewRequested;
            viewModel.ImagePreviewRequested += this.OnImagePreviewRequested;
            this.viewModel = viewModel;
            this.DataContext = viewModel;
        }

        private async void OnAudioPreviewRequested(object? sender, Stream mediaStream)
        {
            this.audioPlayer?.Dispose();
            try
            {
                using var audioPlayer = new AudioPlayer(mediaStream);
                this.audioPlayer = audioPlayer;
                await audioPlayer.PlayAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                this.viewModel.StatusMessage = $"Failed to play audio: {ex.Message}";
            }
        }

        private void OnImagePreviewRequested(object? sender, Bitmap bmp)
        {
            var preview = new ImagePreviewWindow(bmp);
            preview.Show(this);
        }
    }
}
