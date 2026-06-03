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

        private void OnAudioPreviewRequested(object? sender, (string FileName, Stream Stream) args)
        {
            try
            {
                var preview = new AudioPreviewWindow(args.Stream)
                {
                    Title = args.FileName,
                };
                preview.Show(this);
            }
            catch (Exception ex)
            {
                this.viewModel.StatusMessage = $"Failed to play audio: {ex.Message}";
            }
        }

        private void OnImagePreviewRequested(object? sender, (string FileName, Bitmap Bitmap) args)
        {
            var preview = new ImagePreviewWindow(args.Bitmap)
            {
                Title = args.FileName,
            };
            preview.Show(this);
        }
    }
}
