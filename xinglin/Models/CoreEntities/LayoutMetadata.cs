using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace xinglin.Models.CoreEntities
{
    public partial class LayoutMetadata : ObservableObject
    {
        [ObservableProperty]
        private string _paperType = "A4";

        [ObservableProperty]
        private bool _isLandscape = false;

        [ObservableProperty]
        private double _paperWidth = 210;

        [ObservableProperty]
        private double _paperHeight = 297;

        [ObservableProperty]
        private double _marginLeft = 10;

        [ObservableProperty]
        private double _marginRight = 10;

        [ObservableProperty]
        private double _marginTop = 10;

        [ObservableProperty]
        private double _marginBottom = 10;

        [ObservableProperty]
        private ObservableCollection<ControlElement> _fixedElements = new ObservableCollection<ControlElement>();

        [ObservableProperty]
        private ObservableCollection<ControlElement> _editableElements = new ObservableCollection<ControlElement>();
    }
}
