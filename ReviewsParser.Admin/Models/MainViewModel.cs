using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Net.Sockets;

namespace ReviewsParser.Admin
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient = new();
        private readonly CsvExportService _csvExportService = new();
        private readonly DispatcherTimer _timer;

        public ObservableCollection<ParsingTask> Tasks { get; } = new();

        public ObservableCollection<string> AvailableSites { get; } = new();

        private ParsingTask? _selectedTask;
        public ParsingTask? SelectedTask
        {
            get => _selectedTask;
            set { _selectedTask = value; OnPropertyChanged(); }
        }

        private string? _selectedSite;
        public string? SelectedSite
        {
            get => _selectedSite;
            set { _selectedSite = value; OnPropertyChanged(); }
        }

        private string? _proxyAddress;
        public string? ProxyAddress { get => _proxyAddress; set { _proxyAddress = value; OnPropertyChanged(); } }
        private string? _proxyUser;
        public string? ProxyUser { get => _proxyUser; set { _proxyUser = value; OnPropertyChanged(); } }
        private string? _proxyPass;
        public string? ProxyPass { get => _proxyPass; set { _proxyPass = value; OnPropertyChanged(); } }

        //Свойства вкладки прокси
        public ObservableCollection<ProxyItem> Proxies { get; } = new();
        private ProxyItem? _selectedProxyItem;
        public ProxyItem? SelectedProxyItem
        {
            get => _selectedProxyItem;
            set
            {
                _selectedProxyItem = value;
                OnPropertyChanged();
                if (value != null)
                {
                    ProxyAddress = $"{value.Ip}:{value.Port}";
                    ProxyUser = value.Username;
                    ProxyPass = value.Password;
                }
            }
        }

        public ICommand CreateTaskCommand { get; }
        public ICommand PauseTaskCommand { get; }
        public ICommand ResumeTaskCommand { get; }
        public ICommand ExportToCsvCommand { get; }
        public ICommand LoadedCommand { get; }
        public ICommand StartLocalAgentCommand { get; }
        public ICommand ImportProxiesCommand { get; }
        public ICommand PingProxiesCommand { get; }
        public ICommand SelectBestProxyCommand { get; }

        public MainViewModel()
        {
            CreateTaskCommand = new RelayCommand(async _ => await CreateTask(), _ => !string.IsNullOrEmpty(SelectedSite));
            PauseTaskCommand = new RelayCommand(async _ => await PauseTask(), _ => SelectedTask?.Status == TaskStatus.Running);
            ResumeTaskCommand = new RelayCommand(async _ => await ResumeTask(), _ => SelectedTask?.Status == TaskStatus.Paused || SelectedTask?.Status == TaskStatus.Failed);
            ExportToCsvCommand = new RelayCommand(async _ => await ExportResults(), _ => SelectedTask?.Status == TaskStatus.Completed);
            StartLocalAgentCommand = new RelayCommand(_ => StartNewAgentProcess());
            ImportProxiesCommand = new RelayCommand(_ => ImportProxiesFromFile());
            PingProxiesCommand = new RelayCommand(async _ => await PingAllProxies());
            SelectBestProxyCommand = new RelayCommand(_ => SelectBestProxy());

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _timer.Tick += async (s, e) => await RefreshTasks();

            LoadedCommand = new RelayCommand(async _ =>
            {
                await LoadInitialData();
                _timer.Start();
            });
        }

        private async Task LoadInitialData()
        {
            try
            {
                var sites = await _apiClient.GetAvailableSitesAsync();
                AvailableSites.Clear();
                foreach (var site in sites)
                {
                    AvailableSites.Add(site);
                }
                SelectedSite = AvailableSites.FirstOrDefault();
            }
            catch (Exception ex) { MessageBox.Show($"Не удалось загрузить список сайтов: {ex.Message}"); }

            await RefreshTasks();
        }

        private async Task RefreshTasks()
        {
            try
            {
                var tasksFromServer = await _apiClient.GetAllTasksAsync();
                var selectedTaskId = SelectedTask?.Id;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var tasksToRemove = Tasks.Where(localTask => !tasksFromServer.Any(serverTask => serverTask.Id == localTask.Id)).ToList();
                    foreach (var taskToRemove in tasksToRemove)
                    {
                        Tasks.Remove(taskToRemove);
                    }

                    foreach (var serverTask in tasksFromServer)
                    {
                        var existingTask = Tasks.FirstOrDefault(t => t.Id == serverTask.Id);
                        if (existingTask != null)
                        {
                            var index = Tasks.IndexOf(existingTask);
                            Tasks[index] = serverTask;
                        }
                        else
                        {
                            Tasks.Add(serverTask);
                        }
                    }

                    if (selectedTaskId != null)
                    {
                        SelectedTask = Tasks.FirstOrDefault(t => t.Id == selectedTaskId);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось обновить задачи: {ex.Message}");
                _timer.Stop();
            }
        }
        private async Task CreateTask()
        {
            if (string.IsNullOrEmpty(SelectedSite)) return;
            try
            {
                await _apiClient.CreateTaskAsync(SelectedSite, ProxyAddress, ProxyUser, ProxyPass);
                await RefreshTasks();
            }
            catch (Exception ex) { MessageBox.Show($"Не удалось создать задачу: {ex.Message}"); }
        }

        private async Task PauseTask()
        {
            if (SelectedTask == null) return;
            try
            {
                await _apiClient.PauseTaskAsync(SelectedTask.Id);
                await RefreshTasks();
            }
            catch (Exception ex) { MessageBox.Show($"Не удалось поставить на паузу: {ex.Message}"); }
        }
        private async Task ResumeTask()
        {
            if (SelectedTask == null) return;
            try
            {
                await _apiClient.ResumeTaskAsync(SelectedTask.Id, ProxyAddress, ProxyUser, ProxyPass);
                await RefreshTasks();
            }
            catch (Exception ex) { MessageBox.Show($"Не удалось возобновить: {ex.Message}"); }
        }
        private async Task ExportResults()
        {
            if (SelectedTask == null) return;
            try
            {
                List<ParsedReview> reviewsToExport = await _apiClient.GetTaskResultsAsync(SelectedTask.Id);
                string defaultFileName = $"task_{SelectedTask.Id}_{SelectedTask.TargetSite}_results.csv";
                _csvExportService.Export(reviewsToExport, defaultFileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении данных для экспорта:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void StartNewAgentProcess()
        {
            try
            {
                const string agentExeName = "ReviewsParser.Agent.exe";
                string currentAppPath = AppDomain.CurrentDomain.BaseDirectory;
                string agentPath = Path.Combine(currentAppPath, agentExeName);

                if (File.Exists(agentPath))
                {
                    Process.Start(agentPath);
                }
                else
                {
                    MessageBox.Show($"Не удалось найти файл агента по пути:\n{agentPath}\n.",
                                    "Файл не найден", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при запуске нового агента:\n{ex.Message}",
                                "Ошибка запуска", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        //Прокси
        private void ImportProxiesFromFile()
        {
            var openFileDialog = new OpenFileDialog { Filter = "Text files (*.txt)|*.txt" };
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var lines = File.ReadAllLines(openFileDialog.FileName);
                    Proxies.Clear();
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var proxy = ParseProxyString(line);
                        if (proxy != null) Proxies.Add(proxy);
                    }
                    MessageBox.Show($"Загружено {Proxies.Count} прокси.");
                }
                catch (Exception ex) { MessageBox.Show("Ошибка чтения файла: " + ex.Message); }
            }
        }
        private ProxyItem? ParseProxyString(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;
            line = line.Trim();

            string cleanLine = line;
            string detectedProtocol = "HTTP";

            if (cleanLine.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                cleanLine = cleanLine.Substring(7);
                detectedProtocol = "HTTP";
            }
            else if (cleanLine.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                cleanLine = cleanLine.Substring(8);
                detectedProtocol = "HTTPS";
            }
            else if (cleanLine.StartsWith("socks4://", StringComparison.OrdinalIgnoreCase))
            {
                cleanLine = cleanLine.Substring(9);
                detectedProtocol = "SOCKS4";
            }
            else if (cleanLine.StartsWith("socks5://", StringComparison.OrdinalIgnoreCase))
            {
                cleanLine = cleanLine.Substring(9);
                detectedProtocol = "SOCKS5";
            }

            var addressParts = cleanLine.Split(':');

            if (addressParts.Length >= 2)
            {
                if (int.TryParse(addressParts[1], out int port))
                {
                    var item = new ProxyItem
                    {
                        Ip = addressParts[0],
                        Port = port,
                        Protocol = detectedProtocol
                    };
                    if (addressParts.Length >= 4)
                    {
                        item.Username = addressParts[2];
                        item.Password = addressParts[3];
                    }

                    return item;
                }
            }

            return null;
        }
        private async Task PingAllProxies()
        {
            var tasks = new List<Task>();
            foreach (var p in Proxies)
            {
                p.IsBest = false;
                tasks.Add(Task.Run(async () => {
                    p.PingMs = -1;
                    try
                    {
                        using var client = new TcpClient();
                        var sw = Stopwatch.StartNew();
                        var t = client.ConnectAsync(p.Ip, p.Port);
                        if (await Task.WhenAny(t, Task.Delay(2000)) == t)
                        {
                            sw.Stop();
                            p.PingMs = sw.ElapsedMilliseconds;
                        }
                        else p.PingMs = 9999;
                    }
                    catch { p.PingMs = 9999; }
                }));
            }
            await Task.WhenAll(tasks);
        }
        private void SelectBestProxy()
        {
            var best = Proxies.Where(p => p.PingMs > 0 && p.PingMs < 5000).OrderBy(p => p.PingMs).FirstOrDefault();
            if (best != null)
            {
                foreach (var p in Proxies) p.IsBest = false;
                best.IsBest = true;
                SelectedProxyItem = best;
            }
        }
    }
}