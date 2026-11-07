// Gatomate - núcleo del juego (sin dependencias de Unity).
// Tablero: casillas con terreno y una capa de zonas temporales (fuego, enredadera, frasco).
using System.Collections.Generic;

namespace Gatomate.Core
{
    public sealed class Tablero
    {
        public readonly int Ancho;
        public readonly int Alto;
        private readonly Terreno[,] _t;
        private readonly Dictionary<Pos, Zona> _zonas = new Dictionary<Pos, Zona>();
        private readonly Dictionary<Pos, int> _vidaObs = new Dictionary<Pos, int>();     // vida de los obstáculos que se rompen
        private readonly Dictionary<Pos, int> _monedas = new Dictionary<Pos, int>();     // monedas tiradas en el suelo

        public static readonly Pos[] Dirs4 = { new Pos(1, 0), new Pos(-1, 0), new Pos(0, 1), new Pos(0, -1) };

        public Tablero(int ancho, int alto)
        {
            Ancho = ancho; Alto = alto;
            _t = new Terreno[ancho, alto];
        }

        public bool Dentro(Pos p) { return p.X >= 0 && p.Y >= 0 && p.X < Ancho && p.Y < Alto; }

        public Terreno TerrenoEn(Pos p) { return _t[p.X, p.Y]; }

        public void PonerTerreno(Pos p, Terreno t)
        {
            _t[p.X, p.Y] = t;
            _vidaObs.Remove(p);
            if (t == Terreno.Caja) _vidaObs[p] = Config.VidaCaja;
            else if (t == Terreno.RocaFragil) _vidaObs[p] = Config.VidaRocaFragil;
            else if (t == Terreno.Barril) _vidaObs[p] = Config.VidaBarril;
            else if (t == Terreno.Roca) _vidaObs[p] = Config.VidaRoca;
            else if (t == Terreno.Muro) _vidaObs[p] = Config.VidaMuro;
        }

        /// <summary>Vida con la que nace cada obstáculo (0 si no es un obstáculo).</summary>
        public static int VidaInicial(Terreno t)
        {
            switch (t)
            {
                case Terreno.Caja: return Config.VidaCaja;
                case Terreno.RocaFragil: return Config.VidaRocaFragil;
                case Terreno.Barril: return Config.VidaBarril;
                case Terreno.Roca: return Config.VidaRoca;
                case Terreno.Muro: return Config.VidaMuro;
                default: return 0;
            }
        }

        public static bool EsObstaculoRompible(Terreno t) { return t == Terreno.Caja || t == Terreno.RocaFragil || t == Terreno.Barril || t == Terreno.Roca || t == Terreno.Muro; }

        public bool EsRompible(Pos p) { return Dentro(p) && EsObstaculoRompible(_t[p.X, p.Y]); }

        public int VidaObstaculo(Pos p) { int v; return _vidaObs.TryGetValue(p, out v) ? v : 0; }

        /// <summary>Daña un obstáculo rompible. Devuelve true si se rompe (queda suelo).</summary>
        public bool DanarObstaculo(Pos p, int d)
        {
            if (!EsRompible(p)) return false;
            int v = VidaObstaculo(p) - d;
            if (v > 0) { _vidaObs[p] = v; return false; }
            _vidaObs.Remove(p);
            _t[p.X, p.Y] = Terreno.Suelo;
            return true;
        }

        public bool Pasable(Pos p)
        {
            if (!Dentro(p)) return false;
            Terreno t = _t[p.X, p.Y];
            return !EsObstaculoRompible(t);
        }

        /// <summary>Movimiento que cuesta entrar en la casilla (barro 2, agua 3).</summary>
        public int CosteEntrar(Pos p)
        {
            Terreno t = _t[p.X, p.Y];
            if (t == Terreno.Agua) return Config.CosteMovAgua;
            if (t == Terreno.Barro) return Config.CosteMovBarro;
            return 1;
        }

        // ---- monedas
        public IEnumerable<KeyValuePair<Pos, int>> Monedas { get { return _monedas; } }
        public int MonedasEn(Pos p) { int n; return _monedas.TryGetValue(p, out n) ? n : 0; }
        public void PonerMonedas(Pos p, int n) { _monedas[p] = MonedasEn(p) + n; }
        public int TomarMonedas(Pos p) { int n = MonedasEn(p); _monedas.Remove(p); return n; }
        public int TotalMonedas { get { int s = 0; foreach (int n in _monedas.Values) s += n; return s; } }

        public IEnumerable<Zona> Zonas { get { return _zonas.Values; } }

        public Zona ZonaEn(Pos p)
        {
            Zona z;
            return _zonas.TryGetValue(p, out z) ? z : null;
        }

        /// <summary>Pone una zona; reemplaza la que hubiera en esa casilla.</summary>
        public void PonerZona(ZonaTipo tipo, Pos p, int turnos, Bando dueno)
        {
            _zonas[p] = new Zona { Tipo = tipo, Pos = p, Turnos = turnos, Dueno = dueno };
        }

        public void QuitarZona(Pos p) { _zonas.Remove(p); }

        /// <summary>Resta una ronda a cada zona y quita las que terminan.</summary>
        public void AvanzarZonas()
        {
            var fin = new List<Pos>();
            foreach (var z in _zonas.Values)
            {
                z.Turnos--;
                if (z.Turnos <= 0) fin.Add(z.Pos);
            }
            foreach (var p in fin) _zonas.Remove(p);
        }

        public List<Pos> Vecinos(Pos p)
        {
            var l = new List<Pos>(4);
            for (int i = 0; i < 4; i++)
            {
                Pos q = new Pos(p.X + Dirs4[i].X, p.Y + Dirs4[i].Y);
                if (Dentro(q)) l.Add(q);
            }
            return l;
        }

        /// <summary>La casilla y sus 4 vecinas (las que existan).</summary>
        public List<Pos> Cruz(Pos c)
        {
            var l = new List<Pos>(5);
            l.Add(c);
            l.AddRange(Vecinos(c));
            return l;
        }

        /// <summary>
        /// Línea de 3 casillas contiguas con centro en 'centro', perpendicular a la dirección desde 'origen'
        /// (una "pared"). Las casillas que no se pueden pisar se omiten. (Orientación: propuesta.)
        /// </summary>
        public List<Pos> Linea3(Pos origen, Pos centro)
        {
            int dx = centro.X - origen.X, dy = centro.Y - origen.Y;
            bool horizontal = System.Math.Abs(dx) >= System.Math.Abs(dy);
            Pos a = horizontal ? new Pos(centro.X, centro.Y - 1) : new Pos(centro.X - 1, centro.Y);
            Pos b = horizontal ? new Pos(centro.X, centro.Y + 1) : new Pos(centro.X + 1, centro.Y);
            var l = new List<Pos>(3);
            if (Pasable(a)) l.Add(a);
            if (Pasable(centro)) l.Add(centro);
            if (Pasable(b)) l.Add(b);
            return l;
        }

        /// <summary>true si todas las casillas pasables están conectadas entre sí.</summary>
        public bool EsConexo()
        {
            int total = 0;
            Pos? inicio = null;
            for (int x = 0; x < Ancho; x++)
                for (int y = 0; y < Alto; y++)
                    if (Pasable(new Pos(x, y))) { total++; if (inicio == null) inicio = new Pos(x, y); }
            if (inicio == null) return false;

            var vistos = new HashSet<Pos>();
            var cola = new Queue<Pos>();
            vistos.Add(inicio.Value);
            cola.Enqueue(inicio.Value);
            while (cola.Count > 0)
            {
                Pos p = cola.Dequeue();
                foreach (Pos q in Vecinos(p))
                    if (Pasable(q) && vistos.Add(q)) cola.Enqueue(q);
            }
            return vistos.Count == total;
        }
    }
}
