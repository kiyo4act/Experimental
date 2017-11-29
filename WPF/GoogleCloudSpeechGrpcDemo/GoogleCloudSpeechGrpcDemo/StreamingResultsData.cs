using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleTranscribeStreamingDemo
{
    public class StreamingResultsData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _transcript;
        public string Transcript
        {
            get { return _transcript; }
            set { _transcript = value; OnPropertyChanged("Transcript"); }
        }

        private float _confidence;
        public float Confidence
        {
            get { return _confidence; }
            set { _confidence = value; OnPropertyChanged("Transcript"); }
        }
        private bool _isFinal;
        public bool IsFinal
        {
            get { return _isFinal; }
            set { _isFinal = value; OnPropertyChanged("Transcript"); }
        }
        void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class StreamingResultsDataCollection : ObservableCollection<StreamingResultsData>
    {
        public StreamingResultsDataCollection()
        {
            
        }
    }
}
