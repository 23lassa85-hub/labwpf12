using System;
using System.ComponentModel;

namespace WpfMatrixApp.Models
{
    public class MatrixModel : INotifyPropertyChanged
    {
        private double[,] _data = new double[0, 0];
        private double _diagonalDifference;
        private bool _isEvenDifference;
        private string _status = "";

        public double[,] Data
        {
            get => _data;
            set
            {
                _data = value;
                OnPropertyChanged(nameof(Data));
                OnPropertyChanged(nameof(Rows));
                OnPropertyChanged(nameof(Cols));
            }
        }

        public int Rows => _data.GetLength(0);
        public int Cols => _data.GetLength(1);

        public double DiagonalDifference
        {
            get => _diagonalDifference;
            set
            {
                _diagonalDifference = value;
                IsEvenDifference = Math.Abs(value) % 2 < 0.0001;
                OnPropertyChanged(nameof(DiagonalDifference));
                OnPropertyChanged(nameof(IsEvenDifference));
            }
        }

        public bool IsEvenDifference
        {
            get => _isEvenDifference;
            private set
            {
                _isEvenDifference = value;
                OnPropertyChanged(nameof(IsEvenDifference));
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
