using System.Windows.Media;
using Serilog;
using Color = System.Drawing.Color;
using Brush = System.Windows.Media.Brush;

namespace WinHUD.Services
{
    public class BackgroundAnalyzer
    {
        // Settings
        private const int LuminanceThreshold = 140; // 0-255 (Higher = strict dark mode)

        // We reuse the bitmap to reduce memory churn
        private readonly Bitmap _sampleBitmap;
        private readonly Graphics _graphics;

        // Define your High-Contrast Colors
        private readonly Brush _lightModeColor = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)); // Almost Black
        private readonly Brush _darkModeColor = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 0));   // Bright Green

        public BackgroundAnalyzer()
        {
            // We only need a 1x1 pixel sample to get the "Average" effectively
            // But sampling a 50x50 area gives a better average of the region
            _sampleBitmap = new Bitmap(50, 50);
            _graphics = Graphics.FromImage(_sampleBitmap);
            Log.Information("[Contrast] BackgroundAnalyzer initialized.");
        }

        public Brush GetOptimalTextColor(int x, int y)
        {
            try
            {
                // 1. Capture the screen area where the HUD is
                // We assume the HUD is transparent, so this captures the game/wallpaper behind it.
                _graphics.CopyFromScreen(x, y, 0, 0, _sampleBitmap.Size, CopyPixelOperation.SourceCopy);

                // 2. Calculate Average Luminance
                float totalLum = 0;
                int samples = 0;

                // Optimization: Step by 5 pixels to save CPU
                for (int i = 0; i < _sampleBitmap.Width; i += 2)
                {
                    for (int j = 0; j < _sampleBitmap.Height; j += 2)
                    {
                        Color pixel = _sampleBitmap.GetPixel(i, j);

                        // Standard Luminance Formula: 0.2126R + 0.7152G + 0.0722B
                        totalLum += (0.2126f * pixel.R + 0.7152f * pixel.G + 0.0722f * pixel.B);
                        samples++;
                    }
                }

                float averageLum = totalLum / samples;

                // 3. Decision Logic
                // If background is bright (> Threshold) -> Use Dark Text
                // If background is dark (< Threshold)   -> Use Light Text
                return (averageLum > LuminanceThreshold) ? _lightModeColor : _darkModeColor;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Contrast] Screen capture or analysis failed: {Message}", ex.Message);
                // Fallback if screen capture fails (e.g., locked screen)
                return _darkModeColor;
            }
        }
    }
}
