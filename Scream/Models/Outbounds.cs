using System.ComponentModel;

namespace Scream.Models
{
    public class OutboundSummary
    {
        public string Protocol { get; set; }
        public string Tag { get; set; }
    }

    public class Outbound : INotifyPropertyChanged
    {

        private string _OutboundJson;
        public string OutboundJson
        {
            get
            {
                return _OutboundJson;
            }
            set
            {
                _OutboundJson = value;
                OnPropertyChanged("OutboundJson");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }
}
