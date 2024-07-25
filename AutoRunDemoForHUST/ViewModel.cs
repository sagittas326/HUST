using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace AutoRunDemoForHUST
{
    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private double _speed;
        public double Speed
        {
            get { return _speed; }
            set
            {
                _speed = value;
                OnPropertyChanged(nameof(Speed));
            }
        }

        private Dictionary<string, string> _campusMap;
        public Dictionary<string, string> CampusMap
        {
            get { return _campusMap; }
            set
            {
                _campusMap = value;
                OnPropertyChanged(nameof(CampusMap));
            }
        }

        private Dictionary<string, (string, int[])> _courtMap;
        public Dictionary<string, (string, int[])> CourtMap
        {
            get { return _courtMap; }
            set
            {
                _courtMap = value;
                OnPropertyChanged(nameof(CourtMap));
            }
        }

        private Dictionary<string, int> _dateMap;
        public Dictionary<string, int> DateMap
        {
            get { return _dateMap; }
            set
            {
                _dateMap = value;
                OnPropertyChanged(nameof(DateMap));
            }
        }

        private Dictionary<string, int> _timeMap;
        public Dictionary<string, int> TimeMap
        {
            get { return _timeMap; }
            set
            {
                _timeMap = value;
                OnPropertyChanged(nameof(TimeMap));
            }
        }

        public void AddToListAndUpdateText(List<string> list, string item, TextBlock textBox, string prefix)
        {
            if (!list.Contains(item))
            {
                list.Add(item);
                UpdateText(list, textBox, prefix);
            }
        }

        public void ClearListAndUpdateText(List<string> list, TextBlock textBox, string prefix)
        {
            list.Clear();
            UpdateText(list, textBox, prefix);
        }

        public void UpdateText(List<string> list, TextBlock textBox, string prefix)
        {
            //textBox.Text = $"{prefix}：\n{string.Join(";\n", list)}";
            textBox.Inlines.Clear();
            textBox.Inlines.Add(new Run($"{prefix}："));
            textBox.Inlines.AddRange(list.Select(x => new Run($"\n{x};") { Foreground = Brushes.DarkGreen, FontWeight = FontWeights.Normal }));
        }



    }
}
