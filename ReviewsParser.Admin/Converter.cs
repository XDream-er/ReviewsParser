using System;
using System.Globalization;
using System.Windows.Data;

namespace ReviewsParser.Admin
{
    public class Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TaskStatus status)
            {
                return status switch
                {
                    TaskStatus.Pending => "Ожидание",
                    TaskStatus.Running => "В работе",
                    TaskStatus.Paused => "Пауза",
                    TaskStatus.Completed => "Завершено",
                    TaskStatus.Failed => "Ошибка",
                    _ => "Неизвестно"
                };
            }
            return string.Empty;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}