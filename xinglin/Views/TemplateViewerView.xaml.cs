using System.Windows.Controls;
using xinglin.ViewModels;

namespace xinglin.Views
{
    public partial class TemplateViewerView : UserControl
    {
        public TemplateViewerView(TemplateViewerViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
