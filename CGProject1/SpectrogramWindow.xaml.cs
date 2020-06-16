using System;
using System.Windows;
using System.Threading;
using System.Windows.Media;
using System.Windows.Controls;
using System.Collections.Generic;
using CGProject1.Chart;
using System.Windows.Input;
using System.Linq;
using System.Globalization;

namespace CGProject1 {
    public partial class SpectrogramWindow : Window {
        private static List<byte[][]> palettes;

        private byte[][] curPalette = palettes[1];

        private double spectrogramHeight = 150;
        private double coeffN = 1.0;
        private double boostCoeff = 1.0;

        private HashSet<string> channelNames;
        private List<Spectrogram> spectrograms;
        private List<Channel> channels;
        //private List<TextBox> brightnessBoxes;


        public SpectrogramWindow() {
            channelNames = new HashSet<string>();
            spectrograms = new List<Spectrogram>();
            channels = new List<Channel>();
            //brightnessBoxes = new List<TextBox>();

            InitializeComponent();
        }

        static SpectrogramWindow()
        {
            palettes = new List<byte[][]>();

            var greyPalette = new byte[256][];
            for (int i = 0; i < 256; i++) {
                greyPalette[i] = new byte[3];
                for (int j = 0; j < 3; j++) {
                    greyPalette[i][j] = (byte)i;
                }
            }

            palettes.Add(greyPalette);

            var hotPalette = new byte[256][];

            for (int i = 0; i <= 85; i++) hotPalette[i] = new byte[3] { (byte)(i * 3), 0, 0 };
            for (int i = 86; i <= 170; i++) hotPalette[i] = new byte[3] { 255, (byte)((i - 85) * 3), 0 };
            for (int i = 171; i <= 255; i++) hotPalette[i] = new byte[3] { 255, 255, (byte)((i - 170) * 3) };
            palettes.Add(hotPalette);

            var icePalette = new byte[256][];

            for (int i = 0; i < 128; i++) {
                icePalette[i] = new byte[3] { 0, (byte)(i * 2), (byte)(i * 2) };
            }
            for (int i = 128; i < 256; i++) {
                icePalette[i] = new byte[3] { (byte)((i - 128) * 2), 255, 255 };
            }
            palettes.Add(icePalette);

            var blueRedYellow = new byte[256][];
            for (int i = 0; i < 128; i++) {
                blueRedYellow[i] = new byte[3] { (byte)(i * 2), 0, (byte)(255 - i * 2) };
            }
            for (int i = 128; i < 256; i++) {
                blueRedYellow[i] = new byte[3] { 255, (byte)((i - 128) * 2), 0 };
            }
            palettes.Add(blueRedYellow);
        }

        public void AddChannel(Channel channel) {
            if (channelNames.Contains(channel.Name)) {
                return;
            }

            channelNames.Add(channel.Name);
            channels.Add(channel);

            var border = new Border();
            border.BorderThickness = new Thickness(1);
            border.BorderBrush = Brushes.Black;

            var channelPanel = new StackPanel();
            border.Child = channelPanel;

            var sp = new Spectrogram(channel);
            sp.Palette = this.curPalette;
            sp.CoeffN = this.coeffN;
            sp.BoostCoeff = this.boostCoeff;
            sp.SpectrogramHeight = this.spectrogramHeight;
            channelPanel.Children.Add(sp);

            spectrograms.Add(sp);

            var bottomGrid = new Grid();
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition());
            var cd1 = new ColumnDefinition();
            cd1.Width = new GridLength(sp.RightOffset);
            bottomGrid.ColumnDefinitions.Add(cd1);

            var chart = new ChartLine(in channel);
            chart.Height = 100;
            chart.Margin = new Thickness(sp.LeftOffset, 2, 2, 2);
            chart.Begin = 0;
            chart.End = channel.SamplesCount;
            chart.DisplayTitle = false;
            chart.GridDraw = true;
            chart.DisplayVAxisInfo = false;
            chart.DisplayHAxisInfo = true;
            chart.HAxisAlligment = ChartLine.HAxisAlligmentEnum.Bottom;
            chart.MappingXAxis = (int idx, ChartLine chart) => {
                var t = chart.Channel.StartDateTime + TimeSpan.FromSeconds(chart.Channel.DeltaTime * idx);
                return t.ToString("dd-MM-yyyy \n HH\\:mm\\:ss");
            };

            bottomGrid.Children.Add(chart);

            //var brightnessPanel = new StackPanel();
            //Grid.SetColumn(brightnessPanel, 1);
            //bottomGrid.Children.Add(brightnessPanel);

            //var brightnessLabel = new Label();
            //brightnessLabel.Content = "Яркость: ";
            //brightnessLabel.VerticalAlignment = VerticalAlignment.Center;
            //brightnessPanel.Children.Add(brightnessLabel);

            //var brightnessBox = new TextBox();
            //brightnessBox.Text = this.boostCoeff.ToString(CultureInfo.InvariantCulture);
            //brightnessBox.VerticalAlignment = VerticalAlignment.Center;
            //brightnessPanel.Children.Add(brightnessBox);

            channelPanel.Children.Add(bottomGrid);

            Spectrograms.Children.Add(border);
        }


        private void UpdateSpectrograms(object sender, RoutedEventArgs e) {
            if (!double.TryParse(BrightnessField.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double newBrightness)
                    || !double.TryParse(HeightField.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double newHeight)
                    || !double.TryParse(CoeffSelector.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double newCoeff))
            {
                MessageBox.Show("Некорректные параметры", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (newCoeff < 1) {
                newCoeff = 1;
            }
            if (newCoeff > 10) {
                newCoeff = 10;
            }

            BrightnessField.Text = newBrightness.ToString(CultureInfo.InvariantCulture);
            HeightField.Text = newHeight.ToString(CultureInfo.InvariantCulture);
            CoeffSelector.Text = newCoeff.ToString(CultureInfo.InvariantCulture);

            this.boostCoeff = newBrightness;
            this.spectrogramHeight = newHeight;
            this.coeffN = newCoeff;

            this.CoeffSlider.Value = newCoeff;

            foreach (var sp in spectrograms) {
                sp.BoostCoeff = boostCoeff;
                sp.CoeffN = coeffN;
                sp.SpectrogramHeight = spectrogramHeight;
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) {
            //RedrawSpectrograms();
        }

        private void ComboBoxMode_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (PaletteSelector.SelectedIndex >= 0 && PaletteSelector.SelectedIndex < palettes.Count) {
                curPalette = palettes[PaletteSelector.SelectedIndex];
            }

            foreach (var sp in spectrograms) {
                sp.Palette = curPalette;
            }
        }

        private void previewTextInput(object sender, TextCompositionEventArgs e) {
            e.Handled = !TextIsNumeric(e.Text);
        }

        private void previewPasting(object sender, DataObjectPastingEventArgs e) {
            if (e.DataObject.GetDataPresent(typeof(string))) {
                string input = (string)e.DataObject.GetData(typeof(string));
                if (!TextIsNumeric(input)) e.CancelCommand();
            } else {
                e.CancelCommand();
            }
        }

        private bool TextIsNumeric(string input) {
            return input.All(c => char.IsDigit(c) || char.IsControl(c) || c == '.');
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            this.coeffN = e.NewValue;
            CoeffSelector.Text = this.coeffN.ToString(CultureInfo.InvariantCulture);
            
            foreach (var sp in spectrograms) {
                sp.CoeffN = coeffN;
            }
        }
    }
}
