using System.ComponentModel;

namespace Scream.Models
{
    public class RouteSummary
    {
        public string Name { get; set; }
        public string DomainStrategy { get; set; }
    }

    public class Rule : INotifyPropertyChanged
    {

        private string _RuleJson;
        public string RuleJson
        {
            get
            {
                return _RuleJson;
            }
            set
            {
                _RuleJson = value;
                OnPropertyChanged("RuleJson");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }
}
