﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.System.Display;
using Windows.System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using WindowsMLDemos.Common.Helper;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace WindowsMLDemos.Common.UI
{
    public sealed partial class ImagePickerControl : UserControl
    {


        public string EvalutionTime
        {
            get { return (string)GetValue(EvalutionTimeProperty); }
            set
            {
                SetValue(EvalutionTimeProperty, value);
                evaluateTimeText.Text = value;
            }
        }

        // Using a DependencyProperty as the backing store for EvalutionTime.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EvalutionTimeProperty =
            DependencyProperty.Register("EvalutionTime", typeof(string), typeof(ImagePickerControl), new PropertyMetadata(null));



        public int ImageTargetWidth
        {
            get { return (int)GetValue(ImageTargetWidthProperty); }
            set { SetValue(ImageTargetWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ImageTargetWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImageTargetWidthProperty =
            DependencyProperty.Register("ImageTargetWidth", typeof(int), typeof(ImagePickerControl), new PropertyMetadata(299));




        public int ImageTargetHeight
        {
            get { return (int)GetValue(ImageTargetHeightProperty); }
            set { SetValue(ImageTargetHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ImageTargetHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImageTargetHeightProperty =
            DependencyProperty.Register("ImageTargetHeight", typeof(int), typeof(ImagePickerControl), new PropertyMetadata(299));



        public int PreviewInterval
        {
            get { return (int)GetValue(PreviewIntervalProperty); }
            set { SetValue(PreviewIntervalProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PreviewInterval.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PreviewIntervalProperty =
            DependencyProperty.Register("PreviewInterval", typeof(int), typeof(ImagePickerControl), new PropertyMetadata(3));





        MediaCapture mediaCapture;
        ThreadPoolTimer timer;
        private bool isPreviewing = false;
        DisplayRequest displayRequest = new DisplayRequest();

        public event EventHandler<ImageReceivedEventArgs> ImageReceived;
        public event EventHandler<ImagePreviewReceivedEventArgs> ImagePreviewReceived;

        public ImagePickerControl()
        {
            this.InitializeComponent();
        }
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (timer != null)
            {
                timer.Cancel();
            }
            if (mediaCapture != null)
            {
                if (isPreviewing)
                {
                    await mediaCapture.StopPreviewAsync();
                    isPreviewing = false;
                }
                mediaCapture.Dispose();
                mediaCapture = null;
            }
            PreviewControl.Visibility = Visibility.Collapsed;
            inputImage.Visibility = Visibility.Visible;
            var file = await ImageHelper.PickerImageAsync();
            if (file != null)
            {
                using (var fs = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                {
                    var img = new BitmapImage();
                    await img.SetSourceAsync(fs);
                    inputImage.Source = img;
                    using (var tempStream = fs.CloneStream())
                    {
                        var softImg = await ImageHelper.ResizeImageAsync(tempStream, ImageTargetWidth, ImageTargetHeight);

                        if (ImageReceived != null)
                        {
                            ImageReceived(this, new ImageReceivedEventArgs(softImg));
                        }
                    }
                }
            }
        }

        private async void captureBtn_Click(object sender, RoutedEventArgs e)
        {
            PreviewControl.Visibility = Visibility.Visible;
            inputImage.Visibility = Visibility.Collapsed;

            if (mediaCapture == null)
            {
                var targetWidth = ImageTargetWidth;
                var targetHeight = ImageTargetHeight;
                timer = ThreadPoolTimer.CreatePeriodicTimer(async (source) =>
                {
                    if (mediaCapture != null)
                    {
                        try
                        {
                            // Get information about the preview
                            var previewProperties = mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;
                            var previewFrame = await mediaCapture.GetPreviewFrameAsync();
                            if (previewFrame != null)
                            {
                                var resizedFrame = await ImageHelper.ResizeVideoFrameAsync(previewFrame, previewProperties, targetWidth, targetHeight);
                                if (ImagePreviewReceived != null)
                                {
                                    ImagePreviewReceived(this, new ImagePreviewReceivedEventArgs(resizedFrame));
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.ToString());
                        }

                    }
                }, TimeSpan.FromSeconds(PreviewInterval));
                await StartPreviewAsync();
            }
        }

        private async Task StartPreviewAsync()
        {
            try
            {

                mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync();

                displayRequest.RequestActive();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
            }
            catch (UnauthorizedAccessException)
            {
                // This will be thrown if the user denied access to the camera in privacy settings
                await AlertHelper.ShowMessageAsync("The app was denied access to the camera");
                return;
            }

            try
            {
                PreviewControl.Source = mediaCapture;
                await mediaCapture.StartPreviewAsync();
                isPreviewing = true;
            }
            catch (System.IO.FileLoadException)
            {
                mediaCapture.CaptureDeviceExclusiveControlStatusChanged += _mediaCapture_CaptureDeviceExclusiveControlStatusChanged;
            }

        }

        private void _mediaCapture_CaptureDeviceExclusiveControlStatusChanged(MediaCapture sender, MediaCaptureDeviceExclusiveControlStatusChangedEventArgs args)
        {
        }

    }
}
