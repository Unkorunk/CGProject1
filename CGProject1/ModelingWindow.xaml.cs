using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using CGProject1.SignalProcessing;


namespace CGProject1 {
    public partial class ModelingWindow : Window {
        public ModelingWindow(Signal signal) {
            InitializeComponent();

            if (signal == null) {
                this.samplesCount = 100;
                this.samplingFrq = 1;
            } else {
                this.samplesCount = signal.SamplesCount;
                this.samplingFrq = signal.SamplingFrq;
            }
            

            foreach (var model in Modelling.discreteModels) {
                var modelBtn = new Button();
                modelBtn.Content = model.ModelName;
                modelBtn.Tag = model;
                modelBtn.Click += OpenModel;

                DiscreteModelsPanel.Children.Add(modelBtn);
            }

            foreach (var model in Modelling.continiousModels) {
                var modelBtn = new Button();
                modelBtn.Content = model.ModelName;
                modelBtn.Tag = model;
                modelBtn.Click += OpenModel;

                ContinousModelPanel.Children.Add(modelBtn);
            }

            foreach (var model in Modelling.randomModels) {
                var modelBtn = new Button();
                modelBtn.Content = model.ModelName;
                modelBtn.Tag = model;
                modelBtn.Click += OpenModel;

                RandomModelPanel.Children.Add(modelBtn);
            }

            PreviewButton.IsEnabled = false;
            ChannelSaveBtn.IsEnabled = false;
        }

        private ChannelConstructor currentModel = null;
        private TextBox[] argumentsFields = null;
        private int samplesCount;
        private double samplingFrq;

        private void OpenModel(object sender, RoutedEventArgs e) {
            var btn = sender as Button;
            var btnModel = btn.Tag as ChannelConstructor;

            if (btnModel != this.currentModel) {
                this.currentModel = btnModel;
                ParamsHeader.Content = $"Параметры {this.currentModel.ModelName}";

                ArgumentsPanel.Children.Clear();
                argumentsFields = new TextBox[btnModel.ArgsNames.Length + 2];

                var samplesCountLabel = new Label();
                samplesCountLabel.Content = "Количество отсчетов";
                ArgumentsPanel.Children.Add(samplesCountLabel);

                var samplesCountField = new TextBox();

                DataObject.AddPastingHandler(samplesCountField, previewPasting);
                samplesCountField.PreviewTextInput += previewTextInput;

                samplesCountField.Text = this.samplesCount.ToString(CultureInfo.InvariantCulture);
                argumentsFields[btnModel.ArgsNames.Length] = samplesCountField;
                ArgumentsPanel.Children.Add(samplesCountField);

                var samplingFrqLabel = new Label();
                samplingFrqLabel.Content = "Частота дискретизации";
                ArgumentsPanel.Children.Add(samplingFrqLabel);

                var samplingFrqField = new TextBox();

                DataObject.AddPastingHandler(samplingFrqField, previewPasting);
                samplingFrqField.PreviewTextInput += previewTextInput;

                samplingFrqField.Text = this.samplingFrq.ToString(CultureInfo.InvariantCulture);
                argumentsFields[btnModel.ArgsNames.Length + 1] = samplingFrqField;
                ArgumentsPanel.Children.Add(samplingFrqField);

                for (int i = 0; i < btnModel.ArgsNames.Length; i++) {
                    var label = new Label();
                    string header = btnModel.ArgsNames[i];

                    if (btnModel.MinArgValues[i] != double.MinValue) {
                        header += $" от {btnModel.MinArgValues[i]}";
                    }

                    if (btnModel.MaxArgValues[i] != double.MaxValue) {
                        header += $" до {btnModel.MaxArgValues[i]}";
                    }

                    label.Content = header;
                    ArgumentsPanel.Children.Add(label);

                    var field = new TextBox();
                    field.Text = "0";
                    field.PreviewTextInput += previewTextInput;
                    DataObject.AddPastingHandler(field, previewPasting);

                    ArgumentsPanel.Children.Add(field);
                    argumentsFields[i] = field;
                }

                PreviewButton.IsEnabled = true;
                ChannelSaveBtn.IsEnabled = true;
            }
        }

        private double[] ValidateArguments() {
            var args = new double[currentModel.MinArgValues.Length];
            for (int i = 0; i < currentModel.MinArgValues.Length; i++) {
                double val = 0;
                if (!double.TryParse(argumentsFields[i].Text, NumberStyles.Any, CultureInfo.InvariantCulture, out val)) {
                    MessageBox.Show("Некорректные параметры", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }

                if (val < currentModel.MinArgValues[i]) {
                    val = currentModel.MinArgValues[i];
                }

                if (val > currentModel.MaxArgValues[i]) {
                    val = currentModel.MaxArgValues[i];
                }

                args[i] = val;
                argumentsFields[i].Text = val.ToString(CultureInfo.InvariantCulture);
            }

            int samples = (int)double.Parse(argumentsFields[argumentsFields.Length - 2].Text, CultureInfo.InvariantCulture);
            if (samples < 1) {
                samples = 1;
            }
            if (samples > 1000000000) {
                samples = 1000000000;
            }
            argumentsFields[argumentsFields.Length - 2].Text = samples.ToString(CultureInfo.InvariantCulture);

            this.samplesCount = samples;
            this.samplingFrq = double.Parse(argumentsFields[argumentsFields.Length - 1].Text, CultureInfo.InvariantCulture);

            return args;
        }

        private void OnPreview_Click(object sender, RoutedEventArgs e) {
            var args = ValidateArguments();
            if (args == null) {
                return;
            }

            var channel = this.currentModel.CreatePreviewChannel(this.samplesCount, args, this.samplingFrq, Modelling.defaultStartDateTime);

            ChartPreview.Children.Clear();

            var chart = new Chart(channel);
            chart.Height = 100;

            ChartPreview.Children.Add(chart);

            chart.Begin = 0;
            chart.End = this.samplesCount;
        }

        private void OnSave_Click(object sender, RoutedEventArgs e) {
            var args = ValidateArguments();
            if (args == null) {
                return;
            }

            var channel = this.currentModel.CreateChannel(this.samplesCount, args, this.samplingFrq, Modelling.defaultStartDateTime);

            var curSignal = MainWindow.instance.currentSignal;

            if (curSignal == null || Math.Abs(curSignal.SamplingFrq - this.samplingFrq) > 1e-8 || curSignal.SamplesCount != this.samplesCount) {
                var newSignal = new Signal("Model signal");
                newSignal.SamplingFrq = this.samplingFrq;
                newSignal.StartDateTime = Modelling.defaultStartDateTime;
                newSignal.channels.Add(channel);
                newSignal.UpdateChannelsInfo();
                MainWindow.instance.ResetSignal(newSignal);
                return;
            }

            MainWindow.instance.AddChannel(channel);
        }

        private void previewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e) {
            bool approvedDecimalPoint = false;
            bool approvedMinus = false;

            if (e.Text == ".") {
                if (!((TextBox)sender).Text.Contains("."))
                    approvedDecimalPoint = true;
            }

            if (e.Text == "-") {
                if (!((TextBox)sender).Text.Contains("-"))
                    approvedDecimalPoint = true;
            }

            if (!(char.IsDigit(e.Text, e.Text.Length - 1) || approvedDecimalPoint || approvedMinus))
                e.Handled = true;
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
            return input.All(c => char.IsDigit(c) || char.IsControl(c));
        }
    }
}
