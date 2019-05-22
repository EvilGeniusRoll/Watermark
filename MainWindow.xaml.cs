using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Watermark
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                tbPath.Text = openFileDialog.FileName;

                Watermark();
            }
        }

        private void Watermark()
        {
            if (string.IsNullOrEmpty(tbPath.Text))
            {
                return;
            }
            BitmapImage mainImage = new BitmapImage(new Uri(tbPath.Text));

            BitmapImage watermarkImage = new BitmapImage();
            watermarkImage.BeginInit();
            watermarkImage.UriSource = new Uri(@"pack://application:,,,/test.png");
            watermarkImage.DecodePixelWidth = (int)(50 + 20 * SliderS.Value);
            watermarkImage.DecodePixelHeight = (int)(50 + 20 * SliderS.Value);
            watermarkImage.EndInit();

            var result = StitchBitmaps(mainImage, watermarkImage);
            Image.Source = result;
        }

        private void SliderY_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Watermark();
        }

        private void SliderX_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Watermark();
        }

        private void SliderO_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Watermark();
        }

        private void SliderS_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Watermark();
        }

        private BitmapSource StitchBitmaps(BitmapSource b1, BitmapSource b2)
        {
            var width = b1.PixelWidth;
            var height = b1.PixelHeight;
            var wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
            var stride1 = (b1.PixelWidth * b1.Format.BitsPerPixel + 7) / 8;
            var stride2 = (b2.PixelWidth * b2.Format.BitsPerPixel + 7) / 8;
            var size = b1.PixelHeight * stride1;
            size = Math.Max(size, b2.PixelHeight * stride2);

            var buffer = new byte[size];
            b1.CopyPixels(buffer, stride1, 0);
            wb.WritePixels(
                new Int32Rect(0, 0, b1.PixelWidth, b1.PixelHeight),
                buffer, stride1, 0);

            Chubs_BitBltMerge(ref wb, (int)((width - b2.PixelWidth) / 100 * SliderX.Value), (int)((height - b2.PixelHeight) / 100 * SliderY.Value), ref b2);

            return wb;
        }

        private void Chubs_BitBltMerge(ref WriteableBitmap dest, int nXDest, int nYDest, ref BitmapSource src)
        {
            // copy the source image into a byte buffer
            int src_stride = src.PixelWidth * (src.Format.BitsPerPixel >> 3);
            byte[] src_buffer = new byte[src_stride * src.PixelHeight];
            src.CopyPixels(src_buffer, src_stride, 0);

            // copy the dest image into a byte buffer
            int dest_stride = src.PixelWidth * (dest.Format.BitsPerPixel >> 3);
            byte[] dest_buffer = new byte[(src.PixelWidth * src.PixelHeight) << 2];
            dest.CopyPixels(new Int32Rect(nXDest, nYDest, src.PixelWidth, src.PixelHeight), dest_buffer, dest_stride, 0);

            // do merge (could be made faster through parallelization)
            for (int i = 0; i < src_buffer.Length; i = i + 4)
            {
                float src_alpha = ((float)src_buffer[i + 3] / 255);
                src_alpha = src_alpha * (float)SliderO.Value;
                dest_buffer[i + 0] = (byte)((src_buffer[i + 0] * src_alpha) + dest_buffer[i + 0] * (1.0 - src_alpha));
                dest_buffer[i + 1] = (byte)((src_buffer[i + 1] * src_alpha) + dest_buffer[i + 1] * (1.0 - src_alpha));
                dest_buffer[i + 2] = (byte)((src_buffer[i + 2] * src_alpha) + dest_buffer[i + 2] * (1.0 - src_alpha));
            }

            // copy dest buffer back to the dest WriteableBitmap
            dest.WritePixels(new Int32Rect(nXDest, nYDest, src.PixelWidth, src.PixelHeight), dest_buffer, dest_stride, 0);
        }


        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (Image.Source == null)
            {
                return;
            }
            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Image files (*.png) | *.png"
            };
            if (sfd.ShowDialog() == true)
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                BitmapFrame outputFrame = BitmapFrame.Create((BitmapSource)Image.Source);
                encoder.Frames.Add(outputFrame);

                using (FileStream file = File.OpenWrite(sfd.FileName))
                {
                    encoder.Save(file);
                }
            }

        }


    }
}

