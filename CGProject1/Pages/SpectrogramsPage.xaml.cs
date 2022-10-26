using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CGProject1.Chart;
using CGProject1.SignalProcessing;
using Xceed.Wpf.Toolkit;
using MessageBox = System.Windows.MessageBox;

namespace CGProject1.Pages
{
    public partial class SpectrogramsPage : IChannelComponent
    {
        private static readonly Settings Settings = Settings.GetInstance(nameof(SpectrogramsPage));
        
        private static readonly List<byte[][]> Palettes = new List<byte[][]>();

        private byte[][] curPalette = Palettes[1];

        private double spectrogramHeight = 150;

        private readonly HashSet<string> channelNames = new HashSet<string>();
        private readonly List<Spectrogram> spectrograms = new List<Spectrogram>();
        private readonly List<ChartLine> charts = new List<ChartLine>();

        private int begin;
        private int end;

        private bool settingsLoaded;

        private readonly IDftCalculator myDftCalculator;

        public SpectrogramsPage(IDftCalculator dftCalculator)
        {
            myDftCalculator = dftCalculator;

            begin = 0;
            end = 0;

            InitializeComponent();
            
            LoadSettings();

            RecalculateHeight(1);
        }

        static SpectrogramsPage()
        {
            var greyPalette = new byte[256][];
            for (var i = 0; i < 256; i++) greyPalette[i] = new byte[] {(byte) i, (byte) i, (byte) i};
            Palettes.Add(greyPalette);

            var hotPalette = new byte[256][];
            for (var i = 0; i <= 85; i++) hotPalette[i] = new byte[] {(byte) (i * 3), 0, 0};
            for (var i = 86; i <= 170; i++) hotPalette[i] = new byte[] {255, (byte) ((i - 85) * 3), 0};
            for (var i = 171; i <= 255; i++) hotPalette[i] = new byte[] {255, 255, (byte) ((i - 170) * 3)};
            Palettes.Add(hotPalette);

            var icePalette = new byte[256][];
            for (var i = 0; i < 128; i++) icePalette[i] = new byte[] {0, (byte) (i * 2), (byte) (i * 2)};
            for (var i = 128; i < 256; i++) icePalette[i] = new byte[] {(byte) ((i - 128) * 2), 255, 255};
            Palettes.Add(icePalette);

            var blueRedYellow = new byte[256][];
            for (var i = 0; i < 128; i++) blueRedYellow[i] = new byte[] {(byte) (i * 2), 0, (byte) (255 - i * 2)};
            for (var i = 128; i < 256; i++) blueRedYellow[i] = new byte[] {255, (byte) ((i - 128) * 2), 0};
            Palettes.Add(blueRedYellow);
        }
        
        private void LoadSettings()
        {
            PaletteComboBox.SelectedIndex = Settings.GetOrDefault("paletteSelectedIndex", PaletteComboBox.SelectedIndex);
            
            CoeffSlider.Minimum = Settings.GetOrDefault("coeffMinimum", CoeffSlider.Minimum);
            CoeffSlider.Maximum = Settings.GetOrDefault("coeffMaximum", CoeffSlider.Maximum);
            CoeffSlider.Value = Settings.GetOrDefault("coeffValue", CoeffSlider.Value);
            
            CountPerPage.Minimum = Settings.GetOrDefault("countPerPageMinimum", CountPerPage.Minimum);
            CountPerPage.Maximum = Settings.GetOrDefault("countPerPageMaximum", CountPerPage.Maximum);
            CountPerPage.Value = Settings.GetOrDefault("countPerPageValue", CountPerPage.Value);
            
            BrightnessSlider.Minimum = Settings.GetOrDefault("brightnessMinimum", BrightnessSlider.Minimum);
            BrightnessSlider.Maximum = Settings.GetOrDefault("brightnessMaximum", BrightnessSlider.Maximum);
            BrightnessSlider.Value = Settings.GetOrDefault("brightnessValue", BrightnessSlider.Value);

            settingsLoaded = true;
        }

        public void Reset(Signal signal)
        {
            channelNames.Clear();
            spectrograms.Clear();
            charts.Clear();

            Spectrograms.Children.Clear();
        }

        public void UpdateActiveSegment(int begin, int end)
        {
            this.begin = begin;
            this.end = end;

            BeginLabel.Content = $"Начало: {begin}";
            EndLabel.Content = $"Конец: {end}";

            foreach (var sp in spectrograms)
            {
                sp.Begin = begin;
                sp.End = end;
            }

            foreach (var chart in charts)
            {
                chart.Segment.SetLeftRight(begin, end);
            }
        }

        public void AddChannel(Channel channel)
        {
            if (channelNames.Contains(channel.Name))
            {
                return;
            }

            channelNames.Add(channel.Name);

            var border = new Border();
            border.BorderThickness = new Thickness(1);
            border.BorderBrush = Brushes.Black;

            var channelPanel = new StackPanel();
            border.Child = channelPanel;

            var sp = new Spectrogram(myDftCalculator, channel)
            {
                Begin = begin,
                End = end,
                Palette = curPalette,
                CoeffN = CoeffSlider.Value,
                BoostCoeff = BrightnessSlider.Value,
                SpectrogramHeight = this.spectrogramHeight,
                ShowCurrentXY = true,
                ContextMenu = new ContextMenu()
            };

            channelPanel.Children.Add(sp);

            spectrograms.Add(sp);

            var bottomGrid = new Grid();
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition());
            var cd1 = new ColumnDefinition();
            cd1.Width = new GridLength(sp.RightOffset);
            bottomGrid.ColumnDefinitions.Add(cd1);

            var chart = new ChartLine(channel);
            chart.Height = 45;
            chart.Margin = new Thickness(sp.LeftOffset, 2, 2, 2);
            chart.Segment.SetLeftRight(this.begin, this.end);
            chart.DisplayTitle = false;
            chart.GridDraw = true;
            chart.DisplayVAxisInfo = false;
            chart.DisplayHAxisInfo = true;
            chart.HAxisAlligment = ChartLine.HAxisAlligmentEnum.Bottom;
            chart.MappingXAxis = (idx, chartLine) =>
            {
                var t = chartLine.Channel.StartDateTime + TimeSpan.FromSeconds(chartLine.Channel.DeltaTime * idx);
                return t.ToString("dd-MM-yyyy \n HH\\:mm\\:ss");
            };
            charts.Add(chart);

            var item = new MenuItem();
            item.Header = "Закрыть канал";
            item.Click += (sender, args) =>
            {
                channelNames.Remove(channel.Name);
                spectrograms.Remove(sp);
                charts.Remove(chart);
                Spectrograms.Children.Remove(border);
            };

            sp.ContextMenu.Items.Add(item);

            bottomGrid.Children.Add(chart);

            channelPanel.Children.Add(bottomGrid);

            Spectrograms.Children.Add(border);
        }

        private void UpdateSpectrograms(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(BrightnessField.Text, NumberStyles.Any, CultureInfo.InvariantCulture,
                    out double newBrightness)
                || !double.TryParse(CoeffSelector.Text, NumberStyles.Any, CultureInfo.InvariantCulture,
                    out double newCoeff))
            {
                MessageBox.Show("Некорректные параметры", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (newCoeff < 1)
            {
                newCoeff = 1;
            }

            if (newCoeff > 10)
            {
                newCoeff = 10;
            }

            BrightnessSlider.Value = newBrightness;

            BrightnessField.Text = newBrightness.ToString(CultureInfo.InvariantCulture);
            CoeffSelector.Text = newCoeff.ToString(CultureInfo.InvariantCulture);

            this.CoeffSlider.Value = newCoeff;

            foreach (var sp in spectrograms)
            {
                sp.BoostCoeff = BrightnessSlider.Value;
                sp.CoeffN = CoeffSlider.Value;
                sp.SpectrogramHeight = spectrogramHeight;
            }
        }

        private void PaletteComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PaletteComboBox.SelectedIndex < 0 || PaletteComboBox.SelectedIndex >= Palettes.Count) return;

            if (settingsLoaded)
            {
                Settings.Set("paletteSelectedIndex", PaletteComboBox.SelectedIndex);
            }

            curPalette = Palettes[PaletteComboBox.SelectedIndex];
                
            foreach (var sp in spectrograms)
            {
                sp.Palette = curPalette;
            }
        }

        private void PreviewTextInputHandle(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !TextIsNumeric(e.Text);
        }

        private void PreviewPastingHandle(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string input = (string) e.DataObject.GetData(typeof(string));
                if (!TextIsNumeric(input)) e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        private bool TextIsNumeric(string input)
        {
            return input.All(c => char.IsDigit(c) || char.IsControl(c) || c == '.');
        }

        private void CoeffSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (settingsLoaded && sender is Slider coeffSlider)
            {
                Settings.Set("coeffMinimum", coeffSlider.Minimum);
                Settings.Set("coeffValue", e.NewValue);
                Settings.Set("coeffMaximum", coeffSlider.Maximum);
            }

            CoeffSelector.Text = e.NewValue.ToString(CultureInfo.InvariantCulture);

            foreach (var sp in spectrograms)
            {
                sp.CoeffN = e.NewValue;
            }
        }

        private void RecalculateHeight(int count)
        {
            if (this.ActualHeight <= 0)
            {
                return;
            }

            double newHeight = (this.ActualHeight) / count;
            this.spectrogramHeight = newHeight;

            foreach (var spectrogram in spectrograms)
            {
                spectrogram.SpectrogramHeight = this.spectrogramHeight;
            }
        }

        private void CountPerPage_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (settingsLoaded && sender is IntegerUpDown countPerPage)
            {
                Settings.Set("countPerPageMinimum", countPerPage.Minimum);
                Settings.Set("countPerPageValue", e.NewValue);
                Settings.Set("countPerPageMaximum", countPerPage.Maximum);
            }

            RecalculateHeight((int) e.NewValue);
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (CountPerPage.Value.HasValue)
            {
                RecalculateHeight((int) CountPerPage.Value);
            }
        }

        private void BrightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (settingsLoaded && sender is Slider brightnessSlider)
            {
                Settings.Set("brightnessMinimum", brightnessSlider.Minimum);
                Settings.Set("brightnessValue", e.NewValue);
                Settings.Set("brightnessMaximum", brightnessSlider.Maximum);
            }

            BrightnessField.Text = e.NewValue.ToString(CultureInfo.InvariantCulture);

            foreach (var sp in spectrograms)
            {
                sp.BoostCoeff = e.NewValue;
            }
        }
    }
}
