using System.Windows.Controls;

namespace RotinaClone.App.Views
{
    public partial class LogView : UserControl
    {
        public LogView()
        {
            InitializeComponent();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.ScrollToEnd();
            }
        }
    }
}
