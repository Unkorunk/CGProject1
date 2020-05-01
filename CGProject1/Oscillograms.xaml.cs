using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CGProject1 {
    /// <summary>
    /// Interaction logic for Oscillograms.xaml
    /// </summary>
    public partial class Oscillograms : Window {
        private HashSet<Chart> activeCharts;

        private int samplesCount = 0;

        public Oscillograms() {
            InitializeComponent();
            activeCharts = new HashSet<Chart>();
        }

        public void Update(Signal signal) {
            BeginSlider.IsEnabled = signal != null;
            EndSlider.IsEnabled = signal != null;
            BeginBox.IsEnabled = signal != null;
            EndBox.IsEnabled = signal != null;

            if (signal != null) {
                samplesCount = signal.SamplesCount;
                OscillogramsField.Children.Clear();
                activeCharts.Clear();

                BeginSlider.Maximum = samplesCount;
                EndSlider.Maximum = samplesCount;

                BeginSlider.Value = 0;
                EndSlider.Value = samplesCount;

                BeginBox.Text = 0.ToString();
                EndBox.Text = samplesCount.ToString();
            }
            
        }

        public void AddChannel(Channel channel) {
            var newChart = new Chart(channel);

            newChart.Begin = (int)BeginSlider.Value;
            newChart.End = (int)EndSlider.Value;

            newChart.Height = 100;

            OscillogramsField.Children.Add(newChart);
            activeCharts.Add(newChart);

            newChart.ContextMenu = new ContextMenu();
            var item1 = new MenuItem();
            item1.Header = "Закрыть канал";
            item1.Click += (object sender, RoutedEventArgs args) => {
                OscillogramsField.Children.Remove(newChart);
                activeCharts.Remove(newChart);
            };

            newChart.ContextMenu.Items.Add(item1);
        }

        private void BeginSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            InputBeginEnd((int)BeginSlider.Value, (int)e.NewValue);
        }

        private void EndSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            InputBeginEnd((int)BeginSlider.Value, (int)e.NewValue);
        }

        private void textBox_ValueChanged(object sender, EventArgs e) {
            long begin;
            long end;

            if (!Int64.TryParse(BeginBox.Text, out begin)) {
                begin = 0;
            }

            if (!Int64.TryParse(EndBox.Text, out end)) {
                end = samplesCount;
            }

            InputBeginEnd(begin, end);
        }

        private void InputBeginEnd(long begin, long end) {
            if (begin > samplesCount) {
                begin = samplesCount;
            }

            if (begin < 0) {
                begin = 0;
            }

            if (end > samplesCount) {
                end = samplesCount;
            }

            if (end < begin) {
                end = begin;
            }

            foreach (var chart in activeCharts) {
                chart.Begin = (int)begin;
                chart.End = (int)end;
            }

            BeginSlider.Value = begin;
            EndSlider.Value = end;
            BeginBox.Text = begin.ToString();
            EndBox.Text = end.ToString();
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
            return input.All(c => Char.IsDigit(c) || Char.IsControl(c));
        }
    }
}
