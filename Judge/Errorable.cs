using System;

namespace Judge
{
    public sealed class Errorable<T> : IDisposable
        where T : class
    {
        private readonly T? _value;
        private readonly string? _error;

        public bool IsError => _error != null;
        public T Value
        {
            get
            {
                if (_value == null)
                    throw new NullReferenceException();
                return _value;
            }
        }
        public string Error
        {
            get
            {
                if (_error == null)
                    throw new NullReferenceException();
                return _error;
            }
        }

        public Errorable(T value)
        {
            _value = value;
        }
        private Errorable(string? error)
        {
            _error = error;
        }

        
        public void Dispose()
        {
            (_value as IDisposable)?.Dispose();
        }

        public static implicit operator Errorable<T>(string? error) => new Errorable<T>(error: error);
    }
}
