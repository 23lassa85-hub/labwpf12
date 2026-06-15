using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace WpfMatrixApp.Models
{
    public class MatrixFileModel : INotifyPropertyChanged
    {
        private string _filePath = "";
        private int _matrixCount;
        private int _matrixSize;
        private List<MatrixModel> _matrices = new();

        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                OnPropertyChanged(nameof(FilePath));
            }
        }

        public int MatrixCount
        {
            get => _matrixCount;
            set
            {
                _matrixCount = value;
                OnPropertyChanged(nameof(MatrixCount));
            }
        }

        public int MatrixSize
        {
            get => _matrixSize;
            set
            {
                _matrixSize = value;
                OnPropertyChanged(nameof(MatrixSize));
            }
        }

        public List<MatrixModel> Matrices
        {
            get => _matrices;
            set
            {
                _matrices = value;
                OnPropertyChanged(nameof(Matrices));
                OnPropertyChanged(nameof(EvenDiffCount));
                OnPropertyChanged(nameof(OddDiffCount));
            }
        }

        public int EvenDiffCount => _matrices.Count(m => m.IsEvenDifference);
        public int OddDiffCount => _matrices.Count(m => !m.IsEvenDifference);

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
