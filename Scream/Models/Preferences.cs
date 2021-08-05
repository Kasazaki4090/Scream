using System.ComponentModel;

namespace Scream.Models
{
    public class Settings : INotifyPropertyChanged
    {
        private int _HTTP;
        public int HTTP
        {
            get
            {
                return _HTTP;
            }
            set
            {
                _HTTP = value;
                OnPropertyChanged("HTTP");
            }
        }
        private int _SOCKS5;
        public int SOCKS5
        {
            get
            {
                return _SOCKS5;
            }
            set
            {
                _SOCKS5 = value;
                OnPropertyChanged("SOCKS5");
            }
        }
        private string _DNS;
        public string DNS
        {
            get
            {
                return _DNS;
            }
            set
            {
                _DNS = value;
                OnPropertyChanged("DNS");
            }
        }
        private int _LogLevelIndex;
        public int LogLevelIndex
        {
            get
            {
                return _LogLevelIndex;
            }
            set
            {
                _LogLevelIndex = value;
                OnPropertyChanged("LogLevelIndex");
            }
        }
        private string _Bypass;
        public string Bypass
        {
            get
            {
                return _Bypass;
            }
            set
            {
                _Bypass = value;
                OnPropertyChanged("Bypass");
            }
        }
        private string _Subscription;
        public string Subscription
        {
            get
            {
                return _Subscription;
            }
            set
            {
                _Subscription = value;
                OnPropertyChanged("Subscription");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
