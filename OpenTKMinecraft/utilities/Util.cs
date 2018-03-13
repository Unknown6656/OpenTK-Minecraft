using System;

namespace OpenTKMinecraft.Utilities
{
    public sealed class Indexer<I, V>
    {
        internal Action<I, V> Setter { get; }
        internal Func<I, V> Getter { get; }


        public V this[I i]
        {
            set => Setter(i, value);
            get => Getter(i);
        }

        public Indexer(Func<I, V> getter, Action<I, V> setter)
        {
            Setter = setter is null ? throw new ArgumentException("The setter function must not be null.", nameof(setter)) : setter;
            Getter = getter is null ? throw new ArgumentException("The getter function must not be null.", nameof(getter)) : getter;
        }
    }

    public sealed class ReadOnlyIndexer<I, V>
    {
        Func<I, V> Getter { get; }


        public V this[I i] => Getter(i);

        public ReadOnlyIndexer(Func<I, V> getter) => Getter = getter is null ? throw new ArgumentException("The getter function must not be null.", nameof(getter)) : getter;
    }

    public sealed class WriteOnlyIndexer<I, V>
    {
        internal Action<I, V> Setter { get; }


        public V this[I i]
        {
            set => Setter(i, value);
        }

        public WriteOnlyIndexer(Action<I, V> setter) => Setter = setter is null ? throw new ArgumentException("The setter function must not be null.", nameof(setter)) : setter;
    }
}
