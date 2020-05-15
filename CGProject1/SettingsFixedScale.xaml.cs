using System.Globalization;
using System.Linq;
using System.Windows;

namespace CGProject1
{
    /// <summary>
    /// Interaction logic for SettingsFixedScale.xaml
    /// </summary>
    public partial class SettingsFixedScale : Window
    {
        public bool Status { get; set; }
        private double _from, _to;
        public double From { get => _from; set => _from = value; }
        public double To { get => _to; set => _to = value; }

        public SettingsFixedScale()
        {
            InitializeComponent();
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(TextBoxFrom.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out _from)
                && double.TryParse(TextBoxTo.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out _to) && _from < _to)
            {
                Status = true;
                this.Close();
            } else
            {
                MessageBox.Show("Invalid values", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool TextIsNumeric(string input)
        {
            return input.All(c => char.IsDigit(c) || char.IsControl(c) || c == '.' || c == '-');
        }

        private void TextBoxFrom_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !TextIsNumeric(e.Text);
        }

        private void TextBoxTo_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !TextIsNumeric(e.Text);
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Status = false;
            this.Close();
        }
    }
}
