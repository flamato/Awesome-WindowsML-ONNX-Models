﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using WindowsMLDemos.Common;
using WindowsMLDemos.Common.Helper;
// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace GoogleNetPlaces
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        GoogLeNetPlacesModelModel model;
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void ImagePickerControl_ImageReceived(object sender, WindowsMLDemos.Common.UI.ImageReceivedEventArgs e)
        {
            var softImg = e.PickedImage;
            await EvaluteImageAsync(VideoFrame.CreateWithSoftwareBitmap(softImg));
        }

        private async void ImagePickerControl_ImagePreviewReceived(object sender, WindowsMLDemos.Common.UI.ImagePreviewReceivedEventArgs e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, async () =>
            {
                await EvaluteImageAsync(e.PreviewImage);
            });
        }

        private async Task EvaluteImageAsync(VideoFrame videoFrame)
        {
            var startTime = DateTime.Now;
            if (model == null)
            {
                var modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Model/GoogLeNetPlaces.onnx"));
                if (modelFile != null)
                {
                    model = new GoogLeNetPlacesModelModel();
                    await MLHelper.CreateModelAsync(modelFile, model);
                }
            }
            var input = new GoogLeNetPlacesModelModelInput()
            {
                sceneImage = videoFrame
            };

            try
            {
                var res = await model.EvaluateAsync(input) as GoogLeNetPlacesModelModelOutput;
                if (res != null)
                {
                    var results = new List<LabelResult>();
                    foreach (var kv in res.sceneLabelProbs)
                    {
                        results.Add(new LabelResult
                        {
                            Label = kv.Key,
                            Result = (float)Math.Round(kv.Value * 100, 2)
                        });
                    }
                    results.Sort((p1, p2) =>
                    {
                        return p2.Result.CompareTo(p1.Result);
                    });
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
                     {
                         outputText.Text = res.sceneLabel.FirstOrDefault();
                         resultList.ItemsSource = results;
                         previewControl.EvalutionTime = (DateTime.Now - startTime).TotalSeconds.ToString();
                     });
                }
            }
            catch (Exception ex)
            {
                await AlertHelper.ShowMessageAsync(ex.ToString());
            }
        }
    }
}
