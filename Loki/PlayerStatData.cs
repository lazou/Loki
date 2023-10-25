using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Loki
{
    public class PlayerStatData : INotifyPropertyChanged
    {
        private float _value;

        public PlayerStatType Type { get; }

        public float Value
        {
            get => _value;
            set
            {
                if (value.Equals(_value)) return;
                if (value <= 0f) value = 0f;
                _value = value;
                OnPropertyChanged();
            }
        }

        public PlayerStatData(PlayerStatType type, float value)
        {
            Type = type;
            Value = value;
        }

        public event PropertyChangedEventHandler PropertyChanged; [NotifyPropertyChangedInvocator]

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
