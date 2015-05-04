﻿using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.Activation;

namespace ImageResize
{
    public sealed partial class MainPage : Page, IFileOpenPickerContinuable, IFileSavePickerContinuable
    {
        private CanvasBitmap bitmapImg;
        private Rect bitmapRect;
        private const double baseImageSize = 300;
        private double maxImageSize = 300;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void pickImgBtn_Click(object sender, RoutedEventArgs e)
        {
            //select file
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.PickSingleFileAndContinue();
        }

        public async void ContinueFileOpenPicker(FileOpenPickerContinuationEventArgs args)
        {
            if (args.Files.Count > 0)
            {
                await loadBitmapImage(args.Files[0]);
            }
        }

        private async Task loadBitmapImage(StorageFile sourceFile)
        {
            using (var sourceStream = await sourceFile.OpenAsync(FileAccessMode.Read))
            {
                //resize if needed
                if (useLogicChk.IsChecked.HasValue && !useLogicChk.IsChecked.Value)
                {
                    var scale = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
                    if (scale > 1)
                    {
                        maxImageSize = baseImageSize / scale;
                        canvas.Height = maxImageSize;
                        canvas.Width = maxImageSize;
                    }
                }

                //load image
                bitmapImg = await CanvasBitmap.LoadAsync(canvas, sourceStream);
                bitmapRect = new Rect();

                //determine width and height
                var width = bitmapImg.Size.Width;
                var height = bitmapImg.Size.Height;
                if (width > height)
                {
                    bitmapRect.Width = Math.Round(width * maxImageSize / height);
                    bitmapRect.Height = maxImageSize;
                    bitmapRect.X = -((bitmapRect.Width - bitmapRect.Height) / 2);
                    bitmapRect.Y = 0;
                }
                else if (height > width)
                {
                    bitmapRect.Width = maxImageSize;
                    bitmapRect.Height = Math.Round(height * maxImageSize / width);
                    bitmapRect.X = 0;
                    bitmapRect.Y = -((bitmapRect.Height - bitmapRect.Width) / 2);
                }
                else
                {
                    bitmapRect.Width = maxImageSize;
                    bitmapRect.Height = maxImageSize;
                    bitmapRect.X = 0;
                    bitmapRect.Y = 0;
                }

                //force canvas to redraw
                canvas.Invalidate();
            }
        }

        private void canvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            //only draw if image is loaded
            if (bitmapImg != null)
            {
                args.DrawingSession.DrawImage(bitmapImg, bitmapRect);
                sender.Invalidate();
            }
        }

        private void saveImgBtn_Click(object sender, RoutedEventArgs e)
        {
            // create file 
            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            savePicker.DefaultFileExtension = ".png";
            savePicker.SuggestedFileName = "resizedImage";
            savePicker.FileTypeChoices.Add("PNG", new string[] { ".png" });
            savePicker.PickSaveFileAndContinue();            
        }

        public async void ContinueFileSavePicker(FileSavePickerContinuationEventArgs args)
        {
            if(args.File != null)
            {
                // render canvas to bitmap
                var bitmap = new RenderTargetBitmap();
                await bitmap.RenderAsync(canvas);

                //save bitmap to file
                using (var stream = await args.File.OpenStreamForWriteAsync())
                {
                    var pixelBuffer = await bitmap.GetPixelsAsync();
                    var logicalDpi = DisplayInformation.GetForCurrentView().LogicalDpi;
                    var randomAccessStream = stream.AsRandomAccessStream();
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, randomAccessStream);
                    encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, (uint)bitmap.PixelWidth,
                        (uint)bitmap.PixelHeight, logicalDpi, logicalDpi, pixelBuffer.ToArray());
                    await encoder.FlushAsync();
                }
            }
        }
    }
}
