using System.Windows.Controls;

namespace RotinaClone.App.Views
{
    public partial class WinPeCreatorView : UserControl
    {
        public WinPeCreatorView()
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
