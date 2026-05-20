using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Npgsql;

namespace AlgebraSystem
{
    public partial class MainWindow : Window
    {
        private string connectionString = "Host=localhost;Port=5432;Database=AlgebraDB;Username=postgres;Password=sa";

        public MainWindow()
        {
            InitializeComponent();
            LoadTopics();
            LoadSquares();
            LoadCubes();
            LoadFactorials();
            UpdateStatus("Приложение готово к работе");
        }

        private void LoadTopics()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT Id, Name FROM Topics ORDER BY Id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        TopicsList.Items.Clear();
                        while (reader.Read())
                        {
                            TopicsList.Items.Add(new TopicItem
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }
                UpdateStatus($"Загружено {TopicsList.Items.Count} тем");
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки тем: {ex.Message}");
            }
        }

        private void TopicsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TopicsList.SelectedItem is TopicItem selected)
            {
                LoadTopicContent(selected.Id);
            }
        }

        private void LoadTopicContent(int topicId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT Name, Description, Content FROM Topics WHERE Id = @id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", topicId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                TopicContent.Text = $"**{reader.GetString(0)}**\n\n{reader.GetString(1)}\n\n{reader.GetString(2)}";
                            }
                        }
                    }
                }
                UpdateStatus("Тема загружена");
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки темы: {ex.Message}");
            }
        }

        private void AddTopicBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewTopicName.Text))
            {
                ShowError("Введите название темы!");
                return;
            }

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "INSERT INTO Topics (Name, Description, Content) VALUES (@name, @desc, @content)";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", NewTopicName.Text.Trim());
                        cmd.Parameters.AddWithValue("@desc", NewTopicDesc.Text.Trim());
                        cmd.Parameters.AddWithValue("@content", NewTopicContent.Text.Trim());
                        cmd.ExecuteNonQuery();
                    }
                }

                NewTopicName.Text = "";
                NewTopicDesc.Text = "";
                NewTopicContent.Text = "";
                LoadTopics();
                UpdateStatus("Новая тема добавлена!");
                ShowMessage("Успех", "Тема успешно добавлена");
            }
            catch (NpgsqlException ex) when (ex.Message.Contains("unique"))
            {
                ShowError("Тема с таким названием уже существует!");
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка добавления темы: {ex.Message}");
            }
        }

        private void LoadSquares(string filter = "")
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT number, square FROM numbersquares";
                    if (!string.IsNullOrEmpty(filter))
                        sql += $" WHERE number = {filter} OR square = {filter}";
                    sql += " ORDER BY number";
                    DataTable dt = new DataTable();
                    using (var adapter = new NpgsqlDataAdapter(sql, conn))
                    {
                        adapter.Fill(dt);
                    }
                    SquaresGrid.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки квадратов: {ex.Message}");
            }
        }

        private void LoadCubes(string filter = "")
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT number, cube FROM numbercubes";
                    if (!string.IsNullOrEmpty(filter))
                        sql += $" WHERE number = {filter} OR cube = {filter}";
                    sql += " ORDER BY number";
                    DataTable dt = new DataTable();
                    using (var adapter = new NpgsqlDataAdapter(sql, conn))
                    {
                        adapter.Fill(dt);
                    }
                    CubesGrid.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки кубов: {ex.Message}");
            }
        }

        private void LoadFactorials(string filter = "")
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT number, factorial FROM numberfactorials";
                    if (!string.IsNullOrEmpty(filter))
                        sql += $" WHERE number = {filter} OR factorial = {filter}";
                    sql += " ORDER BY number";
                    DataTable dt = new DataTable();
                    using (var adapter = new NpgsqlDataAdapter(sql, conn))
                    {
                        adapter.Fill(dt);
                    }
                    FactorialsGrid.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки факториалов: {ex.Message}");
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int filter = 0;
            if (int.TryParse(SearchBox.Text, out filter))
            {
                LoadSquares(filter.ToString());
                LoadCubes(filter.ToString());
                LoadFactorials(filter.ToString());
            }
            else
            {
                LoadSquares();
                LoadCubes();
                LoadFactorials();
            }
        }

        private void RefreshTablesBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadSquares();
            LoadCubes();
            LoadFactorials();
            UpdateStatus("Таблицы обновлены");
        }

        private void SolveEquationBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double a, b, c;
                if (!double.TryParse(CoefA.Text, out a) || !double.TryParse(CoefB.Text, out b) || !double.TryParse(CoefC.Text, out c))
                {
                    ShowError("Введите корректные числовые коэффициенты!");
                    return;
                }

                string result = "";
                string method = UseDiscriminant.IsChecked == true ? "discriminant" : "vieta";

                if (UseDiscriminant.IsChecked == true)
                {
                    result = SolveByDiscriminant(a, b, c);
                }
                else
                {
                    result = SolveByVieta(a, b, c);
                }

                EquationResult.Text = result;
                SaveToHistory(a, b, c, result, method);
                UpdateStatus($"Уравнение решено методом {(UseDiscriminant.IsChecked == true ? "дискриминанта" : "Виета")}");
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка решения: {ex.Message}");
            }
        }

        private string SolveByDiscriminant(double a, double b, double c)
        {
            if (a == 0)
            {
                if (b == 0)
                    return c == 0 ? "Уравнение имеет бесконечно много решений" : "Уравнение не имеет решений";
                else
                    return $"Линейное уравнение: x = {(-c / b):F3}";
            }

            double d = b * b - 4 * a * c;

            if (d < 0)
                return "Дискриминант отрицательный. Действительных корней нет.";

            if (Math.Abs(d) < 1e-10)
            {
                double x = -b / (2 * a);
                return $"Дискриминант = 0\nОдин корень: x = {x:F3}";
            }

            double x1 = (-b + Math.Sqrt(d)) / (2 * a);
            double x2 = (-b - Math.Sqrt(d)) / (2 * a);
            return $"Дискриминант = {d:F3}\nДва корня:\nx₁ = {x1:F3}\nx₂ = {x2:F3}";
        }

        private string SolveByVieta(double a, double b, double c)
        {
            if (a != 1)
            {
                return "Теорема Виета применяется только для приведённого уравнения (a = 1)!";
            }

            double sum = -b;
            double product = c;

            List<double> roots = new List<double>();
            int maxAttempt = (int)Math.Abs(product) + 10;

            for (int i = -maxAttempt; i <= maxAttempt; i++)
            {
                if (Math.Abs(i * i - sum * i + product) < 0.001)
                {
                    roots.Add(i);
                }
            }

            if (roots.Count == 0)
                return "Целых корней не найдено. Попробуйте метод дискриминанта.";

            if (roots.Count == 1 && Math.Abs(roots[0] * 2 - sum) < 0.001)
                return $"Корень: x = {roots[0]:F3} (кратный)";

            if (roots.Count >= 2)
                return $"Корни:\nx₁ = {roots[0]:F3}\nx₂ = {roots[1]:F3}";

            return "Корни не найдены";
        }

        private void SaveToHistory(double a, double b, double c, string solution, string method)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "INSERT INTO EquationHistory (EquationType, CoefficientA, CoefficientB, CoefficientC, Solution) VALUES (@type, @a, @b, @c, @sol)";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@type", method);
                        cmd.Parameters.AddWithValue("@a", a);
                        cmd.Parameters.AddWithValue("@b", b);
                        cmd.Parameters.AddWithValue("@c", c);
                        cmd.Parameters.AddWithValue("@sol", solution);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения истории: {ex.Message}");
            }
        }

        private void ClearEquationBtn_Click(object sender, RoutedEventArgs e)
        {
            CoefA.Text = "1";
            CoefB.Text = "0";
            CoefC.Text = "0";
            EquationResult.Text = "";
            UpdateStatus("Поля очищены");
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            UpdateStatus($"Ошибка: {message}");
        }

        private void ShowMessage(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateStatus(string message)
        {
            StatusText.Text = $"[{DateTime.Now:HH:mm:ss}] {message}";
        }
    }

    public class TopicItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString() => Name;
    }
    public class NumberFormatConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return "";

            if (value is int intValue)
                return string.Format("{0:N0}", intValue).Replace(",", " ");

            if (value is long longValue)
                return string.Format("{0:N0}", longValue).Replace(",", " ");

            if (value is double doubleValue)
                return string.Format("{0:N0}", doubleValue).Replace(",", " ");

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}