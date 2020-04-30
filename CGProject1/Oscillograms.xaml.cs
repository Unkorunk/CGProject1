using System;
using System.Collections.Generic;
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
            samplesCount = signal.SamplesCount;
            OscillogramsField.Children.Clear();
            activeCharts.Clear();
        }

        public void AddChannel(Channel channel) {
            var newChart = new Chart(channel);

            newChart.Begin = 0;
            newChart.End = samplesCount;

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
    }
}
