using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace xinglin.Models.CoreEntities
{
    public class TableColumn
    {
        public string Name { get; set; }
        public double Width { get; set; }
        public string BindingPath { get; set; }
        public string DefaultValue { get; set; }
        public ControlType ControlType { get; set; } = ControlType.Label;
        public bool IsEditable { get; set; } = true;
        public int Index { get; set; }
    }

    public class TableRow
    {
        public Dictionary<string, string> CellValues { get; set; } = new Dictionary<string, string>();
        public int RowIndex { get; set; }
    }

    public class TableElement : ControlElement
    {
        public ObservableCollection<TableColumn> Columns { get; set; } = new ObservableCollection<TableColumn>();
        public ObservableCollection<TableRow> Rows { get; set; } = new ObservableCollection<TableRow>();
        public int RowCount { get; set; } = 1;
        public bool AllowAddRows { get; set; } = true;
    }
}
