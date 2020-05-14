using System.Windows;

namespace CGProject1
{
    /// <summary>
    /// Interaction logic for SettingsFixedScale.xaml
    /// </summary>
    public partial class SettingsFixedScale : Window
    {
        public bool Status { get; set; }
        private int _from, _to;
        public int From { get => _from; set => _from = value; }
        public int To { get => _to; set => _to = value; }

        public SettingsFixedScale()
        {
            InitializeComponent();
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            Status = true;
            if (int.TryParse(TextBoxFrom.Text, out _from) && int.TryParse(TextBoxTo.Text, out _to) &&
                _from >= 0 && _to >= 0)
            {
                this.Close();
            } else
            {
                MessageBox.Show("Invalid values", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Status = false;
        }
    }
}
