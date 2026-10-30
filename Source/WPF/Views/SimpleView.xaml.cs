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

namespace WWSTE.WPF.Views
{
    /// <summary>
    /// Interaction logic for SimpleView.xaml
    /// </summary>
    public partial class SimpleView : UserControl
    {
        public SimpleView()
        {
            InitializeComponent();
        }

        private void Splitter_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2)
                return;

            TopRow.Height = new GridLength(1, GridUnitType.Star);
            BottomRow.Height = new GridLength(1, GridUnitType.Star);
            e.Handled = true;
        }
    }
}
