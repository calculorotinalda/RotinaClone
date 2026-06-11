using System.Windows;
using System.Windows.Controls;
using RotinaClone.App.ViewModels;

namespace RotinaClone.App.Views
{
    public partial class CloneWizardView : UserControl
    {
        public CloneWizardView()
        {
            InitializeComponent();
            DataContextChanged += CloneWizardView_DataContextChanged;
        }

        private void CloneWizardView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is CloneWizardViewModel vm)
            {
                vm.OnSpeedUpdated = (read, write) =>
                {
                    // Update speed graph on UI thread
                    Dispatcher.Invoke(() =>
                    {
                        CloningSpeedChart.AddDataPoint(read, write);
                    });
                };
            }
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
