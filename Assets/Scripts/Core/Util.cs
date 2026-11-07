// Gatomate - núcleo del juego (sin dependencias de Unity).
// Utilidades: posición en el tablero y generador de números al azar con semilla.
using System;
using System.Collections.Generic;

namespace Gatomate.Core
{
    /// <summary>Posición en el tablero (X = columna, Y = fila).</summary>
    public struct Pos : IEquatable<Pos>
    {
        public readonly int X;
        public readonly int Y;

        public Pos(int x, int y) { X = x; Y = y; }

        /// <summary>Distancia en casillas (sin diagonales).</summary>
        public int Dist(Pos o) { return Math.Abs(X - o.X) + Math.Abs(Y - o.Y); }

        public bool Equals(Pos o) { return X == o.X && Y == o.Y; }
        public override bool Equals(object obj) { return obj is Pos && Equals((Pos)obj); }
        public override int GetHashCode() { return unchecked(X * 397 ^ Y); }
        public static bool operator ==(Pos a, Pos b) { return a.Equals(b); }
        public static bool operator !=(Pos a, Pos b) { return !a.Equals(b); }
        public override string ToString() { return "(" + X + "," + Y + ")"; }
    }

    /// <summary>
    /// Generador de números al azar con semilla, escrito a mano para que dé los mismos
    /// resultados en cualquier plataforma (System.Random cambia entre Unity y .NET).
    /// Así una semilla reproduce siempre el mismo tablero.
    /// </summary>
    public sealed class Azar
    {
        private uint _s;
        public readonly int Semilla;

        public Azar(int semilla)
        {
            Semilla = semilla;
            _s = unchecked((uint)semilla * 2654435761u + 0x9E3779B9u);
            if (_s == 0) _s = 1;
            for (int i = 0; i < 8; i++) Sig();
        }

        /// <summary>Estado interno del generador (para guardar y cargar la partida y seguir con la misma secuencia).</summary>
        public uint Estado { get { return _s; } set { _s = value == 0 ? 1u : value; } }

        private uint Sig()
        {
            _s ^= _s << 13;
            _s ^= _s >> 17;
            _s ^= _s << 5;
            return _s;
        }

        /// <summary>Entero entre min (incluido) y maxExcl (excluido).</summary>
        public int Entre(int min, int maxExcl)
        {
            if (maxExcl <= min) return min;
            return min + (int)(Sig() % (uint)(maxExcl - min));
        }

        public double Doble() { return (Sig() & 0xFFFFFF) / 16777216.0; }

        /// <summary>true con probabilidad p (0 a 1).</summary>
        public bool Prob(double p) { return Doble() < p; }

        public T Elegir<T>(IList<T> l) { return l[Entre(0, l.Count)]; }

        public void Mezclar<T>(IList<T> l)
        {
            for (int i = l.Count - 1; i > 0; i--)
            {
                int j = Entre(0, i + 1);
                T t = l[i]; l[i] = l[j]; l[j] = t;
            }
        }

        /// <summary>Mezcla varios números en una semilla nueva.</summary>
        public static int Derivar(params int[] v)
        {
            unchecked
            {
                int h = 17;
                for (int i = 0; i < v.Length; i++) h = h * 31 + v[i];
                return h;
            }
        }
    }
}
