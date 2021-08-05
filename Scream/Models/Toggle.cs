using System.ComponentModel;

namespace Scream.Models
{
    public class Toggle : INotifyPropertyChanged
    {
        private bool _ColorScheme;
        public bool ColorScheme
        {
            get
            {
                return _ColorScheme;
            }
            set
            {
                _ColorScheme = value;
                OnPropertyChanged("ColorScheme");
            }
        }

        private bool _AutoStart;
        public bool AutoStart
        {
            get
            {
                return _AutoStart;
            }
            set
            {
                _AutoStart = value;
                OnPropertyChanged("AutoStart");
            }
        }

        private bool _UDPSupport;
        public bool UDPSupport
        {
            get
            {
                return _UDPSupport;
            }
            set
            {
                _UDPSupport = value;
                OnPropertyChanged("UDPSupport");
            }
        }

        private bool _ShareOverLan;
        public bool ShareOverLan
        {
            get
            {
                return _ShareOverLan;
            }
            set
            {
                _ShareOverLan = value;
                OnPropertyChanged("ShareOverLan");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }
}
