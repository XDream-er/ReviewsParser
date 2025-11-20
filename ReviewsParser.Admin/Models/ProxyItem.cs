using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ReviewsParser.Admin
{
    public class ProxyItem : INotifyPropertyChanged
    {
        public string Ip { get; set; } = "";
        public int Port { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string Protocol { get; set; } = "HTTP";
        public string? Comment { get; set; }

        public string DisplayName => $"{Ip}:{Port} {Comment}";

        private long _pingMs = -1;
        public long PingMs
        {
            get => _pingMs;
            set
            {
                _pingMs = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PingDisplay));
                OnPropertyChanged(nameof(IsBest));
            }
        }

        public string PingDisplay => _pingMs == -1 ? "..." : (_pingMs == 9999 ? "Error" : $"{_pingMs} ms");

        private bool _isBest;
        public bool IsBest
        {
            get => _isBest;
            set { _isBest = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? prop = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}