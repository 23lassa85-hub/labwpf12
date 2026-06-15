using System;
using System.Data;
using System.Globalization;
using System.Windows.Data;

namespace WpfMatrixApp.Converters
{
    [ValueConversion(typeof(double[,]), typeof(DataView))]
    public class MatrixToDataTableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not double[,] matrix)
                return null!;

            var table = new DataTable();
            int n = matrix.GetLength(0);

            for (int j = 0; j < n; j++)
                table.Columns.Add($"Col{j + 1}", typeof(double));

            for (int i = 0; i < n; i++)
            {
                var row = table.NewRow();
                for (int j = 0; j < n; j++)
                    row[j] = matrix[i, j];
                table.Rows.Add(row);
            }

            return table.DefaultView;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
