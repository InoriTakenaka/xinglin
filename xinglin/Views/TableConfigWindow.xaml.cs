using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using xinglin.Models.CoreEntities;

namespace xinglin.Views
{
    public partial class TableConfigWindow : Window
    {
        public TableElement Table { get; set; }

        public TableConfigWindow(TableElement table)
        {
            InitializeComponent();
            Table = table;
            DataContext = table;
            InitializeColumnsDataGrid();
            InitializeRowsDataGrid();
        }

        private void InitializeColumnsDataGrid()
        {
            ColumnsDataGrid.ItemsSource = Table.Columns;
        }

        private void InitializeRowsDataGrid()
        {
            // 清空现有列
            RowsDataGrid.Columns.Clear();

            // 添加行号列
            DataGridTextColumn rowNumberColumn = new DataGridTextColumn
            {
                Header = "行号",
                Width = 50
            };
            rowNumberColumn.Binding = new System.Windows.Data.Binding("ItemIndex") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.FindAncestor, typeof(DataGridRow), 1) };
            RowsDataGrid.Columns.Add(rowNumberColumn);

            // 为每一列创建一个DataGridTextColumn
            foreach (var column in Table.Columns)
            {
                DataGridTextColumn dataColumn = new DataGridTextColumn
                {
                    Header = column.Name,
                    Width = new DataGridLength(column.Width, DataGridLengthUnitType.Pixel)
                };
                // 使用自定义绑定路径，通过列名获取单元格值
                dataColumn.Binding = new System.Windows.Data.Binding($"CellValues[{column.Name}]") { Mode = System.Windows.Data.BindingMode.TwoWay };
                RowsDataGrid.Columns.Add(dataColumn);
            }

            // 确保行数正确
            EnsureRowCount();

            // 设置行数据源
            RowsDataGrid.ItemsSource = Table.Rows;
        }

        private void EnsureRowCount()
        {
            // 确保行数与RowCount一致
            while (Table.Rows.Count < Table.RowCount)
            {
                TableRow newRow = new TableRow
                {
                    RowIndex = Table.Rows.Count
                };
                // 使用列的默认值填充新行
                foreach (var column in Table.Columns)
                {
                    newRow.CellValues[column.Name] = column.DefaultValue;
                }
                Table.Rows.Add(newRow);
            }

            while (Table.Rows.Count > Table.RowCount)
            {
                Table.Rows.RemoveAt(Table.Rows.Count - 1);
            }

            // 更新所有行的 RowIndex
            for (int i = 0; i < Table.Rows.Count; i++)
            {
                Table.Rows[i].RowIndex = i;
            }
        }

        private void AddColumn_Click(object sender, RoutedEventArgs e)
        {
            TableColumn newColumn = new TableColumn
            {
                Name = $"列{Table.Columns.Count + 1}",
                Width = 100,
                DefaultValue = "",
                ControlType = ControlType.Label,
                IsEditable = true,
                Index = Table.Columns.Count
            };
            Table.Columns.Add(newColumn);

            // 更新行数据，为新列添加默认值
            foreach (var row in Table.Rows)
            {
                row.CellValues[newColumn.Name] = newColumn.DefaultValue;
            }

            // 重新初始化行数据网格
            InitializeRowsDataGrid();
        }

        private void DeleteColumn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (button.DataContext is TableColumn column && Table != null && Table.Columns != null && Table.Columns.Contains(column))
                {
                    // 从所有行中删除该列的值
                    if (Table.Rows != null)
                    {
                        foreach (var row in Table.Rows)
                        {
                            if (row != null && row.CellValues != null && row.CellValues.ContainsKey(column.Name))
                            {
                                row.CellValues.Remove(column.Name);
                            }
                        }
                    }

                    // 删除列
                    Table.Columns.Remove(column);

                    // 更新剩余列的 Index
                    for (int i = 0; i < Table.Columns.Count; i++)
                    {
                        Table.Columns[i].Index = i;
                    }

                    // 重新初始化行数据网格
                    InitializeRowsDataGrid();
                }
            }
        }

        private void AddRow_Click(object sender, RoutedEventArgs e)
        {
            TableRow newRow = new TableRow
            {
                RowIndex = Table.Rows.Count
            };
            // 使用列的默认值填充新行
            foreach (var column in Table.Columns)
            {
                newRow.CellValues[column.Name] = column.DefaultValue;
            }
            Table.Rows.Add(newRow);
            Table.RowCount = Table.Rows.Count;
            RowsDataGrid.Items.Refresh();
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (Table.Rows.Count > 0)
            {
                Table.Rows.RemoveAt(Table.Rows.Count - 1);
                Table.RowCount = Table.Rows.Count;
                // 更新剩余行的 RowIndex
                for (int i = 0; i < Table.Rows.Count; i++)
                {
                    Table.Rows[i].RowIndex = i;
                }
                RowsDataGrid.Items.Refresh();
            }
        }

        private void SetRowCount_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(RowCountTextBox.Text, out int rowCount) && rowCount > 0)
            {
                Table.RowCount = rowCount;
                EnsureRowCount();
                RowsDataGrid.Items.Refresh();
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}