using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CGProject1.SignalProcessing;
using Microsoft.Win32;

namespace CGProject1 {
    public partial class MainWindow : Window {
        public static MainWindow instance = null;

        private AboutSignal aboutSignalWindow;
        private Oscillograms oscillogramWindow;
        private ModelingWindow modelingWindow;
        private SaveWindow savingWindow;
        private AnalyzerWindow analyzerWindow;
        private SpectrogramWindow spectrogramWindow;

        public StatisticsWindow statisticsWindow;

        private bool showing = false;
        private bool isOscillogramShowing = false;
        private bool isModelingWindowShowing = false;
        private bool isSavingWindowShowing = false;
        private bool isAnalyzerShowing = false;
        private bool isSpectrogramsShowing = false;

        public bool isStatisticShowing = false;

        public Signal currentSignal;

        private List<Chart> charts = new List<Chart>();

        public MainWindow() {
            instance = this;
            InitializeComponent();
            this.Closed += (object sender, System.EventArgs e) => {
                if (aboutSignalWindow != null) {
                    aboutSignalWindow.Close();
                }

                if (modelingWindow != null) {
                    modelingWindow.Close();
                }

                if (isOscillogramShowing) {
                    oscillogramWindow.Close();
                }
                //if (oscillogramWindow != null && oscillogramWindow.isStatisticsWindowShowing)
                //{
                //    oscillogramWindow.statisticsWindow.Close();
                //}

                if (isSavingWindowShowing) {
                    savingWindow.Close();
                }

                if (isAnalyzerShowing) {
                    analyzerWindow.Close();
                }
                if (isStatisticShowing) {
                    statisticsWindow.Close();
                }
                if (isSpectrogramsShowing) {
                    spectrogramWindow.Close();
                }

                Serializer.SerializeModels(Modelling.defaultPath, new List<ChannelConstructor>[] { Modelling.discreteModels, Modelling.continiousModels, Modelling.randomModels });
            };
        }

        private void AboutClick(object sender, RoutedEventArgs e) {
            MessageBox.Show("КГ-СИСТПРО-1-КАЛИНИН\r\n" +
                "Работу выполнили:\r\n" +
                "Михалев Юрий\r\n" +
                "Калинин Владислав\r\n" +
                "29.02.2020",
                "О программе", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ModelingClick(object sender, RoutedEventArgs e) {
            if (!this.isModelingWindowShowing) {
                this.modelingWindow = new ModelingWindow(currentSignal);
                this.modelingWindow.Closed += (object sender, System.EventArgs e) => this.isModelingWindowShowing = false;
                this.modelingWindow.Show();
                this.isModelingWindowShowing = true;
            }
        }

        public void AddChannel(Channel channel) {
            channel.StartDateTime = this.currentSignal.StartDateTime;
            channel.SamplingFrq = this.currentSignal.SamplingFrq;

            this.currentSignal.channels.Add(channel);

            if (aboutSignalWindow != null) {
                aboutSignalWindow.Close();
            }

            if (modelingWindow != null) {
                modelingWindow.Close();
            }

            if (isSavingWindowShowing) {
                savingWindow.Close();
            }

            if (isAnalyzerShowing) {
                analyzerWindow.Close();
            }

            var chart = new Chart(channel);
            chart.Height = 100;

            charts.Add(chart);
            channels.Children.Add(chart);

            chart.ContextMenu = new ContextMenu();

            var item1 = new MenuItem();
            item1.Header = "Осциллограмма";
            int cur = this.currentSignal.channels.Count - 1;
            item1.Click += (object sender, RoutedEventArgs args) => {
                OpenOscillograms();

                oscillogramWindow.AddChannel(currentSignal.channels[cur]);
            };
            chart.ContextMenu.Items.Add(item1);

            var item2 = new MenuItem();
            item2.Header = "Статистики";
            item2.Click += (object sender, RoutedEventArgs args) => {
                if (!isStatisticShowing) {
                    statisticsWindow = new StatisticsWindow();
                    isStatisticShowing = true;
                    statisticsWindow.Closed += (object sender, EventArgs e) => isStatisticShowing = false;
                    statisticsWindow.Show();
                }

                statisticsWindow.Update(charts[cur], true);
            };
            chart.ContextMenu.Items.Add(item2);

            var item3 = new MenuItem();
            item3.Header = "Анализ";
            item3.Click += (object sender, RoutedEventArgs args) => {
                OpenAnalyzer();
                analyzerWindow.AddChannel(currentSignal.channels[cur]);
            };
            chart.ContextMenu.Items.Add(item3);

            var item4 = new MenuItem();
            item4.Header = "Спектрограмма";
            item4.Click += (object sender, RoutedEventArgs args) => {
                OpenSpectrograms();
                spectrogramWindow.AddChannel(currentSignal.channels[cur]);
            };
            chart.ContextMenu.Items.Add(item4);

            chart.Begin = 0;
            chart.End = currentSignal.SamplesCount;
            
        }

        public void ResetSignal(Signal newSignal) {
            if (aboutSignalWindow != null) {
                aboutSignalWindow.Close();
            }

            if (modelingWindow != null) {
                modelingWindow.Close();
            }

            if (isOscillogramShowing) {
                oscillogramWindow.Close();
            }
            if (isStatisticShowing)
            {
                statisticsWindow.Close();
            }

            if (isSavingWindowShowing) {
                savingWindow.Close();
            }

            if (isAnalyzerShowing) {
                analyzerWindow.Close();
            }
            if (isSpectrogramsShowing) {
                spectrogramWindow.Close();
            }
 
            foreach (var chart in charts) {
                channels.Children.Remove(chart);
            }
            charts.Clear();

            Modelling.ResetCounters();

            this.currentSignal = newSignal;

            if (this.currentSignal == null) {
                return;
            }

            for (int i = 0; i < currentSignal.channels.Count; i++) {
                var chart = new Chart(currentSignal.channels[i]);
                chart.Height = 100;

                charts.Add(chart);
                channels.Children.Add(chart);

                chart.ContextMenu = new ContextMenu();

                var item1 = new MenuItem();
                item1.Header = "Осциллограмма";
                int cur = i;
                item1.Click += (object sender, RoutedEventArgs args) => {
                    OpenOscillograms();

                    oscillogramWindow.AddChannel(currentSignal.channels[cur]);
                };
                chart.ContextMenu.Items.Add(item1);

                var item2 = new MenuItem();
                item2.Header = "Статистики";
                item2.Click += (object sender, RoutedEventArgs args) => {
                    if (!isStatisticShowing) {
                        statisticsWindow = new StatisticsWindow();
                        isStatisticShowing = true;
                        statisticsWindow.Closed += (object sender, EventArgs e) => isStatisticShowing = false;
                        statisticsWindow.Show();
                    }

                    statisticsWindow.Update(charts[cur], true);
                };
                chart.ContextMenu.Items.Add(item2);

                var item3 = new MenuItem();
                item3.Header = "Анализ";
                item3.Click += (object sender, RoutedEventArgs args) => {
                    OpenAnalyzer();
                    analyzerWindow.AddChannel(currentSignal.channels[cur]);
                };
                chart.ContextMenu.Items.Add(item3);

                var item4 = new MenuItem();
                item4.Header = "Спектрограмма";
                item4.Click += (object sender, RoutedEventArgs args) => {
                    OpenSpectrograms();
                    spectrogramWindow.AddChannel(currentSignal.channels[cur]);
                };
                chart.ContextMenu.Items.Add(item4);

                chart.Begin = 0;
                chart.End = currentSignal.SamplesCount;
            }
        }

        private void OpenFileClick(object sender, RoutedEventArgs e) {
            var openFileDialog = new OpenFileDialog();
            
            if (openFileDialog.ShowDialog() == true) {
                ResetSignal(Parser.Parse(openFileDialog.FileName));
            }
        }

        private void OnChannelClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            var point = Mouse.GetPosition(channels);

            int row = 0;
            double accumulatedHeight = 0.0;

            foreach (var chart in charts) {
                accumulatedHeight += chart.ActualHeight;
                if (accumulatedHeight >= point.Y)
                    break;
                row++;
            }

            if (currentSignal == null || row >= currentSignal.channels.Count) {
                return;
            }

            foreach (var chart in charts) {
                chart.Selected = false;
                chart.InvalidateVisual();
            }

            charts[row].Selected = true;
            //charts[row].SetSelectInterval((int)sliderBegin.Value, (int)sliderEnd.Value);
            charts[row].InvalidateVisual();
        }

        private void AboutSignalClick(object sender, RoutedEventArgs e) {
            if (!this.showing)
            {
                aboutSignalWindow = new AboutSignal();
                aboutSignalWindow.UpdateInfo(currentSignal);
                aboutSignalWindow.Closed += (object sender, System.EventArgs e) => this.showing = false;
                aboutSignalWindow.Show();
                showing = true;
            }
        }

        private void OscillogramsClick(object sender, RoutedEventArgs e) {
            OpenOscillograms();
        }

        private void OpenOscillograms() {
            if (!this.isOscillogramShowing) {
                isOscillogramShowing = true;
                oscillogramWindow = new Oscillograms();
                oscillogramWindow.Closed += (object sender, System.EventArgs e) => this.isOscillogramShowing = false;
                oscillogramWindow.Update(currentSignal);
                oscillogramWindow.Show();
            }
        }

        private void OpenAnalyzer() {
            if (!this.isAnalyzerShowing) {
                isAnalyzerShowing = true;

                int begin = 0;
                int end = 0;

                if (this.currentSignal != null) {
                    end = this.currentSignal.SamplesCount - 1;
                }

                if (isOscillogramShowing) {
                    begin = oscillogramWindow.GetBegin();
                    end = oscillogramWindow.GetEnd();
                }

                analyzerWindow = new AnalyzerWindow(begin, end);
                analyzerWindow.Closed += (object sender, System.EventArgs e) => this.isAnalyzerShowing = false;
                analyzerWindow.Show();
            }
        }

        private void OpenSpectrograms() {
            if (!this.isSpectrogramsShowing) {
                isSpectrogramsShowing = true;
                spectrogramWindow = new SpectrogramWindow();
                spectrogramWindow.Closed += (object sender, System.EventArgs e) => this.isSpectrogramsShowing = false;
                spectrogramWindow.Show();
            }
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e) {
            if (this.currentSignal == null) {
                MessageBox.Show("Нет сигнала для сохранения", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!this.isSavingWindowShowing) {
                int begin = 0;
                int end = this.currentSignal.SamplesCount - 1;

                if (oscillogramWindow != null && isOscillogramShowing) {
                    begin = oscillogramWindow.GetBegin();
                    end = oscillogramWindow.GetEnd();
                }

                savingWindow = new SaveWindow(this.currentSignal, begin, end);
                savingWindow.Closed += (object sender, System.EventArgs e) => this.isSavingWindowShowing = false;
                savingWindow.Show();
                this.isSavingWindowShowing = true;
            }
        }
    }
}
