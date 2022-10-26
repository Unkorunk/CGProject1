using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CGProject1.Chart;
using CGProject1.SignalProcessing;
using CGProject1.SignalProcessing.Models;


namespace CGProject1
{
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

            if (MainWindow.Instance.currentSignal != null) {
                var linearSuperposBtn = new Button();
                linearSuperposBtn.Content = "Линейная суперпозиция";
                linearSuperposBtn.Click += OpenLinearSuperpos;
                SuperpositionsPanel.Children.Add(linearSuperposBtn);

                var multSuperposBtn = new Button();
                multSuperposBtn.Content = "Мультипликативная суперпозиция";
                multSuperposBtn.Click += OpenMultiplicativeSuperpos;
                SuperpositionsPanel.Children.Add(multSuperposBtn);
            }

            PreviewButton.IsEnabled = false;
            ChannelSaveBtn.IsEnabled = false;
        }

        private enum ModelType {
            Empty,
            Model,
            LinearSuperpos,
            MultSuperpos
        }

        private ModelType curModelType = ModelType.Empty;

        private ChannelConstructor currentModel = null;
        private TextBox[] argumentsFields = null;
        private TextBox[] varargFields = null;
        private StackPanel[] varargPanels = null;
        private int samplesCount;
        private double samplingFrq;

        private TextBox superposCoef = null;
        private TextBox[] channelMultipliers = null;

        private void OpenModel(object sender, RoutedEventArgs e) {
            var btn = sender as Button;
            var btnModel = btn.Tag as ChannelConstructor;

            if (btnModel != this.currentModel) {
                this.curModelType = ModelType.Model;
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
                    field.Text = btnModel.LastValues[i].ToString(CultureInfo.InvariantCulture);
                    field.PreviewTextInput += previewTextInput;
                    DataObject.AddPastingHandler(field, previewPasting);

                    ArgumentsPanel.Children.Add(field);
                    argumentsFields[i] = field;
                }

                varargFields = new TextBox[btnModel.VarArgNames.Length];
                varargPanels = new StackPanel[btnModel.VarArgNames.Length];

                for (int i = 0; i < btnModel.VarArgNames.Length; i++) {
                    varargFields[i] = new TextBox();
                    string defaultVarargs = "";

                    for (int j = 0; j < btnModel.LastVarargs[i].Length; j++) {
                        defaultVarargs += btnModel.LastVarargs[i][j].ToString(CultureInfo.InvariantCulture) + ", ";
                    }

                    varargFields[i].Text = defaultVarargs[0..^2];

                    var label = new Label();
                    string header = btnModel.VarArgNames[i];
                    label.Content = header + ":";
                    ArgumentsPanel.Children.Add(label);
                    ArgumentsPanel.Children.Add(varargFields[i]);
                }

                var savePresetBtn = new Button();
                savePresetBtn.Content = "Сохранить набор значений";
                savePresetBtn.Click += (object sender, RoutedEventArgs e) => {
                    var args = ValidateArguments();
                    var varargs = ValidateVarArgs();

                    btnModel.AddPreset(args, varargs);

                    var presetBtn = new Button();
                    int idx = btnModel.presets.Count - 1;
                    presetBtn.Content = "Пресет " + idx.ToString();
                    var preset = btnModel.presets[idx];

                    presetBtn.Click += (object sender, RoutedEventArgs e) => {
                        var args = preset.Args;
                        for (int j = 0; j < args.Length; j++) {
                            argumentsFields[j].Text = args[j].ToString(CultureInfo.InvariantCulture);
                        }

                        var varargs = preset.VarArgs;
                        for (int j = 0; j < varargs.Length; j++) {
                            string curVarargs = "";
                            for (int k = 0; k < varargs[j].Length; k++) {
                                curVarargs += varargs[j][k].ToString(CultureInfo.InvariantCulture) + ", ";
                            }

                            varargFields[j].Text = curVarargs[0..^2];
                        }
                    };

                    ArgumentsPanel.Children.Add(presetBtn);
                };
                savePresetBtn.Margin = new Thickness(0, 2.5, 0, 2.5);
                ArgumentsPanel.Children.Add(savePresetBtn);

                for (int i = 0; i < btnModel.presets.Count; i++) {
                    var presetBtn = new Button();
                    int idx = i;
                    presetBtn.Content = "Пресет " + idx.ToString();
                    var preset = btnModel.presets[i];

                    presetBtn.Click += (object sender, RoutedEventArgs e) => {
                        var args = preset.Args;
                        for (int j = 0; j < args.Length; j++) {
                            argumentsFields[j].Text = args[j].ToString(CultureInfo.InvariantCulture);
                        }

                        var varargs = preset.VarArgs;
                        for (int j = 0; j < varargs.Length; j++) {
                            string curVarargs = "";
                            for (int k = 0; k < varargs[j].Length; k++) {
                                curVarargs += varargs[j][k].ToString(CultureInfo.InvariantCulture) + ", ";
                            }

                            varargFields[j].Text = curVarargs[0..^2];
                        }
                    };

                    ArgumentsPanel.Children.Add(presetBtn);
                }

                PreviewButton.IsEnabled = true;
                ChannelSaveBtn.IsEnabled = true;
            }
        }

        private void OpenLinearSuperpos(object sender, RoutedEventArgs e) {
            if (this.curModelType != ModelType.LinearSuperpos) {
                this.curModelType = ModelType.LinearSuperpos;
                OpenSuperpos();
            }
        }

        private void OpenMultiplicativeSuperpos(object sender, RoutedEventArgs e) {
            if (this.curModelType != ModelType.MultSuperpos) {
                this.curModelType = ModelType.MultSuperpos;
                OpenSuperpos();
            }
        }

        private void OpenSuperpos() {
            this.currentModel = null;
            ArgumentsPanel.Children.Clear();

            var label = new Label();
            label.Content = "Свободный коэффициент";
            ArgumentsPanel.Children.Add(label);

            superposCoef = new TextBox();
            superposCoef.Text = "0";
            superposCoef.PreviewTextInput += previewTextInput;
            superposCoef.Margin = new Thickness(0, 0, 0, 5);
            ArgumentsPanel.Children.Add(superposCoef);

            var border = new Border();
            border.BorderBrush = Brushes.Black;
            border.BorderThickness = new Thickness(2);
            ArgumentsPanel.Children.Add(border);

            var channelSelector = new Grid();
            var titleRow = new RowDefinition();
            titleRow.Height = new GridLength(25);
            channelSelector.RowDefinitions.Add(titleRow);

            var channelColumn = new ColumnDefinition();
            channelSelector.ColumnDefinitions.Add(channelColumn);

            var checkColumn = new ColumnDefinition();
            checkColumn.Width = new GridLength(100);
            channelSelector.ColumnDefinitions.Add(checkColumn);

            var titleBorder = new Border();
            titleBorder.BorderBrush = Brushes.Black;
            titleBorder.BorderThickness = new Thickness(1);
            Grid.SetRow(titleBorder, 0);
            Grid.SetColumn(titleBorder, 0);
            var title1 = new Label();
            title1.Content = "Канал";
            titleBorder.Child = title1;

            var emptyBorder = new Border();
            emptyBorder.BorderBrush = Brushes.Black;
            emptyBorder.BorderThickness = new Thickness(1);
            Grid.SetRow(emptyBorder, 0);
            Grid.SetColumn(emptyBorder, 1);
            var title2 = new Label();
            title2.Content = "Коэф.";
            emptyBorder.Child = title2;

            channelSelector.Children.Add(titleBorder);
            channelSelector.Children.Add(emptyBorder);

            channelMultipliers = new TextBox[MainWindow.Instance.currentSignal.channels.Count];

            for (int i = 0; i < channelMultipliers.Length; i++) {
                var row = new RowDefinition();
                row.Height = new GridLength(25);
                channelSelector.RowDefinitions.Add(row);

                var channel = MainWindow.Instance.currentSignal.channels[i];

                var channelLabel = new Label();
                channelLabel.Content = channel.Name;
                Grid.SetColumn(channelLabel, 0);
                Grid.SetRow(channelLabel, i + 1);
                channelSelector.Children.Add(channelLabel);

                var channelMultiplier = new TextBox();
                channelMultiplier.Text = "0";
                channelMultipliers[i] = channelMultiplier;
                channelMultiplier.PreviewTextInput += previewTextInput;
                Grid.SetColumn(channelMultiplier, 1);
                Grid.SetRow(channelMultiplier, i + 1);
                channelSelector.Children.Add(channelMultiplier);
            }

            border.Child = channelSelector;

            PreviewButton.IsEnabled = true;
            ChannelSaveBtn.IsEnabled = true;
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

        private double[][] ValidateVarArgs() {
            var varargs = new double[currentModel.VarArgNames.Length][];

            for (int i = 0; i < currentModel.VarArgNames.Length; i++) {
                string[] varargsStr = varargFields[i].Text.Split(',');
                varargs[i] = new double[varargsStr.Length];

                string newVarargs = "";

                for (int j = 0; j < varargsStr.Length; j++) {
                    if (!double.TryParse(varargsStr[j], NumberStyles.Any, CultureInfo.InvariantCulture, out varargs[i][j])) {
                        MessageBox.Show("Некорректные параметры", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return null;
                    }

                    newVarargs += varargs[i][j].ToString(CultureInfo.InvariantCulture) + ", ";
                }

                varargFields[i].Text = newVarargs[0..^2];
            }

            return varargs;
        }

        private double[] GetSuperposArgs() {
            double[] args = new double[this.channelMultipliers.Length + 1];

            if (!double.TryParse(superposCoef.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out args[0])) {
                MessageBox.Show("Некорректные параметры", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            for (int i = 0; i < this.channelMultipliers.Length; i++) {
                if (!double.TryParse(channelMultipliers[i].Text, NumberStyles.Any, CultureInfo.InvariantCulture, out args[i + 1])) {
                    MessageBox.Show("Некорректные параметры", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }

            return args;
        }

        private void OnPreview_Click(object sender, RoutedEventArgs e) {
            Channel channel = null;

            if (this.curModelType != ModelType.Model) {
                double[] a = GetSuperposArgs();
                
                if (a == null) {
                    return;
                }

                if (this.curModelType == ModelType.LinearSuperpos) {
                    channel = Modelling.LinearSuperpos(a, MainWindow.Instance.currentSignal.channels.ToArray(), true);
                } else {
                    channel = Modelling.MultiplicativeSuperpos(a, MainWindow.Instance.currentSignal.channels.ToArray(), true);
                }
            } else {
                double[] args = ValidateArguments();
                if (args == null) {
                    return;
                }

                if (this.currentModel == Modelling.randomModels[0]) {
                    if (args[1] <= args[0]) {
                        MessageBox.Show("Некорректные параметры", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                double[][] varargs = ValidateVarArgs();
                if (varargs == null) {
                    return;
                }

                channel = this.currentModel.CreatePreviewChannel(this.samplesCount, args, varargs, this.samplingFrq, Modelling.defaultStartDateTime);
            }

            ChartPreview.Children.Clear();

            var chart = new ChartLine(channel);
            chart.Height = 100;
            chart.ShowCurrentXY = true;

            ChartPreview.Children.Add(chart);
        }

        private void OnSave_Click(object sender, RoutedEventArgs e) {
            Channel channel = null;

            if (this.curModelType != ModelType.Model) {
                double[] a = GetSuperposArgs();

                if (a == null) {
                    return;
                }

                if (this.curModelType == ModelType.LinearSuperpos) {
                    channel = Modelling.LinearSuperpos(a, MainWindow.Instance.currentSignal.channels.ToArray(), false);
                } else {
                    channel = Modelling.MultiplicativeSuperpos(a, MainWindow.Instance.currentSignal.channels.ToArray(), false);
                }
            } else {
                var args = ValidateArguments();
                if (args == null) {
                    return;
                }
                if (this.currentModel == Modelling.randomModels[0]) {
                    if (args[1] <= args[0]) {
                        MessageBox.Show("Некорректные параметры", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                var varargs = ValidateVarArgs();

                channel = this.currentModel.CreateChannel(this.samplesCount, args, varargs, this.samplingFrq, Modelling.defaultStartDateTime);
            }

            var curSignal = MainWindow.Instance.currentSignal;

            if (curSignal == null || Math.Abs(curSignal.SamplingFrq - this.samplingFrq) > 1e-8 || curSignal.SamplesCount != this.samplesCount) {
                var newSignal = new Signal("Model signal");
                newSignal.SamplingFrq = this.samplingFrq;
                newSignal.StartDateTime = Modelling.defaultStartDateTime;
                newSignal.channels.Add(channel);
                newSignal.UpdateChannelsInfo();
                MainWindow.Instance.ResetSignal(newSignal);
                this.currentModel.IncCounter();
                return;
            }

            MainWindow.Instance.AddChannel(channel);
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
