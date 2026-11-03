using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ZwCadPlugin.Models
{
    /// <summary>
    /// Một mục trong từ điển dịch ghi chú tiếng Nhật sang tiếng Việt.
    /// </summary>
    public class NoteDictionaryItem : INotifyPropertyChanged
    {
        private string _japanese = string.Empty;
        private string _vietnamese = string.Empty;

        public string Japanese
        {
            get => _japanese;
            set
            {
                if (_japanese != value)
                {
                    _japanese = value ?? string.Empty;
                    OnPropertyChanged();
                }
            }
        }

        public string Vietnamese
        {
            get => _vietnamese;
            set
            {
                if (_vietnamese != value)
                {
                    _vietnamese = value ?? string.Empty;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
