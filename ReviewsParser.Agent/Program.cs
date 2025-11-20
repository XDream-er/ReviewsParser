using ReviewsParser.Agent.Parsers;
using System.Diagnostics;

var agentId = $"Agent-{Process.GetCurrentProcess().Id}";
var apiClient = new ApiClient();
var parsers = new Dictionary<string, IParser> { { "drom.ru", new DromParser() } };
var pollInterval = TimeSpan.FromMilliseconds(500); 

Console.WriteLine($"{agentId} запущен.");
Console.WriteLine("-----------------------------------");

while (true)
{
    Console.WriteLine("Ищет задачу...");
    ParsingTask? task = null;

    try { task = await apiClient.GetTaskAsync(agentId); }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Ошибка при получении задачи: {ex.Message}");
        Console.ResetColor();
        await Task.Delay(TimeSpan.FromSeconds(30));
        continue;
    }

    if (task != null)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Получена задача #{task.Id} для сайта '{task.TargetSite}'.");
        Console.ResetColor();

        if (!parsers.ContainsKey(task.TargetSite))
        {
            await HandleError("Не найден парсер для этого сайта.", task.Id);
            continue;
        }

        var parser = parsers[task.TargetSite];
        var reviewBuffer = new List<ParsedReview>();
        var cancellationTokenSource = new CancellationTokenSource();
        string lastUrl = task.ProgressIdentifier ?? string.Empty;
        bool taskCompletedSuccessfully = false;

        var statusWatcher = Task.Run(async () =>
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var currentStatus = await apiClient.GetTaskStatusAsync(task.Id);
                    if (currentStatus == TaskStatus.Paused)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("  Пауза...");
                        Console.ResetColor();
                        cancellationTokenSource.Cancel();
                        break;
                    }
                }
                catch { }
                await Task.Delay(pollInterval, cancellationTokenSource.Token);
            }
        }, cancellationTokenSource.Token);

        try
        {
            await foreach (var review in parser.ParseAsync(task, apiClient, cancellationTokenSource.Token))
            {
                cancellationTokenSource.Token.ThrowIfCancellationRequested();

                review.ParsingTaskId = task.Id;
                reviewBuffer.Add(review);
                lastUrl = review.Url;
                Console.WriteLine($"  -> Найден отзыв: {review.Car}");

                if (reviewBuffer.Count >= 5)
                {
                    await apiClient.SubmitResultsAsync(reviewBuffer);
                    await apiClient.UpdateProgressAsync(task.Id, lastUrl, reviewBuffer.Count);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"  -> Сохранены данные из {reviewBuffer.Count} результатов.");
                    Console.ResetColor();
                    reviewBuffer.Clear();
                }
            }

            if (!cancellationTokenSource.IsCancellationRequested)
            {
                taskCompletedSuccessfully = true;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            await HandleError($"Критическая ошибка во время парсинга: {ex.Message}", task.Id);
        }
        finally
        {
            if (!cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
            }

            if (reviewBuffer.Any())
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  -> Сохранение последних данных из {reviewBuffer.Count} результатов...");
                Console.ResetColor();
                await apiClient.SubmitResultsAsync(reviewBuffer);
                await apiClient.UpdateProgressAsync(task.Id, lastUrl, reviewBuffer.Count);
            }

            if (taskCompletedSuccessfully)
            {
                await apiClient.CompleteTaskAsync(task.Id);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Обработка задачи #{task.Id} завершена.");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Задача #{task.Id} приостановленна. Прогресс сохранен.");
                Console.ResetColor();
            }
        }
    }
    else
    {
        Console.WriteLine("Задач нет. Ожидание 5 секунд.");
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
    Console.WriteLine("-----------------------------------");
}

async Task HandleError(string message, int taskId)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(message);
    Console.ResetColor();
    await apiClient.FailTaskAsync(taskId);
}