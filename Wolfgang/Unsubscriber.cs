using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Wolfgang
{
    public class Unsubscriber<T> : IDisposable
    {
        private readonly HashSet<IObserver<T>> _observers;
        private readonly IObserver<T> _observer;
        private readonly object _observerLock;

        public Unsubscriber(
            [NotNull] object observerLock,
            [NotNull] in HashSet<IObserver<T>> observers,
            [NotNull] in IObserver<T> observer
        )
        {
            _observers = observers;
            _observer = observer;
            _observerLock = observerLock;
        }

        public Unsubscriber([NotNull] in HashSet<IObserver<T>> observers, [NotNull] in IObserver<T> observer)
        {
            _observers = observers;
            _observer = observer;
        }

        public void Dispose()
        {
            lock (_observerLock)
                if (_observer != null && _observers.Contains(_observer))
                    _observers.Remove(_observer);
        }
    }
}