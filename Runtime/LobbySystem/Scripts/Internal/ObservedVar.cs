using System;

namespace AnyVR.LobbySystem.Internal
{
    public interface IReadOnlyObservedVar<T>
    {
        T Value { get; }
        event Action<T> OnValueChanged;
    }

    internal class ObservedVar<T> : IReadOnlyObservedVar<T>
    {
        private T _value;

        public ObservedVar(T initialValue = default)
        {
            _value = initialValue;
        }
        public event Action<T> OnValueChanged;

        public T Value
        {
            get => _value;
            set
            {
                if (Equals(_value, value))
                    return;

                _value = value;
                OnValueChanged?.Invoke(_value);
            }
        }
    }
}
