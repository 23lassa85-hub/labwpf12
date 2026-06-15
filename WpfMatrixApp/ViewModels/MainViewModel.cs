using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfMatrixApp.Models;

namespace WpfMatrixApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _sourceFilePath = "";
        private string _secondFilePath = "";
        private string _statusMessage = "Готово";
        private double _progressValue;
        private bool _isProcessing;
        private bool _hasData;
        private MatrixFileModel _sourceFileModel = new();
        private MatrixFileModel _secondFileModel = new();
        private ObservableCollection<MatrixModel> _sourceMatrices = new();
        private ObservableCollection<MatrixModel> _secondMatrices = new();

        public string SourceFilePath
        {
            get => _sourceFilePath;
            set
            {
                _sourceFilePath = value;
                OnPropertyChanged(nameof(SourceFilePath));
            }
        }

        public string SecondFilePath
        {
            get => _secondFilePath;
            set
            {
                _secondFilePath = value;
                OnPropertyChanged(nameof(SecondFilePath));
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public double ProgressValue
        {
            get => _progressValue;
            set
            {
                _progressValue = value;
                OnPropertyChanged(nameof(ProgressValue));
            }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                _isProcessing = value;
                OnPropertyChanged(nameof(IsProcessing));
            }
        }

        public bool HasData
        {
            get => _hasData;
            set
            {
                _hasData = value;
                OnPropertyChanged(nameof(HasData));
            }
        }

        public MatrixFileModel SourceFileModel
        {
            get => _sourceFileModel;
            set
            {
                _sourceFileModel = value;
                OnPropertyChanged(nameof(SourceFileModel));
            }
        }

        public MatrixFileModel SecondFileModel
        {
            get => _secondFileModel;
            set
            {
                _secondFileModel = value;
                OnPropertyChanged(nameof(SecondFileModel));
            }
        }

        public ObservableCollection<MatrixModel> SourceMatrices
        {
            get => _sourceMatrices;
            set
            {
                _sourceMatrices = value;
                OnPropertyChanged(nameof(SourceMatrices));
            }
        }

        public ObservableCollection<MatrixModel> SecondMatrices
        {
            get => _secondMatrices;
            set
            {
                _secondMatrices = value;
                OnPropertyChanged(nameof(SecondMatrices));
            }
        }

        public ICommand OpenFileCommand { get; }
        public ICommand SaveFileCommand { get; }
        public ICommand GenerateTestFileCommand { get; }
        public ICommand ProcessCommand { get; }
        public ICommand ExitCommand { get; }

        public MainViewModel()
        {
            OpenFileCommand = new RelayCommand(_ => OpenFile(), _ => !IsProcessing);
            SaveFileCommand = new RelayCommand(_ => SaveFile(), _ => HasData && !IsProcessing);
            GenerateTestFileCommand = new RelayCommand(_ => GenerateTestFile(), _ => !IsProcessing);
            ProcessCommand = new RelayCommand(_ => ProcessMatrices(), _ => HasData && !IsProcessing);
            ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());
        }

        private void OpenFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                Title = "Открыть файл с матрицами"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    SourceFilePath = dialog.FileName;
                    ReadFile(SourceFilePath);
                    HasData = true;
                    StatusMessage = $"Загружено {SourceMatrices.Count} матриц из {SourceFilePath}";
                }
                catch (FormatException ex)
                {
                    MessageBox.Show($"Ошибка формата файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusMessage = "Ошибка формата файла";
                }
                catch (IOException ex)
                {
                    MessageBox.Show($"Ошибка ввода/вывода: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusMessage = "Ошибка чтения файла";
                }
            }
        }

        private void ReadFile(string filePath)
        {
            SourceMatrices.Clear();
            SecondMatrices.Clear();


            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fileStream, Encoding.UTF8);

            string? firstLine = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(firstLine))
                throw new FormatException("Файл пуст или первая строка отсутствует");

            var parts = firstLine.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !int.TryParse(parts[0], out int k) || !int.TryParse(parts[1], out int n))
                throw new FormatException("Первая строка должна содержать два целых числа: k и n");

            if (k <= 0 || n <= 0)
                throw new FormatException("k и n должны быть положительными");

            SourceFileModel.MatrixCount = k;
            SourceFileModel.MatrixSize = n;

            for (int m = 0; m < k; m++)
            {
                var matrix = new double[n, n];
                for (int i = 0; i < n; i++)
                {
                    string? line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                        throw new FormatException($"Недостаточно строк для матрицы {m + 1}, строка {i + 1}");

                    var nums = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (nums.Length != n)
                        throw new FormatException($"Матрица {m + 1}, строка {i + 1}: ожидалось {n} чисел, получено {nums.Length}");

                    for (int j = 0; j < n; j++)
                    {
                        if (!double.TryParse(nums[j].Replace('.', ','), out matrix[i, j]))
                            throw new FormatException($"Матрица {m + 1}, строка {i + 1}, столбец {j + 1}: неверное число '{nums[j]}'");
                    }
                }

                var model = new MatrixModel { Data = matrix };
                CalculateDiagonalDifference(model);
                SourceMatrices.Add(model);
            }

            SourceFileModel.Matrices = SourceMatrices.ToList();
        }

        private void CalculateDiagonalDifference(MatrixModel model)
        {
            int n = model.Rows;
            double mainDiag = 0, sideDiag = 0;
            for (int i = 0; i < n; i++)
            {
                mainDiag += model.Data[i, i];
                sideDiag += model.Data[i, n - 1 - i];
            }
            model.DiagonalDifference = mainDiag - sideDiag;
        }

        private async void ProcessMatrices()
        {
            if (string.IsNullOrEmpty(SourceFilePath))
            {
                MessageBox.Show("Сначала откройте файл", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                Title = "Сохранить второй файл (чётные разности)",
                FileName = "even_diff_matrices.txt"
            };

            if (saveDialog.ShowDialog() != true)
                return;

            SecondFilePath = saveDialog.FileName;
            IsProcessing = true;
            StatusMessage = "Обработка...";
            ProgressValue = 0;

            try
            {
                await Task.Run(() => ProcessAndSave());


                ReadFile(SourceFilePath);
                ReadSecondFile(SecondFilePath);

                StatusMessage = $"Обработка завершена. Чётных разностей: {SecondMatrices.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обработки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Ошибка обработки";
            }
            finally
            {
                IsProcessing = false;
                ProgressValue = 100;
            }
        }

        private void ProcessAndSave()
        {
            int n = SourceFileModel.MatrixSize;
            var processedMatrices = new List<MatrixModel>();
            var evenDiffMatrices = new List<MatrixModel>();

            for (int m = 0; m < SourceMatrices.Count; m++)
            {
                var matrix = SourceMatrices[m];
                CalculateDiagonalDifference(matrix);

                if (matrix.IsEvenDifference)
                {
                    evenDiffMatrices.Add(matrix);
                    var (inverse, status) = ComputeInverse(matrix.Data);
                    var inverseModel = new MatrixModel
                    {
                        Data = inverse,
                        DiagonalDifference = 0,
                        Status = status
                    };
                    processedMatrices.Add(inverseModel);
                }
                else
                {
                    processedMatrices.Add(matrix);
                }

                double progress = (m + 1) * 100.0 / SourceMatrices.Count;
                Application.Current.Dispatcher.Invoke(() => ProgressValue = progress);
            }

            SaveMatrixFile(SourceFilePath, processedMatrices, n);
            SaveMatrixFile(SecondFilePath, evenDiffMatrices, n);
        }

        private void SaveMatrixFile(string path, List<MatrixModel> matrices, int n)
        {
            using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(fileStream, Encoding.UTF8);

            writer.WriteLine($"{matrices.Count} {n}");
            foreach (var matrix in matrices)
            {
                for (int i = 0; i < n; i++)
                {
                    var line = new StringBuilder();
                    for (int j = 0; j < n; j++)
                    {
                        line.Append(matrix.Data[i, j].ToString("F6"));
                        if (j < n - 1) line.Append(' ');
                    }
                    writer.WriteLine(line.ToString());
                }
                if (!string.IsNullOrEmpty(matrix.Status))
                    writer.WriteLine($"# {matrix.Status}");
            }
        }

        private (double[,] matrix, string status) ComputeInverse(double[,] matrix)
        {
            int n = matrix.GetLength(0);
            double[,] a = new double[n, n];
            double[,] inv = new double[n, n];

            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    a[i, j] = matrix[i, j];

            for (int i = 0; i < n; i++)
                inv[i, i] = 1.0;

            for (int i = 0; i < n; i++)
            {
                double pivot = Math.Abs(a[i, i]);
                int pivotRow = i;
                for (int r = i + 1; r < n; r++)
                {
                    if (Math.Abs(a[r, i]) > pivot)
                    {
                        pivot = Math.Abs(a[r, i]);
                        pivotRow = r;
                    }
                }

                if (pivot < 1e-10)
                {
                    var zero = new double[n, n];
                    return (zero, "Вырожденная матрица (det ≈ 0) — заменена на нулевую");
                }

                if (pivotRow != i)
                {
                    for (int c = 0; c < n; c++)
                    {
                        (a[i, c], a[pivotRow, c]) = (a[pivotRow, c], a[i, c]);
                        (inv[i, c], inv[pivotRow, c]) = (inv[pivotRow, c], inv[i, c]);
                    }
                }
                double div = a[i, i];
                for (int c = 0; c < n; c++)
                {
                    a[i, c] /= div;
                    inv[i, c] /= div;
                }

                for (int r = 0; r < n; r++)
                {
                    if (r == i) continue;
                    double factor = a[r, i];
                    for (int c = 0; c < n; c++)
                    {
                        a[r, c] -= factor * a[i, c];
                        inv[r, c] -= factor * inv[i, c];
                    }
                }
            }

            return (inv, "");
        }

        private void ReadSecondFile(string filePath)
        {
            SecondMatrices.Clear();
            if (!File.Exists(filePath)) return;

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fileStream, Encoding.UTF8);

            string? firstLine = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(firstLine)) return;

            var parts = firstLine.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !int.TryParse(parts[0], out int k) || !int.TryParse(parts[1], out int n))
                return;

            SecondFileModel.MatrixCount = k;
            SecondFileModel.MatrixSize = n;

            for (int m = 0; m < k; m++)
            {
                var matrix = new double[n, n];
                for (int i = 0; i < n; i++)
                {
                    string? line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var nums = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int j = 0; j < Math.Min(nums.Length, n); j++)
                    {
                        double.TryParse(nums[j].Replace('.', ','), out matrix[i, j]);
                    }
                }

                var model = new MatrixModel { Data = matrix };
                CalculateDiagonalDifference(model);
                SecondMatrices.Add(model);
            }

            SecondFileModel.Matrices = SecondMatrices.ToList();
        }

        private void SaveFile()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                Title = "Сохранить как"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    File.Copy(SourceFilePath, dialog.FileName, true);
                    StatusMessage = $"Файл сохранён: {dialog.FileName}";
                }
                catch (IOException ex)
                {
                    MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void GenerateTestFile()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Текстовые файлы (*.txt)|*.txt",
                Title = "Сохранить тестовый файл",
                FileName = "test_matrices.txt"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                var random = new Random();
                int k = 5; 
                int n = 4; 

                using (var fileStream = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(fileStream, Encoding.UTF8))
                {
                    writer.WriteLine($"{k} {n}");

                    for (int m = 0; m < k; m++)
                    {
                        for (int i = 0; i < n; i++)
                        {
                            var line = new StringBuilder();
                            for (int j = 0; j < n; j++)
                            {
                                int val = random.Next(-10, 11);
                                line.Append(val);
                                if (j < n - 1) line.Append(' ');
                            }
                            writer.WriteLine(line.ToString());
                        }
                    }
                } 

                SourceFilePath = dialog.FileName;
                ReadFile(SourceFilePath);
                HasData = true;
                StatusMessage = $"Тестовый файл создан: {dialog.FileName}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания тестового файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
