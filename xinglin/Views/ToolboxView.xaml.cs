using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using xinglin.Models.CoreEntities;
using xinglin.ViewModels;

namespace xinglin.Views
{
    public partial class ToolboxView : UserControl
    {
        public ToolboxView()
        {
            InitializeComponent();
        }

        private void Button_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border != null && border.Tag is ControlType controlType)
            {
                var viewModel = DataContext as ToolboxViewModel;
                if (viewModel != null)
                {
                    var control = viewModel.CreateControl(controlType);
                    DataObject dataObject = new DataObject(typeof(ControlElement), control);
                    DragDrop.DoDragDrop(border, dataObject, DragDropEffects.Copy);
                }
            }
        }
    }
}
