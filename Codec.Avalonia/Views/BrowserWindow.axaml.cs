namespace Codec.Avalonia.Views
{
    using System;
    using System.IO;
    using global::Avalonia.Controls;
    using global::Avalonia.Media.Imaging;
    using Codec.Avalonia.Services;
    using Codec.Avalonia.ViewModels;

    public partial class BrowserWindow : Window
    {
        private readonly AudioPlayer audio;
        private readonly BrowserViewModel viewModel;

        public BrowserWindow(BrowserViewModel viewModel, AudioPlayer audio)
        {
            this.InitializeComponent();
            viewModel.AudioPreviewRequested += this.OnAudioPreviewRequested;
            viewModel.ImagePreviewRequested += this.OnImagePreviewRequested;
            this.viewModel = viewModel;
            this.audio = audio;
            this.DataContext = viewModel;
        }

        private async void OnAudioPreviewRequested(object? sender, Stream mediaStream)
        {
            try
            {
                await this.audio.PlayAsync(mediaStream).ConfigureAwait(false);
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
