// Gatomate - vista. Escena isométrica: tablero en rombos, unidades con animaciones, efectos y reproductor de eventos.
// El núcleo resuelve cada acción al instante y deja un registro de eventos (Core/Eventos.cs); aquí se reproducen en
// orden: el gato salta de casilla en casilla, ataca, recibe daño, etc. La escena lleva su PROPIO estado visual
// (posición, vida mostrada) y al terminar de reproducir se "sincroniza" con el estado real del núcleo.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gatomate.Core;
using Random = UnityEngine.Random;

namespace Gatomate.View
{
    /// <summary>Visual de una unidad.</summary>
    public sealed class VU
    {
        public Unidad U;
        public GameObject Go;
        public Transform Cuerpo;
        public SpriteRenderer SrCuerpo, SrSombra, SrAnillo, SrBFondo, SrBRetraso, SrBVida, SrBMana, SrBManaFondo;
        public float Gx, Gy;                 // posición mostrada en la rejilla (puede ser fraccionaria al saltar)
        public float Alto;                   // altura del salto (unidades de mundo)
        public float OffX, OffY;             // desplazamientos pequeños (retroceso, temblor)
        public float Sx = 1f, Sy = 1f, Rot;  // aplastamiento y giro
        public float VidaVis, VidaRetraso;   // vida mostrada (suavizada)
        public int VidaObj;                  // vida a la que se dirige la barra
        public bool Visible = true, MiraIzq;
        public Color Tinte = Color.white;
        public float Alfa = 1f, Aparicion = 1f, Fase, Altura = 0.8f;
        public bool Muerto;
        public bool Salta;                   // salto de idle (en el campamento)
        public float Orden { get { return (Gx + Gy) * 10f; } }
    }

    /// <summary>Visual de una casilla.</summary>
    public sealed class VC
    {
        public int X, Y;
        public GameObject Go;
        public SpriteRenderer Base, Bloque, Mata, Relleno, Borde, Zona, Moneda;
        public int Monedas;
        public Terreno T;
        public float Edad, Retardo, PopT = 1f;
        public bool TieneZona;
        public ZonaTipo ZT;
        public float ProxFx;
    }

    public sealed class EscenaIso : MonoBehaviour
    {
        public const float MW = 0.64f, MH = 0.32f;   // medio ancho y medio alto del rombo (unidades de mundo)

        // ---------------------------------------------------------------- estado público
        public Combate C;
        public Fx Fx;
        public Camera Cam;
        public float Vel = 1f;                       // velocidad de las animaciones
        public Unidad ActivoVisual;                  // de quién es el turno según lo que se está mostrando
        public string Banner = "";
        public float BannerT, BannerDur = 1.6f;
        public Color BannerCol = Color.white;
        public bool Ocupado { get { return _reproduciendo || _cola.Count > 0 || _entrando; } }
        public bool Entrando { get { return _entrando; } }
        public Gato CampSel;                         // gato resaltado en el campamento
        public readonly List<VU> Unidades = new List<VU>();

        // ---------------------------------------------------------------- estado privado
        private Transform _raiz, _fxRaiz;
        private int _W = 10, _H = 10, _mundo = 1;
        private VC[,] _tiles;
        private readonly Queue<Evento> _cola = new Queue<Evento>();
        private bool _reproduciendo, _entrando;
        private float _t;
        private SpriteRenderer _fondo, _isla, _isla2;
        private readonly List<SpriteRenderer> _nubes = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> _puntos = new List<SpriteRenderer>();
        private readonly HashSet<Pos> _mMover = new HashSet<Pos>(), _mObj = new HashSet<Pos>(), _mArea = new HashSet<Pos>();
        private readonly HashSet<Pos> _mInsp = new HashSet<Pos>(), _mAmen = new HashSet<Pos>();   // inspección con clic derecho: dónde puede moverse y qué casillas amenaza
        private Pos? _hover;
        private List<Pos> _camino = new List<Pos>();
        private Vector2 _foco, _focoObj;
        private float _ambT;
        private bool _modoCamp;
        private float _flash;                         // destello blanco de pantalla (tormenta)
        private readonly List<GameObject> _temporales = new List<GameObject>();   // proyectiles y rayos en vuelo
        public float Destello { get { return _flash; } }

        // ================================================================ arranque
        public void Iniciar(Camera cam)
        {
            Cam = cam;
            Cam.orthographic = true;
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = new Color(0.08f, 0.06f, 0.14f);
            Cam.transform.position = new Vector3(0f, 0f, -10f);
            _raiz = new GameObject("Escena iso").transform;
            _fxRaiz = new GameObject("Efectos").transform;
            _fxRaiz.parent = _raiz;
            Fx = new Fx(_fxRaiz);
            var gf = new GameObject("Fondo");
            gf.transform.parent = _raiz;
            _fondo = gf.AddComponent<SpriteRenderer>();
            _fondo.sortingOrder = -1000;
            var gi = new GameObject("Isla");
            gi.transform.parent = _raiz;
            _isla = gi.AddComponent<SpriteRenderer>();
            _isla.sortingOrder = -60;
            var gi2 = new GameObject("Isla2");
            gi2.transform.parent = _raiz;
            _isla2 = gi2.AddComponent<SpriteRenderer>();
            _isla2.sortingOrder = -61;
            for (int i = 0; i < 7; i++)
            {
                var gn = new GameObject("nube");
                gn.transform.parent = _raiz;
                var sr = gn.AddComponent<SpriteRenderer>();
                sr.sortingOrder = -900 + i;
                _nubes.Add(sr);
            }
            for (int i = 0; i < 24; i++)
            {
                var gp = new GameObject("punto");
                gp.transform.parent = _raiz;
                var sr = gp.AddComponent<SpriteRenderer>();
                sr.sprite = Arte.Disco;
                sr.sortingOrder = 2000;
                gp.SetActive(false);
                _puntos.Add(sr);
            }
        }

        // ================================================================ coordenadas
        public Vector2 AMundo(float gx, float gy)
        {
            float cx = (_W - 1) * 0.5f, cy = (_H - 1) * 0.5f;
            return new Vector2(((gx - cx) - (gy - cy)) * MW, -((gx - cx) + (gy - cy)) * MH);
        }

        public Vector2 AMundo(Pos p) { return AMundo(p.X, p.Y); }

        private static int Ord(float gx, float gy) { return Mathf.RoundToInt((gx + gy) * 10f); }

        public Vector2 PantallaAMundo(Vector2 px)
        {
            Vector3 w = Cam.ScreenToWorldPoint(new Vector3(px.x, px.y, 10f));
            return new Vector2(w.x, w.y);
        }

        public Vector2 MundoAPantalla(Vector2 w)
        {
            Vector3 p = Cam.WorldToScreenPoint(new Vector3(w.x, w.y, 0f));
            return new Vector2(p.x, p.y);
        }

        /// <summary>Casilla bajo un punto de pantalla (origen abajo a la izquierda, como Input.mousePosition).</summary>
        public Pos? CasillaBajo(Vector2 px)
        {
            Vector2 w = PantallaAMundo(px);
            VU v = UnidadBajoMundo(w);
            if (v != null) return new Pos(Mathf.RoundToInt(v.Gx), Mathf.RoundToInt(v.Gy));
            float cx = (_W - 1) * 0.5f, cy = (_H - 1) * 0.5f;
            float a = w.x / MW, b = -w.y / MH;     // a = (gx-cx)-(gy-cy), b = (gx-cx)+(gy-cy)
            float gx = (a + b) * 0.5f + cx, gy = (b - a) * 0.5f + cy;
            int ix = Mathf.RoundToInt(gx), iy = Mathf.RoundToInt(gy);
            if (ix < 0 || iy < 0 || ix >= _W || iy >= _H) return null;
            return new Pos(ix, iy);
        }

        private VU UnidadBajoMundo(Vector2 w)
        {
            VU mejor = null;
            for (int i = 0; i < Unidades.Count; i++)
            {
                VU v = Unidades[i];
                if (!v.Visible || v.Muerto) continue;
                Vector2 b = AMundo(v.Gx, v.Gy);
                if (w.x > b.x - 0.3f && w.x < b.x + 0.3f && w.y > b.y - 0.1f && w.y < b.y + v.Altura + 0.1f)
                    if (mejor == null || v.Orden > mejor.Orden) mejor = v;
            }
            return mejor;
        }

        public VU UnidadBajoPantalla(Vector2 px) { return UnidadBajoMundo(PantallaAMundo(px)); }

        public VU Vis(Unidad u) { return Vis(u, false); }

        public VU Vis(Unidad u, bool crear)
        {
            if (u == null) return null;
            for (int i = 0; i < Unidades.Count; i++) if (Unidades[i].U == u) return Unidades[i];
            return crear ? CrearVU(u, true) : null;
        }

        public Vector2 Cabeza(VU v) { return AMundo(v.Gx, v.Gy) + new Vector2(v.OffX, v.Alto + v.OffY + v.Altura + 0.05f); }

        // ================================================================ construcción de escenas
        private void LimpiarEscena()
        {
            StopAllCoroutines();
            _cola.Clear(); _reproduciendo = false; _entrando = false;
            if (_tiles != null)
                foreach (VC c in _tiles) if (c != null) Destroy(c.Go);
            _tiles = null;
            foreach (VU v in Unidades) Destroy(v.Go);
            Unidades.Clear();
            if (Fx != null) Fx.Limpiar();
            ActivoVisual = null; C = null; CampSel = null;
            _mMover.Clear(); _mObj.Clear(); _mArea.Clear(); _mInsp.Clear(); _mAmen.Clear(); _hover = null; _camino = new List<Pos>();
            Banner = ""; _flash = 0f;
            BorrarTemporales();
        }

        private void BorrarTemporales()
        {
            foreach (GameObject g in _temporales) if (g != null) Destroy(g);
            _temporales.Clear();
        }

        private void PonerMundo(int mundo)
        {
            _mundo = mundo;
            Arte.Precargar(mundo);
            _fondo.sprite = Arte.Degradado(mundo);
            Paleta p = Dibujos.PaletaMundo(mundo);
            Color c1 = new Color(p.Cielo1.R, p.Cielo1.G, p.Cielo1.B, 1f);
            Color c2 = new Color(p.Cielo2.R, p.Cielo2.G, p.Cielo2.B, 1f);
            Cam.backgroundColor = c1;
            bool noche = mundo == 4 || mundo == 7;
            for (int i = 0; i < _nubes.Count; i++)
            {
                SpriteRenderer sr = _nubes[i];
                sr.sprite = Arte.Nube(i % 2);
                sr.color = new Color(1f, 1f, 1f, noche ? 0.07f : 0.55f);
                sr.transform.position = new Vector3(Random.Range(-9f, 9f), Random.Range(-1f, 4.5f), 0f);
                float k = Random.Range(0.8f, 1.6f);
                sr.transform.localScale = new Vector3(k, k, 1f);
                sr.gameObject.SetActive(mundo != 3 || i < 3);
            }
            _isla.sprite = Arte.RomboLleno;
            _isla2.sprite = Arte.RomboLleno;
        }

        private VC NuevaCasilla(int x, int y, Terreno t, float retardo)
        {
            var c = new VC { X = x, Y = y, Retardo = retardo };
            c.Go = new GameObject("casilla " + x + "," + y);
            c.Go.transform.parent = _raiz;
            Vector2 w = AMundo(x, y);
            c.Go.transform.position = new Vector3(w.x, w.y, 0f);
            int o = Ord(x, y);
            c.Base = Hijo(c.Go, "base", o);
            c.Zona = Hijo(c.Go, "zona", o + 1); c.Zona.sprite = Arte.RomboLleno; c.Zona.color = new Color(1, 1, 1, 0);
            c.Relleno = Hijo(c.Go, "resalte", o + 2); c.Relleno.sprite = Arte.RomboLleno; c.Relleno.color = new Color(1, 1, 1, 0);
            c.Bloque = Hijo(c.Go, "bloque", o + 3);
            c.Moneda = Hijo(c.Go, "moneda", o + 4); c.Moneda.gameObject.SetActive(false);
            c.Mata = Hijo(c.Go, "mata", o + 7);
            c.Borde = Hijo(c.Go, "borde", o + 9); c.Borde.sprite = Arte.RomboBorde; c.Borde.color = new Color(1, 1, 1, 0);
            PonerCasilla(c, t);
            return c;
        }

        private SpriteRenderer Hijo(GameObject padre, string nombre, int orden)
        {
            var g = new GameObject(nombre);
            g.transform.parent = padre.transform;
            g.transform.localPosition = Vector3.zero;
            var sr = g.AddComponent<SpriteRenderer>();
            sr.sortingOrder = orden;
            return sr;
        }

        private void PonerCasilla(VC c, Terreno t)
        {
            c.T = t;
            c.Base.sprite = Arte.Casilla(_mundo, t, (c.X + c.Y) % 2);
            bool bloque = t == Terreno.Roca || t == Terreno.Muro || t == Terreno.Caja || t == Terreno.RocaFragil || t == Terreno.Barril;
            c.Bloque.gameObject.SetActive(bloque);
            c.Bloque.color = Color.white;
            if (bloque)
            {
                if (t == Terreno.Roca || t == Terreno.Muro) c.Bloque.sprite = Arte.Bloque(_mundo, t == Terreno.Muro, (c.X * 3 + c.Y) % 2);
                else c.Bloque.sprite = Arte.Obstaculo(_mundo, t);
            }
            c.Mata.gameObject.SetActive(t == Terreno.HierbaAlta);
            if (t == Terreno.HierbaAlta) c.Mata.sprite = Arte.Mata(_mundo, (c.X * 3 + c.Y) % 2);
        }

        private void PonerMonedas(VC c, int n)
        {
            c.Monedas = n < 0 ? 0 : n;
            c.Moneda.gameObject.SetActive(c.Monedas > 0);
            if (c.Monedas > 0) c.Moneda.sprite = Arte.Moneda(c.Monedas);
        }

        public static Color ColorElem(Elemento e, Color porDefecto)
        {
            switch (e)
            {
                case Elemento.Fuego: return new Color(1f, 0.55f, 0.12f);
                case Elemento.Hielo: return new Color(0.55f, 0.85f, 1f);
                case Elemento.Rayo: return new Color(1f, 0.95f, 0.35f);
                case Elemento.Veneno: return new Color(0.6f, 0.9f, 0.25f);
                default: return porDefecto;
            }
        }

        private static Sprite SpriteElem(Elemento e)
        {
            switch (e)
            {
                case Elemento.Fuego: return Arte.Llama;
                case Elemento.Veneno: return Arte.Burbuja;
                case Elemento.Hielo: return Arte.Estrella;
                case Elemento.Rayo: return Arte.Chispa;
                default: return Arte.DiscoSuave;
            }
        }

        private static string SonidoElem(Elemento e)
        {
            switch (e)
            {
                case Elemento.Fuego: return "fuego";
                case Elemento.Hielo: return "hielo";
                case Elemento.Rayo: return "rayo";
                case Elemento.Veneno: return "veneno";
                default: return "magia";
            }
        }

        private void PonerIsla(int w, int h)
        {
            float k = (w + h) * 0.5f;
            Vector2 c = AMundo((w - 1) * 0.5f, (h - 1) * 0.5f);
            _isla.transform.position = new Vector3(c.x, c.y - 0.42f, 0f);
            _isla.transform.localScale = new Vector3(k * 1.04f, k * 1.04f, 1f);
            _isla.color = new Color(0.1f, 0.06f, 0.14f, 0.55f);
            _isla2.transform.position = new Vector3(c.x, c.y - 0.85f, 0f);
            _isla2.transform.localScale = new Vector3(k * 0.8f, k * 0.8f, 1f);
            _isla2.color = new Color(0.1f, 0.06f, 0.14f, 0.35f);
        }

        /// <summary>Monta el tablero de un combate y lanza la animación de entrada.</summary>
        public void CargarNivel(Combate c)
        {
            LimpiarEscena();
            _modoCamp = false;
            C = c;
            _W = c.Tab.Ancho; _H = c.Tab.Alto;
            PonerMundo(c.NumMundo);
            PonerIsla(_W, _H);
            _tiles = new VC[_W, _H];
            for (int x = 0; x < _W; x++)
                for (int y = 0; y < _H; y++)
                    _tiles[x, y] = NuevaCasilla(x, y, c.Tab.TerrenoEn(new Pos(x, y)), (x + y) * 0.04f + Random.Range(0f, 0.05f));
            foreach (Unidad u in c.Unidades)
            {
                VU v = CrearVU(u, false);
                v.Aparicion = 0f;
            }
            Sincronizar();
            foreach (VU v in Unidades) { v.Visible = false; }
            StartCoroutine(Entrada());
        }

        /// <summary>Plataforma pequeña con los gatos (campamento, pantalla de inicio...).</summary>
        public void CargarCampamento(int mundo, List<Gato> gatos)
        {
            LimpiarEscena();
            _modoCamp = true;
            _W = 5; _H = 5;
            PonerMundo(mundo);
            PonerIsla(_W, _H);
            _tiles = new VC[_W, _H];
            for (int x = 0; x < _W; x++)
                for (int y = 0; y < _H; y++)
                {
                    int h = x * 7 + y * 13;
                    Terreno t = (h % 11 == 0) ? Terreno.Flores : ((h % 7 == 0) ? Terreno.HierbaAlta : Terreno.Suelo);
                    _tiles[x, y] = NuevaCasilla(x, y, t, (x + y) * 0.05f);
                }
            // los gatos en la diagonal "horizontal" de la pantalla
            for (int i = 0; i < gatos.Count && i < 5; i++)
            {
                var u = new Unidad { Nombre = gatos[i].Nombre, Bando = Bando.Gatos, Gato = gatos[i], Vida = gatos[i].Vida, VidaMax = gatos[i].VidaMax };
                u.Pos = new Pos(i, 4 - i);
                if (gatos.Count == 4) u.Pos = new Pos(i, 3 - i + 1);
                VU v = CrearVU(u, true);
                v.Salta = true;
                v.Fase = i * 1.3f;
                v.SrBFondo.gameObject.SetActive(false); v.SrBRetraso.gameObject.SetActive(false); v.SrBVida.gameObject.SetActive(false);
                v.SrBMana.gameObject.SetActive(false); v.SrBManaFondo.gameObject.SetActive(false);
            }
            foreach (VC t in _tiles) t.Edad = 0f;
        }

        private VU CrearVU(Unidad u, bool visible)
        {
            var v = new VU { U = u, Visible = visible, Fase = Random.Range(0f, 6.28f) };
            v.Gx = u.Pos.X; v.Gy = u.Pos.Y;
            v.Altura = (u.Def != null && u.Def.EsJefe) ? 1.18f : 0.8f;
            v.MiraIzq = !u.EsGato;
            v.Go = new GameObject("u " + u.Nombre);
            v.Go.transform.parent = _raiz;
            var cuerpo = new GameObject("cuerpo");
            cuerpo.transform.parent = v.Go.transform;
            v.Cuerpo = cuerpo.transform;
            v.SrCuerpo = cuerpo.AddComponent<SpriteRenderer>();
            v.SrCuerpo.sprite = Arte.DeUnidad(u);
            v.SrSombra = Hijo(v.Go, "sombra", 0); v.SrSombra.sprite = Arte.Sombra;
            v.SrAnillo = Hijo(v.Go, "anillo", 0); v.SrAnillo.sprite = Arte.Anillo; v.SrAnillo.color = new Color(1f, 0.85f, 0.3f, 0f);
            v.SrAnillo.transform.localScale = new Vector3(0.75f, 0.375f, 1f);
            v.SrBFondo = Hijo(v.Go, "bfondo", 3000); v.SrBFondo.sprite = Arte.Barra; v.SrBFondo.color = new Color(0.1f, 0.05f, 0.12f, 0.95f);
            v.SrBRetraso = Hijo(v.Go, "bretraso", 3001); v.SrBRetraso.sprite = Arte.Barra; v.SrBRetraso.color = new Color(1f, 1f, 1f, 0.9f);
            v.SrBVida = Hijo(v.Go, "bvida", 3002); v.SrBVida.sprite = Arte.Barra;
            v.SrBManaFondo = Hijo(v.Go, "bmfondo", 3000); v.SrBManaFondo.sprite = Arte.Barra; v.SrBManaFondo.color = new Color(0.1f, 0.05f, 0.12f, 0.95f);
            v.SrBMana = Hijo(v.Go, "bmana", 3002); v.SrBMana.sprite = Arte.Barra; v.SrBMana.color = new Color(0.35f, 0.7f, 1f, 1f);
            v.SrBManaFondo.gameObject.SetActive(u.EsGato); v.SrBMana.gameObject.SetActive(u.EsGato);
            v.VidaVis = v.VidaRetraso = u.Vida; v.VidaObj = u.Vida;
            Unidades.Add(v);
            v.Go.SetActive(visible);
            return v;
        }

        // ================================================================ sincronizar con el estado real
        public void Sincronizar()
        {
            if (C == null) return;
            foreach (Unidad u in C.Unidades)
            {
                VU v = Vis(u, false);
                if (v == null) v = CrearVU(u, true);
                v.Gx = u.Pos.X; v.Gy = u.Pos.Y;
                v.Alto = 0f; v.OffX = 0f; v.OffY = 0f; v.Sx = 1f; v.Sy = 1f; v.Rot = 0f; v.Tinte = Color.white; v.Alfa = 1f; v.Aparicion = 1f;
                v.VidaObj = u.Vida;
                v.Muerto = !u.Vivo;
                v.Visible = u.Vivo;
                if (!u.Vivo) { v.VidaVis = 0f; v.VidaRetraso = 0f; }
            }
            if (_tiles != null)
                for (int x = 0; x < _W; x++)
                    for (int y = 0; y < _H; y++)
                    {
                        VC c = _tiles[x, y];
                        Pos p = new Pos(x, y);
                        Terreno t = C.Tab.TerrenoEn(p);
                        if (t != c.T) PonerCasilla(c, t);
                        PonerMonedas(c, C.Tab.MonedasEn(p));
                        Zona z = C.Tab.ZonaEn(p);
                        c.TieneZona = z != null;
                        if (z != null) c.ZT = z.Tipo;
                    }
            ActivoVisual = C.Estado == EstadoCombate.EnCurso ? C.Activa : null;
        }

        /// <summary>Descarta las animaciones pendientes y deja todo en el estado final (para "saltar").</summary>
        public void Saltar()
        {
            StopAllCoroutines();
            _cola.Clear(); _reproduciendo = false;
            if (_entrando) { _entrando = false; if (_tiles != null) foreach (VC c in _tiles) c.Edad = 99f; }
            BorrarTemporales();
            Sincronizar();
        }

        // ================================================================ entrada del nivel
        private IEnumerator Entrada()
        {
            _entrando = true;
            Sonido.Tocar("carta", 0.6f, 0.8f);
            yield return StartCoroutine(Esp(0.9f));
            // las unidades caen del cielo
            var orden = new List<VU>(Unidades);
            orden.Sort(delegate (VU a, VU b) { return a.Gx.CompareTo(b.Gx); });
            for (int i = 0; i < orden.Count; i++)
            {
                VU v = orden[i];
                if (!v.U.Vivo) continue;
                StartCoroutine(Caer(v));
                yield return StartCoroutine(Esp(0.09f));
            }
            yield return StartCoroutine(Esp(0.55f));
            _entrando = false;
            Sincronizar();
        }

        private IEnumerator Caer(VU v)
        {
            v.Visible = true; v.Go.SetActive(true); v.Aparicion = 1f;
            yield return StartCoroutine(Anim(0.38f, delegate (float u) { v.Alto = (1f - u * u) * 3.2f; v.Alfa = Mathf.Clamp01(u * 4f); }));
            v.Alto = 0f; v.Alfa = 1f;
            Polvo(AMundo(v.Gx, v.Gy), 6);
            Sonido.Tocar("paso", 0.8f, Random.Range(0.8f, 1.1f));
            Fx.Sacudir(0.03f);
            yield return StartCoroutine(Anim(0.22f, delegate (float u) { float s = Mathf.Sin(u * Mathf.PI); v.Sx = 1f + 0.22f * s; v.Sy = 1f - 0.28f * s; }));
            v.Sx = 1f; v.Sy = 1f;
        }

        // ================================================================ marcas del turno del jugador
        public void Marcar(List<Pos> mover, List<Pos> objetivos, List<Pos> area, Pos? hover, List<Pos> camino)
        {
            _mMover.Clear(); _mObj.Clear(); _mArea.Clear();
            if (mover != null) foreach (Pos p in mover) _mMover.Add(p);
            if (objetivos != null) foreach (Pos p in objetivos) _mObj.Add(p);
            if (area != null) foreach (Pos p in area) _mArea.Add(p);
            _hover = hover;
            _camino = camino ?? new List<Pos>();
        }

        public void SinMarcas() { Marcar(null, null, null, null, null); MarcarInspeccion(null, null); }

        /// <summary>Casillas a las que puede ir la unidad inspeccionada (naranja) y las que podría atacar después de moverse (rojo suave).</summary>
        public void MarcarInspeccion(List<Pos> mover, List<Pos> amenaza)
        {
            _mInsp.Clear(); _mAmen.Clear();
            if (mover != null) foreach (Pos p in mover) _mInsp.Add(p);
            if (amenaza != null) foreach (Pos p in amenaza) _mAmen.Add(p);
        }

        // ================================================================ actualización por fotograma
        private void Update()
        {
            float dt = Time.deltaTime;
            _t += dt;
            if (Cam == null) return;

            // cámara
            float asp = Mathf.Max(0.5f, (float)Screen.width / Mathf.Max(1, Screen.height));
            float k = _modoCamp ? 1f : Mathf.Max(0.85f, (_W + _H) / 18f);   // tableros más grandes o chicos que 9x9
            float tamH = (_modoCamp ? 3.4f : 4.55f) * k;
            float tamW = (_modoCamp ? 7.6f : 7.4f) * k / asp;
            Cam.orthographicSize = Mathf.Max(tamH, tamW);
            _foco = Vector2.Lerp(_foco, _focoObj, 1f - Mathf.Exp(-3f * dt));
            Vector2 sh = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * Fx.Temblor;
            float yBase = _modoCamp ? -0.15f : -0.12f;
            Cam.transform.position = new Vector3(_foco.x + sh.x, yBase + _foco.y + sh.y, -10f);

            // fondo
            float alto = Cam.orthographicSize * 2f + 1f, ancho = alto * asp + 1f;
            _fondo.transform.position = new Vector3(Cam.transform.position.x, Cam.transform.position.y, 0f);
            _fondo.transform.localScale = new Vector3(ancho / 0.04f, alto / 0.64f, 1f);
            for (int i = 0; i < _nubes.Count; i++)
            {
                SpriteRenderer n = _nubes[i];
                Vector3 p = n.transform.position;
                p.x += dt * (0.12f + 0.05f * i);
                if (p.x > ancho * 0.5f + 3f) p.x = -ancho * 0.5f - 3f;
                n.transform.position = p;
            }

            if (_flash > 0f) _flash = Mathf.Max(0f, _flash - dt * 2.2f);
            if (BannerT > 0f) BannerT -= dt;

            Ambiente(dt);
            ActualizarCasillas(dt);
            ActualizarUnidades(dt);
            ActualizarPuntos();
            Fx.Actualizar(dt);
        }

        private void Ambiente(float dt)
        {
            _ambT -= dt;
            if (_ambT > 0f) return;
            float ancho = Cam.orthographicSize * 2f * Mathf.Max(0.5f, (float)Screen.width / Mathf.Max(1, Screen.height));
            Vector2 c = Cam.transform.position;
            switch (_mundo)
            {
                case 2: // hojas
                    _ambT = 0.35f;
                    Fx.Una(Arte.Hoja, new Vector2(c.x + Random.Range(-ancho / 2, ancho / 2), c.y + Cam.orthographicSize + 0.3f), new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(-0.7f, -0.4f)), 7f, 0.16f, 0.16f, new Color(0.5f, 0.8f, 0.3f, 0.9f), new Color(0.8f, 0.8f, 0.3f, 0.5f), 0f, Random.Range(0f, 360f), Random.Range(-90f, 90f), 2100, 1f);
                    break;
                case 3: // burbujas
                    _ambT = 0.3f;
                    Fx.Una(Arte.Burbuja, new Vector2(c.x + Random.Range(-ancho / 2, ancho / 2), c.y - Cam.orthographicSize * 0.6f), new Vector2(0f, Random.Range(0.3f, 0.7f)), 5f, 0.14f, 0.3f, new Color(1, 1, 1, 0.55f), new Color(1, 1, 1, 0f), 0f, 0f, 0f, 2100, 1f);
                    break;
                case 4: // polvo en suspensión
                    _ambT = 0.25f;
                    Fx.Una(Arte.DiscoSuave, new Vector2(c.x + Random.Range(-ancho / 2, ancho / 2), c.y + Random.Range(-2f, 3f)), new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(0.05f, 0.2f)), 4f, 0.1f, 0.25f, new Color(0.8f, 0.8f, 1f, 0.5f), new Color(0.8f, 0.8f, 1f, 0f), 0f, 0f, 0f, 2100, 1f);
                    break;
                case 5: // arena al viento
                    _ambT = 0.12f;
                    Fx.Una(Arte.DiscoSuave, new Vector2(c.x - ancho / 2 - 0.3f, c.y + Random.Range(-3f, 3f)), new Vector2(Random.Range(2f, 3.5f), Random.Range(-0.15f, 0.15f)), 6f, 0.14f, 0.2f, new Color(0.95f, 0.8f, 0.5f, 0.45f), new Color(0.95f, 0.8f, 0.5f, 0f), 0f, 0f, 0f, 2100, 1f);
                    break;
                case 6: // nieve
                    _ambT = 0.06f;
                    Fx.Una(Arte.Disco, new Vector2(c.x + Random.Range(-ancho / 2, ancho / 2), c.y + Cam.orthographicSize + 0.2f), new Vector2(Random.Range(-0.3f, 0.1f), Random.Range(-1.0f, -0.6f)), 8f, 0.07f, 0.07f, new Color(1, 1, 1, 0.9f), new Color(1, 1, 1, 0.6f), 0f, 0f, 0f, 2100, 1f);
                    break;
                case 7: // destellos
                    _ambT = 0.15f;
                    Fx.Una(Arte.Chispa, new Vector2(c.x + Random.Range(-ancho / 2, ancho / 2), c.y + Random.Range(-3.5f, 3.5f)), new Vector2(0f, 0.1f), 1.6f, 0.05f, 0.28f, new Color(1f, 0.7f, 1f, 0f), new Color(0.8f, 0.6f, 1f, 0f), 0f, 0f, 90f, 2100, 1f);
                    Fx.Una(Arte.Chispa, new Vector2(c.x + Random.Range(-ancho / 2, ancho / 2), c.y + Random.Range(-3.5f, 3.5f)), new Vector2(0f, 0.15f), 1.8f, 0.2f, 0.02f, new Color(1f, 0.85f, 1f, 0.9f), new Color(0.7f, 0.5f, 1f, 0f), 0f, 0f, 120f, 2100, 1f);
                    break;
                default:
                    _ambT = 1f;
                    break;
            }
        }

        private static float AtrasSuave(float u)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float x = u - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        private void ActualizarCasillas(float dt)
        {
            if (_tiles == null) return;
            float pulso = 0.5f + 0.5f * Mathf.Sin(_t * 5f);
            for (int x = 0; x < _W; x++)
                for (int y = 0; y < _H; y++)
                {
                    VC c = _tiles[x, y];
                    c.Edad += dt;
                    float u = Mathf.Clamp01((c.Edad - c.Retardo) / 0.45f);
                    float ent = AtrasSuave(u);
                    float dy = (1f - ent) * 1.3f;
                    float pop = 0f;
                    if (c.PopT < 1f) { c.PopT += dt / 0.35f; pop = Mathf.Sin(Mathf.Clamp01(c.PopT) * Mathf.PI) * 0.1f; }
                    Vector2 w = AMundo(x, y);
                    c.Go.transform.position = new Vector3(w.x, w.y + dy + pop, 0f);
                    float al = Mathf.Clamp01(u * 3f);
                    c.Go.SetActive(c.Edad >= c.Retardo);
                    float br = c.T == Terreno.Agua ? 0.94f + 0.06f * Mathf.Sin(_t * 2f + x * 0.8f + y * 0.6f) : 1f;
                    c.Base.color = new Color(br, br, br, al);
                    c.Bloque.color = new Color(1, 1, 1, al);
                    if (c.Moneda.gameObject.activeSelf)
                    {
                        c.Moneda.color = new Color(1, 1, 1, al);
                        c.Moneda.transform.localPosition = new Vector3(0f, 0.04f + Mathf.Abs(Mathf.Sin(_t * 2.4f + x + y)) * 0.05f, 0f);
                    }
                    c.Mata.color = new Color(1, 1, 1, al);
                    if (c.Mata.gameObject.activeSelf) c.Mata.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_t * 1.8f + x * 0.7f + y * 0.4f) * 1.2f);

                    // marcas
                    Pos p = new Pos(x, y);
                    Color fill = new Color(1, 1, 1, 0), borde = new Color(1, 1, 1, 0);
                    if (_mAmen.Contains(p)) { fill = new Color(0.9f, 0.2f, 0.3f, 0.16f + 0.06f * pulso); borde = new Color(1f, 0.4f, 0.45f, 0.45f); }
                    if (_mInsp.Contains(p)) { fill = new Color(1f, 0.62f, 0.15f, 0.3f + 0.1f * pulso); borde = new Color(1f, 0.82f, 0.35f, 0.8f); }
                    if (_mMover.Contains(p)) { fill = new Color(0.35f, 0.65f, 1f, 0.22f + 0.1f * pulso); borde = new Color(0.6f, 0.85f, 1f, 0.55f); }
                    if (_mObj.Contains(p)) { fill = new Color(1f, 0.25f, 0.25f, 0.3f + 0.15f * pulso); borde = new Color(1f, 0.6f, 0.5f, 0.9f); }
                    if (_mArea.Contains(p)) { fill = new Color(1f, 0.6f, 0.1f, 0.5f + 0.2f * pulso); borde = new Color(1f, 0.9f, 0.4f, 1f); }
                    if (_hover.HasValue && _hover.Value == p) borde = new Color(1f, 1f, 1f, 0.95f);
                    c.Relleno.color = fill;
                    c.Borde.color = borde;

                    // zonas
                    if (c.TieneZona)
                    {
                        Color zc = c.ZT == ZonaTipo.Fuego ? new Color(1f, 0.45f, 0.1f, 0.45f + 0.2f * pulso)
                                 : (c.ZT == ZonaTipo.Enredadera ? new Color(0.2f, 0.7f, 0.25f, 0.55f)
                                 : (c.ZT == ZonaTipo.Veneno ? new Color(0.55f, 0.85f, 0.15f, 0.45f + 0.15f * pulso) : new Color(0.65f, 0.3f, 0.95f, 0.4f + 0.15f * pulso)));
                        c.Zona.color = zc;
                        c.ProxFx -= dt;
                        if (c.ProxFx <= 0f)
                        {
                            c.ProxFx = Random.Range(0.12f, 0.3f);
                            Vector2 q = w + new Vector2(Random.Range(-0.35f, 0.35f), Random.Range(-0.12f, 0.12f));
                            int o = Ord(x, y) + 8;
                            if (c.ZT == ZonaTipo.Fuego) Fx.Una(Arte.Llama, q, new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(0.5f, 0.9f)), 0.6f, 0.26f, 0.05f, new Color(1f, 0.8f, 0.2f, 0.95f), new Color(1f, 0.2f, 0.05f, 0f), 0f, 0f, 0f, o, 1f);
                            else if (c.ZT == ZonaTipo.Veneno) Fx.Una(Arte.Burbuja, q, new Vector2(0f, Random.Range(0.25f, 0.5f)), 0.9f, 0.1f, 0.2f, new Color(0.6f, 0.95f, 0.25f, 0.85f), new Color(0.6f, 0.95f, 0.25f, 0f), 0f, 0f, 0f, o, 1f);
                            else if (c.ZT == ZonaTipo.Frasco) Fx.Una(Arte.Burbuja, q, new Vector2(0f, Random.Range(0.3f, 0.6f)), 0.9f, 0.1f, 0.2f, new Color(0.9f, 0.6f, 1f, 0.8f), new Color(0.9f, 0.6f, 1f, 0f), 0f, 0f, 0f, o, 1f);
                            else Fx.Una(Arte.Hoja, q, new Vector2(0f, Random.Range(0.15f, 0.35f)), 1.0f, 0.12f, 0.12f, new Color(0.3f, 0.85f, 0.35f, 0.9f), new Color(0.3f, 0.85f, 0.35f, 0f), 0f, Random.Range(0f, 360f), 40f, o, 1f);
                        }
                    }
                    else c.Zona.color = new Color(1, 1, 1, 0);
                }
        }

        private void ActualizarPuntos()
        {
            for (int i = 0; i < _puntos.Count; i++)
            {
                if (i < _camino.Count)
                {
                    Pos p = _camino[i];
                    Vector2 w = AMundo(p);
                    float k = (i == _camino.Count - 1) ? 0.3f : 0.17f;
                    _puntos[i].gameObject.SetActive(true);
                    _puntos[i].transform.position = new Vector3(w.x, w.y, 0f);
                    _puntos[i].transform.localScale = new Vector3(k / 0.32f, k / 0.32f * 0.5f, 1f);
                    _puntos[i].sortingOrder = Ord(p.X, p.Y) + 8;
                    _puntos[i].color = new Color(1f, 1f, 1f, 0.85f);
                }
                else _puntos[i].gameObject.SetActive(false);
            }
        }

        private void ActualizarUnidades(float dt)
        {
            for (int i = 0; i < Unidades.Count; i++)
            {
                VU v = Unidades[i];
                if (!v.Visible) { if (v.Go.activeSelf) v.Go.SetActive(false); continue; }
                if (!v.Go.activeSelf) v.Go.SetActive(true);
                Vector2 w = AMundo(v.Gx, v.Gy);
                v.Go.transform.position = new Vector3(w.x + v.OffX, w.y, 0f);
                int o = Ord(v.Gx, v.Gy);
                float bob = Mathf.Sin(_t * 2.6f + v.Fase) * 0.012f;
                float resp = 1f + Mathf.Sin(_t * 2.6f + v.Fase) * 0.012f;
                float saltito = 0f;
                if (v.Salta)
                {
                    bool sel = CampSel != null && v.U.Gato == CampSel;
                    float ph = (_t * (sel ? 2.4f : 0.7f) + v.Fase) % 1f;
                    if (sel || ph < 0.2f) saltito = Mathf.Abs(Mathf.Sin(ph * Mathf.PI * (sel ? 1f : 5f))) * (sel ? 0.22f : 0.1f);
                }
                v.Cuerpo.localPosition = new Vector3(0f, v.Alto + v.OffY + bob + saltito, 0f);
                v.Cuerpo.localScale = new Vector3(v.Sx * v.Aparicion * (v.MiraIzq ? -1f : 1f) * resp, v.Sy * v.Aparicion / resp, 1f);
                v.Cuerpo.localRotation = Quaternion.Euler(0f, 0f, v.Rot);
                v.SrCuerpo.sortingOrder = o + 5;
                Color tc = v.Tinte; tc.a = v.Alfa;
                if (!_modoCamp && v.U.Vivo && v.U.Tiene(EstadoTipo.Ralentizado))
                {
                    float az = 0.5f + 0.5f * Mathf.Sin(_t * 3f + v.Fase);
                    tc.r *= 0.45f + 0.1f * az; tc.g *= 0.7f + 0.08f * az; tc.b *= 1f;     // ralentizado: el sprite se vuelve azul
                }
                v.SrCuerpo.color = tc;

                float alt = v.Alto + saltito;
                v.SrSombra.sortingOrder = o + 2;
                float ks = Mathf.Max(0.3f, 1f - alt * 0.3f) * (v.Altura > 1f ? 1.7f : 1.05f) * v.Aparicion;
                v.SrSombra.transform.localScale = new Vector3(ks, ks, 1f);
                v.SrSombra.color = new Color(1, 1, 1, v.Alfa * 0.9f);

                bool activo = (ActivoVisual == v.U && !_modoCamp) || (_modoCamp && CampSel != null && v.U.Gato == CampSel);
                v.SrAnillo.sortingOrder = o + 3;
                float aa = activo ? 0.65f + 0.3f * Mathf.Sin(_t * 6f) : 0f;
                v.SrAnillo.color = new Color(1f, 0.85f, 0.3f, aa * v.Alfa);
                float ka = (v.Altura > 1f ? 1.0f : 0.75f) * (1f + 0.04f * Mathf.Sin(_t * 6f));
                v.SrAnillo.transform.localScale = new Vector3(ka, ka * 0.5f, 1f);

                // barras de vida / maná
                if (!_modoCamp)
                {
                    v.VidaVis = Mathf.MoveTowards(v.VidaVis, v.VidaObj, dt * Mathf.Max(6f, v.U.VidaMax * 2.5f) * Mathf.Max(1f, Vel));
                    v.VidaRetraso = Mathf.MoveTowards(v.VidaRetraso, v.VidaVis, dt * Mathf.Max(3f, v.U.VidaMax * 0.9f));
                    if (v.VidaRetraso < v.VidaVis) v.VidaRetraso = v.VidaVis;
                    float max = Mathf.Max(1, v.U.VidaMax);
                    float f = Mathf.Clamp01(v.VidaVis / max), fr = Mathf.Clamp01(v.VidaRetraso / max);
                    float ancho = v.Altura > 1f ? 0.9f : 0.6f;
                    float by = v.Altura + 0.12f;
                    float kx = ancho / 0.16f;
                    PonerBarra(v.SrBFondo, -ancho / 2 - 0.02f, by, (ancho + 0.04f) / 0.16f, 2.2f);
                    PonerBarra(v.SrBRetraso, -ancho / 2, by, kx * fr, 1.5f);
                    PonerBarra(v.SrBVida, -ancho / 2, by, kx * f, 1.5f);
                    v.SrBVida.color = f > 0.5f ? Color.Lerp(new Color(0.95f, 0.8f, 0.2f), new Color(0.4f, 0.85f, 0.3f), (f - 0.5f) * 2f) : Color.Lerp(new Color(0.95f, 0.25f, 0.2f), new Color(0.95f, 0.8f, 0.2f), f * 2f);
                    if (!v.U.EsGato) v.SrBVida.color = Color.Lerp(new Color(0.85f, 0.15f, 0.3f), new Color(1f, 0.45f, 0.5f), f);
                    if (v.U.EsGato)
                    {
                        float fm = v.U.ManaMax > 0 ? Mathf.Clamp01((float)v.U.Mana / v.U.ManaMax) : 0f;
                        PonerBarra(v.SrBManaFondo, -ancho / 2 - 0.02f, by - 0.075f, (ancho + 0.04f) / 0.16f, 1.2f);
                        PonerBarra(v.SrBMana, -ancho / 2, by - 0.075f, kx * fm, 0.8f);
                    }
                    float ba = v.Alfa * v.Aparicion;
                    v.SrBFondo.enabled = ba > 0.5f; v.SrBRetraso.enabled = ba > 0.5f; v.SrBVida.enabled = ba > 0.5f;
                }
            }
        }

        private static void PonerBarra(SpriteRenderer sr, float x, float y, float kx, float ky)
        {
            sr.transform.localPosition = new Vector3(x, y, 0f);
            sr.transform.localScale = new Vector3(Mathf.Max(0.0001f, kx), ky, 1f);
        }

        // ================================================================ utilidades de animación
        public IEnumerator Esp(float s)
        {
            float t = 0f;
            while (t < s) { t += Time.deltaTime * Vel; yield return null; }
        }

        public IEnumerator Anim(float dur, Action<float> f)
        {
            float t = 0f;
            while (t < dur) { t += Time.deltaTime * Vel; f(Mathf.Clamp01(t / dur)); yield return null; }
            f(1f);
        }

        private void Polvo(Vector2 pos, int n)
        {
            Fx.Chorro(Arte.DiscoSuave, pos + new Vector2(0f, 0.05f), n, 0.25f, 0.8f, 0.45f, 0.26f, new Color(0.95f, 0.92f, 0.85f, 0.7f), new Color(0.95f, 0.92f, 0.85f, 0f), -0.2f, 4000);
        }

        private Color ColorDe(Unidad u)
        {
            if (u == null) return Color.white;
            Col c = Col.Hex(u.ColorHex);
            return new Color(c.R, c.G, c.B, 1f);
        }

        private void Mirar(VU v, Pos hacia)
        {
            float dx = (hacia.X - v.Gx) - (hacia.Y - v.Gy);
            if (Mathf.Abs(dx) > 0.01f) v.MiraIzq = dx < 0f;
        }

        public void Anunciar(string txt, Color col) { Banner = txt; BannerCol = col; BannerT = BannerDur; }

        // ================================================================ reproductor de eventos
        public void Encolar(List<Evento> evs)
        {
            if (evs == null || evs.Count == 0) return;
            foreach (Evento e in evs) _cola.Enqueue(e);
            if (!_reproduciendo) StartCoroutine(Bucle());
        }

        private IEnumerator Bucle()
        {
            _reproduciendo = true;
            while (_entrando) yield return null;
            while (_cola.Count > 0)
            {
                Evento e = _cola.Dequeue();
                yield return StartCoroutine(Ejecutar(e));
            }
            _reproduciendo = false;
            Sincronizar();
        }

        private IEnumerator Ejecutar(Evento e)
        {
            switch (e.Tipo)
            {
                case EvTipo.Turno: return EvTurno(e);
                case EvTipo.Mover: return EvMover(e);
                case EvTipo.Habilidad: return EvHabilidad(e);
                case EvTipo.Consumible: return EvConsumible(e);
                case EvTipo.Dano: return EvDano(e);
                case EvTipo.Esquiva: return EvEsquiva(e);
                case EvTipo.Cura: return EvCura(e);
                case EvTipo.Muerte: return EvMuerte(e);
                case EvTipo.Empuje: return EvEmpuje(e);
                case EvTipo.Salto: return EvSalto(e);
                case EvTipo.Invocar: return EvInvocar(e);
                case EvTipo.Zona: return EvZona(e);
                case EvTipo.Terreno: return EvTerreno(e);
                case EvTipo.Texto: return EvTexto(e);
                case EvTipo.Tormenta: return EvTormenta(e);
                case EvTipo.Teletransporte: return EvTeletransporte(e);
                case EvTipo.Embestida: return EvEmbestida(e);
                case EvTipo.Moneda: return EvMoneda(e);
                case EvTipo.Recoger: return EvRecoger(e);
                case EvTipo.Romper: return EvRomper(e);
                case EvTipo.Explosion: return EvExplosion(e);
                case EvTipo.Choque: return EvChoque(e);
                case EvTipo.Fin: return EvFin(e);
            }
            return Esp(0f);
        }

        // ---------------------------------------------------------------- turno
        private IEnumerator EvTurno(Evento e)
        {
            ActivoVisual = e.U;
            VU v = Vis(e.U, true);
            if (v != null && v.Visible)
            {
                _focoObj = (AMundo(v.Gx, v.Gy) - Vector2.zero) * 0.18f;
                if (e.U.EsGato)
                {
                    Anunciar("¡Turno de " + e.U.Nombre + "!", new Color(1f, 0.92f, 0.55f));
                    Sonido.Tocar("turno");
                }
                Fx.Anillo(AMundo(v.Gx, v.Gy), 0.1f, 0.6f, 0.4f, e.U.EsGato ? new Color(1f, 0.9f, 0.4f, 0.9f) : new Color(1f, 0.4f, 0.4f, 0.8f), Ord(v.Gx, v.Gy) + 4);
                StartCoroutine(Anim(0.3f, delegate (float u) { float s = Mathf.Sin(u * Mathf.PI); v.Alto = s * 0.16f; v.Sx = 1f - 0.06f * s; v.Sy = 1f + 0.1f * s; }));
            }
            yield return StartCoroutine(Esp(e.U != null && e.U.EsGato ? 0.35f : 0.2f));
        }

        // ---------------------------------------------------------------- movimiento (salto de casilla en casilla)
        private IEnumerator EvMover(Evento e)
        {
            VU v = Vis(e.U, true);
            if (v == null) yield break;
            v.Visible = true;
            var ruta = new List<Pos>();
            ruta.Add(e.A);
            if (e.Camino != null) ruta.AddRange(e.Camino);
            int pasos = ruta.Count - 1;
            float dur = Mathf.Clamp(0.17f - 0.006f * pasos, 0.09f, 0.17f);
            for (int i = 1; i < ruta.Count; i++)
            {
                Pos desde = ruta[i - 1], hasta = ruta[i];
                Mirar(v, hasta);
                float fx = desde.X, fy = desde.Y, tx = hasta.X, ty = hasta.Y;
                Terreno terrenoDestino = _tiles != null ? _tiles[hasta.X, hasta.Y].T : Terreno.Suelo;
                // agacharse un instante antes de saltar
                yield return StartCoroutine(Anim(0.035f, delegate (float u) { v.Sy = 1f - 0.2f * u; v.Sx = 1f + 0.12f * u; }));
                Sonido.Tocar("salto", 0.35f, Random.Range(0.9f, 1.15f));
                yield return StartCoroutine(Anim(dur, delegate (float u)
                {
                    v.Gx = Mathf.Lerp(fx, tx, u); v.Gy = Mathf.Lerp(fy, ty, u);
                    v.Alto = Mathf.Sin(u * Mathf.PI) * 0.26f;
                    float est = Mathf.Sin(u * Mathf.PI);
                    v.Sy = 1f + 0.16f * est; v.Sx = 1f - 0.1f * est;
                }));
                v.Gx = tx; v.Gy = ty; v.Alto = 0f;
                Vector2 w = AMundo(tx, ty);
                if (terrenoDestino == Terreno.Barro)
                {
                    Fx.Chorro(Arte.Disco, w, 6, 0.3f, 0.9f, 0.5f, 0.1f, new Color(0.4f, 0.28f, 0.18f, 1f), new Color(0.4f, 0.28f, 0.18f, 0f), -2.5f, Ord(tx, ty) + 8);
                    Sonido.Tocar("barro", 0.6f, Random.Range(0.9f, 1.1f));
                }
                else if (terrenoDestino == Terreno.HierbaAlta)
                {
                    Fx.Chorro(Arte.Hoja, w + new Vector2(0f, 0.1f), 5, 0.3f, 0.9f, 0.6f, 0.13f, new Color(0.45f, 0.8f, 0.3f, 1f), new Color(0.45f, 0.8f, 0.3f, 0f), -1.8f, Ord(tx, ty) + 8);
                    Sonido.Tocar("hierba", 0.5f, Random.Range(0.9f, 1.2f));
                }
                else
                {
                    Polvo(w, 3);
                    Sonido.Tocar("paso", 0.45f, Random.Range(0.9f, 1.2f));
                }
                StartCoroutine(Anim(0.1f, delegate (float u) { float s = Mathf.Sin(u * Mathf.PI); v.Sy = 1f - 0.2f * s; v.Sx = 1f + 0.14f * s; }));
            }
            v.Sx = 1f; v.Sy = 1f;
            yield return StartCoroutine(Esp(0.04f));
        }

        // ---------------------------------------------------------------- habilidades
        private IEnumerator Embestida(VU v, Pos objetivo, float fraccion)
        {
            Mirar(v, objetivo);
            float ax = v.Gx, ay = v.Gy;
            float bx = ax + (objetivo.X - ax) * fraccion, by = ay + (objetivo.Y - ay) * fraccion;
            yield return StartCoroutine(Anim(0.1f, delegate (float u) { v.Sx = 1f + 0.12f * u; v.Sy = 1f - 0.16f * u; v.Rot = (v.MiraIzq ? 1f : -1f) * -8f * u; }));
            yield return StartCoroutine(Anim(0.07f, delegate (float u) { v.Gx = Mathf.Lerp(ax, bx, u); v.Gy = Mathf.Lerp(ay, by, u); v.Sx = 1.12f - 0.1f * u; v.Sy = 0.84f + 0.14f * u; v.Rot = (v.MiraIzq ? 1f : -1f) * (-8f + 16f * u); }));
            StartCoroutine(Retorno(v, ax, ay));
        }

        private IEnumerator Retorno(VU v, float ax, float ay)
        {
            float bx = v.Gx, by = v.Gy;
            yield return StartCoroutine(Anim(0.16f, delegate (float u) { v.Gx = Mathf.Lerp(bx, ax, u); v.Gy = Mathf.Lerp(by, ay, u); v.Rot = Mathf.Lerp(v.Rot, 0f, u); v.Sx = Mathf.Lerp(v.Sx, 1f, u); v.Sy = Mathf.Lerp(v.Sy, 1f, u); }));
            v.Gx = ax; v.Gy = ay; v.Rot = 0f; v.Sx = 1f; v.Sy = 1f;
        }

        private IEnumerator Proyectil(Vector2 a, Vector2 b, Sprite s, float tam, Color col, float dur, float arco, bool rotar, bool estela, Color colEstela)
        {
            var go = new GameObject("proyectil");
            _temporales.Add(go);
            go.transform.parent = _raiz;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s; sr.color = col; sr.sortingOrder = 3500;
            float k = tam / s.bounds.size.x;
            go.transform.localScale = new Vector3(k, k, 1f);
            Vector2 prev = a;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime * Vel;
                float u = Mathf.Clamp01(t / dur);
                Vector2 p = Vector2.Lerp(a, b, u) + new Vector2(0f, Mathf.Sin(u * Mathf.PI) * arco);
                go.transform.position = new Vector3(p.x, p.y, 0f);
                if (rotar)
                {
                    Vector2 d = p - prev;
                    if (d.sqrMagnitude > 0.00001f) go.transform.eulerAngles = new Vector3(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                }
                if (estela) Fx.Una(Arte.DiscoSuave, p, new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(0.1f, 0.4f)), 0.35f, tam * 0.9f, tam * 0.15f, colEstela, new Color(colEstela.r, colEstela.g * 0.4f, colEstela.b * 0.2f, 0f), 0f, 3400);
                prev = p;
                yield return null;
            }
            _temporales.Remove(go);
            Destroy(go);
        }

        private void Chispas(Vector2 pos, Color c, int n)
        {
            Fx.Chorro(Arte.Chispa, pos, n, 0.8f, 2.2f, 0.45f, 0.22f, c, new Color(c.r, c.g, c.b, 0f), -2.5f, 3600);
        }

        private void Tajo(Vector2 pos, float tam, Color c, bool espejo)
        {
            Color fin = c; fin.a = 0f;
            Fx.Una(Arte.Tajo, pos + new Vector2(0f, 0.3f), Vector2.zero, 0.22f, tam * 0.6f, tam, c, fin, 0f, espejo ? 160f : 20f, espejo ? 60f : -60f, 3600, 1f);
        }

        private IEnumerator EvHabilidad(Evento e)
        {
            VU v = Vis(e.U, true);
            if (v == null || e.Hab == null) yield break;
            HabilidadDef h = e.Hab;
            Vector2 desde = AMundo(v.Gx, v.Gy) + new Vector2(0f, 0.4f);
            Vector2 hasta = AMundo(e.B);
            Mirar(v, e.B);
            Fx.Texto(h.Nombre, Cabeza(v) + new Vector2(0f, 0.1f), new Color(1f, 0.97f, 0.8f), 17f, 1.1f);
            Color cu = ColorDe(e.U);

            switch (h.Efecto)
            {
                case EfectoHab.Golpe:
                    if (h.RangoMax <= 1)
                    {
                        bool punalada = h.Id == "punalada";
                        yield return StartCoroutine(Embestida(v, e.B, punalada ? 0.75f : 0.6f));
                        Sonido.Tocar("golpe", 0.8f, punalada ? 1.3f : 1f);
                        Tajo(hasta, h.Id == "zarpazo" ? 1.3f : 1.0f, new Color(1f, 1f, 0.9f, 0.95f), false);
                        if (punalada) { yield return StartCoroutine(Esp(0.05f)); Tajo(hasta, 1.0f, new Color(1f, 0.9f, 0.9f, 0.95f), true); Sonido.Tocar("golpe", 0.6f, 1.5f); }
                        Chispas(hasta + new Vector2(0f, 0.3f), new Color(1f, 0.9f, 0.4f), 8);
                        Fx.Sacudir(h.Id == "zarpazo" ? 0.1f : 0.05f);
                        yield return StartCoroutine(Esp(0.06f));
                    }
                    else if (h.Id == "jefe6_atq" || h.Elemento == Elemento.Rayo)
                    {
                        yield return StartCoroutine(Esp(0.1f));
                        Sonido.Tocar("tormenta", 0.6f, 1.6f);
                        _flash = 0.5f;
                        StartCoroutine(Rayo(hasta));
                        yield return StartCoroutine(Esp(0.25f));
                        Fx.Sacudir(0.12f);
                    }
                    else
                    {
                        bool flecha = h.Id.Contains("flecha") || h.Id == "disparo" || h.Id == "arquero_atq" || h.Id == "cuchillos" || h.Id == "daga_veneno" || h.Id == "lazo";
                        Color ce = ColorElem(h.Elemento, cu);
                        yield return StartCoroutine(Anim(0.1f, delegate (float u) { v.Sx = 1f + 0.1f * u; v.Rot = (v.MiraIzq ? 1f : -1f) * -6f * u; }));
                        Sonido.Tocar(flecha ? "flecha" : "magia", 0.7f, 1f);
                        if (flecha) yield return StartCoroutine(Proyectil(desde, hasta + new Vector2(0f, 0.35f), Arte.Flecha, 0.55f, Color.Lerp(Color.white, ColorElem(h.Elemento, Color.white), 0.6f), 0.2f + 0.02f * Mathf.Abs(e.B.X - v.Gx), 0.2f, true, h.Elemento != Elemento.Fisico, ce));
                        else yield return StartCoroutine(Proyectil(desde, hasta + new Vector2(0f, 0.35f), h.Elemento == Elemento.Fisico ? Arte.DiscoSuave : SpriteElem(h.Elemento), 0.35f, ce, 0.3f, 0.4f, false, true, ce));
                        v.Rot = 0f; v.Sx = 1f;
                        Chispas(hasta + new Vector2(0f, 0.3f), h.Elemento != Elemento.Fisico ? ce : (flecha ? new Color(1f, 0.95f, 0.7f) : cu), 8);
                        Fx.Sacudir(0.04f);
                    }
                    break;

                case EfectoHab.AreaCruz:
                    {
                        Fx.Una(Arte.Runa, AMundo(v.Gx, v.Gy), Vector2.zero, 0.55f, 0.9f, 1.3f, new Color(1f, 0.6f, 0.2f, 0.9f), new Color(1f, 0.3f, 0.1f, 0f), 0f, 0f, 120f, Ord(v.Gx, v.Gy) + 3, 0.5f);
                        yield return StartCoroutine(Anim(0.22f, delegate (float u) { v.Alto = Mathf.Sin(u * Mathf.PI) * 0.12f; v.Sy = 1f + 0.08f * Mathf.Sin(u * Mathf.PI); }));
                        Sonido.Tocar("magia", 0.8f, 0.9f);
                        bool bola = e.U.EsGato;
                        Color cf = ColorElem(h.Elemento, bola ? new Color(1f, 0.6f, 0.15f) : new Color(1f, 0.4f, 0.9f));
                        Sprite spE = (h.Elemento == Elemento.Fisico || h.Elemento == Elemento.Fuego) ? Arte.Llama : SpriteElem(h.Elemento);
                        yield return StartCoroutine(Proyectil(desde, hasta + new Vector2(0f, 0.1f), spE, 0.55f, cf, 0.38f, 1.0f, false, true, cf));
                        Sonido.Tocar(h.Elemento != Elemento.Fisico ? SonidoElem(h.Elemento) : (bola ? "fuego" : "bomba"), 0.8f, bola ? 1f : 1.4f);
                        Fx.Sacudir(0.14f);
                        if (e.Area != null)
                        {
                            foreach (Pos p in e.Area)
                            {
                                Vector2 w = AMundo(p);
                                Fx.Chorro(spE, w + new Vector2(0f, 0.15f), p == e.B ? 12 : 7, 0.5f, 1.6f, 0.6f, 0.34f, new Color(1f, 0.85f, 0.3f, 1f), new Color(cf.r, cf.g * 0.3f, cf.b * 0.2f, 0f), 0.3f, 3600);
                                Fx.Destello(w + new Vector2(0f, 0.2f), 0.9f, 0.3f, new Color(1f, 0.9f, 0.6f, 0.9f), 3590);
                                Fx.Anillo(w, 0.1f, 0.65f, 0.4f, new Color(1f, 0.8f, 0.4f, 0.9f), Ord(p.X, p.Y) + 4);
                            }
                        }
                        yield return StartCoroutine(Esp(0.1f));
                        break;
                    }

                case EfectoHab.Curar:
                    {
                        Fx.Una(Arte.Runa, AMundo(v.Gx, v.Gy), Vector2.zero, 0.6f, 0.8f, 1.2f, new Color(0.5f, 1f, 0.6f, 0.9f), new Color(0.5f, 1f, 0.6f, 0f), 0f, 0f, -100f, Ord(v.Gx, v.Gy) + 3, 0.5f);
                        yield return StartCoroutine(Anim(0.2f, delegate (float u) { v.Alto = Mathf.Sin(u * Mathf.PI) * 0.1f; }));
                        Sonido.Tocar("cura", 0.7f, 1f);
                        Vector2 o = AMundo(e.B);
                        for (int i = 0; i < 9; i++)
                            Fx.Una(Arte.Cruz, o + new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0f, 0.3f)), new Vector2(0f, Random.Range(0.5f, 1.1f)), 0.9f, 0.2f, 0.12f, new Color(0.5f, 1f, 0.6f, 1f), new Color(0.8f, 1f, 0.8f, 0f), 0f, 3600);
                        Fx.Anillo(o, 0.1f, 0.7f, 0.5f, new Color(0.5f, 1f, 0.6f, 0.9f), Ord(e.B.X, e.B.Y) + 4);
                        yield return StartCoroutine(Esp(0.2f));
                        break;
                    }

                case EfectoHab.Provocar:
                    {
                        yield return StartCoroutine(Anim(0.15f, delegate (float u) { v.Sx = 1f + 0.18f * u; v.Sy = 1f + 0.12f * u; }));
                        Sonido.Tocar("provocar", 0.8f, 1f);
                        Fx.Anillo(AMundo(v.Gx, v.Gy), 0.2f, 1.6f, 0.5f, new Color(1f, 0.35f, 0.3f, 0.95f), Ord(v.Gx, v.Gy) + 4);
                        Fx.Anillo(AMundo(v.Gx, v.Gy), 0.1f, 1.0f, 0.4f, new Color(1f, 0.8f, 0.3f, 0.9f), Ord(v.Gx, v.Gy) + 4);
                        Fx.Sacudir(0.05f);
                        for (int i = 0; i < 6; i++)
                            StartCoroutine(Proyectil(desde, hasta + new Vector2(0f, 0.5f), Arte.Estrella, 0.18f, new Color(1f, 0.4f, 0.3f), 0.35f + 0.03f * i, 0.2f + 0.08f * i, false, false, Color.white));
                        yield return StartCoroutine(Anim(0.15f, delegate (float u) { v.Sx = 1.18f - 0.18f * u; v.Sy = 1.12f - 0.12f * u; }));
                        yield return StartCoroutine(Esp(0.3f));
                        break;
                    }

                case EfectoHab.ZonaLinea:
                    {
                        bool frasco = h.Zona == ZonaTipo.Frasco || h.Zona == ZonaTipo.Veneno;
                        Color cz = h.Zona == ZonaTipo.Veneno ? new Color(0.6f, 0.9f, 0.25f) : new Color(0.85f, 0.5f, 1f);
                        yield return StartCoroutine(Anim(0.15f, delegate (float u) { v.Sx = 1f + 0.1f * u; v.Rot = (v.MiraIzq ? 1f : -1f) * -8f * u; }));
                        if (frasco)
                        {
                            Sonido.Tocar("pocion", 0.7f, 1f);
                            yield return StartCoroutine(Proyectil(desde, hasta, Arte.Burbuja, 0.3f, cz, 0.4f, 0.9f, false, true, cz));
                            Sonido.Tocar("magia", 0.6f, 0.8f);
                        }
                        else Sonido.Tocar("magia", 0.7f, 1.1f);
                        v.Rot = 0f; v.Sx = 1f;
                        if (e.Area != null)
                            foreach (Pos p in e.Area)
                            {
                                Vector2 w = AMundo(p);
                                if (frasco) Fx.Chorro(Arte.Burbuja, w, 8, 0.3f, 1.0f, 0.8f, 0.2f, new Color(cz.r, cz.g, cz.b, 0.9f), new Color(cz.r, cz.g, cz.b, 0f), -0.5f, 3600);
                                else Fx.Chorro(Arte.Hoja, w, 10, 0.4f, 1.4f, 0.8f, 0.2f, new Color(0.3f, 0.9f, 0.4f, 1f), new Color(0.3f, 0.9f, 0.4f, 0f), -1.2f, 3600);
                                Fx.Anillo(w, 0.1f, 0.6f, 0.4f, frasco ? new Color(cz.r, cz.g, cz.b, 0.9f) : new Color(0.4f, 1f, 0.5f, 0.9f), Ord(p.X, p.Y) + 4);
                            }
                        yield return StartCoroutine(Esp(0.15f));
                        break;
                    }

                default:      // alrededor, cura en área, saltos, embestidas, escudo, potenciar...
                    {
                        Color ce = ColorElem(h.Elemento, h.Efecto == EfectoHab.CuraArea ? new Color(0.5f, 1f, 0.6f) : (h.Efecto == EfectoHab.Escudo ? new Color(0.6f, 0.8f, 1f) : (h.Efecto == EfectoHab.Potenciar ? new Color(1f, 0.6f, 0.3f) : new Color(1f, 0.9f, 0.6f))));
                        bool movimiento = h.Efecto == EfectoHab.Teletransporte || h.Efecto == EfectoHab.TrasEnemigo || h.Efecto == EfectoHab.Intercambio || h.Efecto == EfectoHab.Embestida || h.Efecto == EfectoHab.Carga;
                        yield return StartCoroutine(Anim(movimiento ? 0.14f : 0.2f, delegate (float u) { float s2 = Mathf.Sin(u * Mathf.PI); v.Alto = s2 * (movimiento ? 0.05f : 0.14f); v.Sy = 1f + (movimiento ? -0.15f : 0.1f) * s2; v.Sx = 1f + (movimiento ? 0.12f : -0.05f) * s2; }));
                        if (!movimiento)
                        {
                            Sonido.Tocar(h.Efecto == EfectoHab.CuraArea ? "cura" : (h.Elemento != Elemento.Fisico ? SonidoElem(h.Elemento) : "magia"), 0.7f, 1f);
                            Vector2 centro = h.Efecto == EfectoHab.AreaAlrededor || h.Efecto == EfectoHab.CuraArea ? AMundo(v.Gx, v.Gy) : hasta;
                            Fx.Una(Arte.Runa, centro, Vector2.zero, 0.55f, 0.6f, 1.6f, new Color(ce.r, ce.g, ce.b, 0.9f), new Color(ce.r, ce.g, ce.b, 0f), 0f, 0f, 150f, Ord(v.Gx, v.Gy) + 3, 0.5f);
                            if (e.Area != null)
                                foreach (Pos p in e.Area)
                                {
                                    Vector2 w = AMundo(p);
                                    if (h.Efecto == EfectoHab.CuraArea)
                                        for (int i = 0; i < 4; i++) Fx.Una(Arte.Cruz, w + new Vector2(Random.Range(-0.25f, 0.25f), Random.Range(0f, 0.3f)), new Vector2(0f, Random.Range(0.5f, 1f)), 0.8f, 0.17f, 0.1f, ce, new Color(0.8f, 1f, 0.8f, 0f), 0f, 3600);
                                    else if (h.Efecto == EfectoHab.AreaAlrededor)
                                        Fx.Chorro(SpriteElem(h.Elemento), w + new Vector2(0f, 0.15f), 5, 0.5f, 1.4f, 0.5f, 0.3f, ce, new Color(ce.r, ce.g, ce.b, 0f), 0.2f, 3600);
                                    else
                                        Fx.Chorro(Arte.Estrella, w + new Vector2(0f, 0.3f), 5, 0.4f, 1.1f, 0.6f, 0.2f, ce, new Color(ce.r, ce.g, ce.b, 0f), -0.6f, 3600);
                                    Fx.Anillo(w, 0.1f, 0.6f, 0.4f, new Color(ce.r, ce.g, ce.b, 0.9f), Ord(p.X, p.Y) + 4);
                                }
                            if (h.Efecto == EfectoHab.AreaAlrededor) Fx.Sacudir(0.1f);
                            yield return StartCoroutine(Esp(0.18f));
                        }
                        break;
                    }
            }
        }

        private IEnumerator Rayo(Vector2 destino)
        {
            var go = new GameObject("rayo");
            _temporales.Add(go);
            go.transform.parent = _raiz;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Arte.Rayo; sr.sortingOrder = 3700;
            go.transform.position = new Vector3(destino.x, destino.y, 0f);
            go.transform.localScale = new Vector3(1.6f, 4.6f, 1f);
            for (int i = 0; i < 3; i++)
            {
                sr.color = (i % 2 == 0) ? Color.white : new Color(1f, 0.95f, 0.5f, 0.9f);
                go.transform.localScale = new Vector3(1.6f + 0.2f * i, 4.6f, 1f);
                yield return StartCoroutine(Esp(0.06f));
            }
            Chispas(destino + new Vector2(0f, 0.2f), new Color(1f, 1f, 0.6f), 12);
            _temporales.Remove(go);
            Destroy(go);
        }

        // ---------------------------------------------------------------- consumibles
        private IEnumerator EvConsumible(Evento e)
        {
            VU v = Vis(e.U, true);
            if (v == null) yield break;
            Vector2 desde = AMundo(v.Gx, v.Gy) + new Vector2(0f, 0.4f);
            Vector2 hasta = AMundo(e.B);
            Mirar(v, e.B);
            Fx.Texto(Textos.Nombre(e.Cons), Cabeza(v) + new Vector2(0f, 0.1f), new Color(0.85f, 1f, 0.9f), 17f, 1.1f);
            if (Combate.EsLanzable(e.Cons))
            {
                Elemento el = e.Cons == Consumible.BombaFuego ? Elemento.Fuego : (e.Cons == Consumible.BombaHielo ? Elemento.Hielo : (e.Cons == Consumible.FrascoVeneno ? Elemento.Veneno : Elemento.Fisico));
                Color cb = el == Elemento.Fisico ? new Color(0.18f, 0.18f, 0.22f) : (el == Elemento.Fuego ? new Color(0.75f, 0.2f, 0.15f) : ColorElem(el, Color.white));
                Color ca = el == Elemento.Fisico ? new Color(1f, 0.7f, 0.2f) : ColorElem(el, Color.white);
                yield return StartCoroutine(Anim(0.12f, delegate (float u) { v.Sx = 1f + 0.1f * u; }));
                v.Sx = 1f;
                Sonido.Tocar("salto", 0.5f, 0.7f);
                yield return StartCoroutine(Proyectil(desde, hasta, Arte.Disco, 0.3f, cb, 0.45f, 1.1f, false, true, ca));
                Sonido.Tocar(el == Elemento.Fisico || el == Elemento.Fuego ? "bomba" : SonidoElem(el), 1f, 1f);
                Fx.Sacudir(0.2f);
                if (e.Area != null)
                    foreach (Pos p in e.Area)
                    {
                        Vector2 w = AMundo(p);
                        Fx.Chorro(el == Elemento.Fisico || el == Elemento.Fuego ? Arte.Llama : SpriteElem(el), w + new Vector2(0f, 0.15f), 8, 0.5f, 1.8f, 0.6f, 0.36f, el == Elemento.Fisico || el == Elemento.Fuego ? new Color(1f, 0.9f, 0.4f, 1f) : ca, el == Elemento.Fisico || el == Elemento.Fuego ? new Color(1f, 0.2f, 0.05f, 0f) : new Color(ca.r, ca.g, ca.b, 0f), 0.3f, 3600);
                        Fx.Chorro(Arte.DiscoSuave, w + new Vector2(0f, 0.2f), 4, 0.2f, 0.7f, 0.9f, 0.4f, new Color(0.25f, 0.2f, 0.2f, 0.7f), new Color(0.25f, 0.2f, 0.2f, 0f), 0.4f, 3590);
                        Fx.Anillo(w, 0.1f, 0.7f, 0.4f, new Color(1f, 0.8f, 0.4f, 0.9f), Ord(p.X, p.Y) + 4);
                    }
                yield return StartCoroutine(Esp(0.15f));
            }
            else
            {
                Sonido.Tocar("pocion", 0.8f, 1f);
                Color c = e.Cons == Consumible.PocionVida ? new Color(1f, 0.35f, 0.45f) : (e.Cons == Consumible.PocionMana ? new Color(0.4f, 0.7f, 1f) : (e.Cons == Consumible.PocionVelocidad ? new Color(1f, 0.9f, 0.3f) : new Color(0.4f, 1f, 0.55f)));
                VU objetivo = Vis(e.Obj) ?? v;
                Vector2 o = AMundo(objetivo.Gx, objetivo.Gy);
                yield return StartCoroutine(Anim(0.25f, delegate (float u) { objetivo.Alto = Mathf.Sin(u * Mathf.PI) * 0.2f; objetivo.Sy = 1f + 0.12f * Mathf.Sin(u * Mathf.PI); }));
                for (int i = 0; i < 10; i++)
                    Fx.Una(e.Cons == Consumible.PocionVida ? Arte.Cruz : Arte.Burbuja, o + new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0f, 0.3f)), new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(0.5f, 1.2f)), 0.9f, 0.2f, 0.1f, c, new Color(c.r, c.g, c.b, 0f), 0f, 3600);
                Fx.Anillo(o, 0.1f, 0.7f, 0.5f, c, Ord(objetivo.Gx, objetivo.Gy) + 4);
                yield return StartCoroutine(Esp(0.15f));
            }
        }

        // ---------------------------------------------------------------- daño, esquiva, cura, muerte
        private IEnumerator EvDano(Evento e)
        {
            VU v = Vis(e.Obj, true);
            if (v == null) yield break;
            int n = e.N;
            v.VidaObj = Mathf.Max(0, v.VidaObj - n);
            Vector2 w = AMundo(v.Gx, v.Gy);
            bool aGato = e.Obj.EsGato;
            bool fuerte = n >= 8;
            Fx.Texto("-" + n, Cabeza(v), aGato ? new Color(1f, 0.35f, 0.35f) : new Color(1f, 0.95f, 0.45f), 26f + Mathf.Min(n, 14) * 2f, 1.0f);
            if (!string.IsNullOrEmpty(e.Txt)) Fx.Texto(e.Txt, Cabeza(v) + new Vector2(0f, 0.28f), new Color(1f, 0.8f, 0.5f), 15f, 1.0f);
            Chispas(w + new Vector2(0f, 0.35f), e.Elem != Elemento.Fisico ? ColorElem(e.Elem, Color.white) : (aGato ? new Color(1f, 0.5f, 0.4f) : new Color(1f, 0.9f, 0.4f)), fuerte ? 14 : 7);
            if (e.Elem != Elemento.Fisico)
            {
                Color cel = ColorElem(e.Elem, Color.white);
                Fx.Chorro(SpriteElem(e.Elem), w + new Vector2(0f, 0.35f), 6, 0.5f, 1.4f, 0.6f, 0.22f, cel, new Color(cel.r, cel.g, cel.b, 0f), e.Elem == Elemento.Fuego ? 0.5f : -1f, 3600);
                if (e.Elem == Elemento.Rayo) _flash = Mathf.Max(_flash, 0.25f);
            }
            if (fuerte) Fx.Chorro(Arte.Estrella, w + new Vector2(0f, 0.4f), 6, 1f, 2.4f, 0.5f, 0.28f, new Color(1f, 0.95f, 0.4f, 1f), new Color(1f, 0.6f, 0.2f, 0f), -1.5f, 3600);
            Fx.Sacudir(0.03f + 0.012f * n);
            Sonido.Tocar("dano", 0.8f, Mathf.Clamp(1.25f - n * 0.04f, 0.7f, 1.3f));
            // dirección del retroceso (en pantalla), alejándose del atacante
            Vector2 dir = Vector2.right;
            if (e.U != null) { VU a = Vis(e.U); if (a != null) { Vector2 d = w - AMundo(a.Gx, a.Gy); if (d.sqrMagnitude > 0.001f) dir = d.normalized; } }
            StartCoroutine(Reaccion(v, dir, fuerte ? 0.2f : 0.12f));
            yield return StartCoroutine(Esp(fuerte ? 0.3f : 0.2f));
        }

        private IEnumerator Reaccion(VU v, Vector2 dir, float retroceso)
        {
            yield return StartCoroutine(Anim(0.22f, delegate (float u)
            {
                float s = Mathf.Sin(u * Mathf.PI);
                v.OffX = dir.x * retroceso * s; v.OffY = Mathf.Max(0f, dir.y) * retroceso * s;
                v.Sx = 1f + 0.18f * s; v.Sy = 1f - 0.2f * s;
                float r = Mathf.Clamp01(1f - u * 1.2f);
                v.Tinte = new Color(1f, 1f - 0.65f * r, 1f - 0.65f * r, 1f);
            }));
            v.OffX = 0f; v.OffY = 0f; v.Sx = 1f; v.Sy = 1f; v.Tinte = Color.white;
        }

        private IEnumerator EvEsquiva(Evento e)
        {
            VU v = Vis(e.Obj, true);
            if (v == null) yield break;
            Fx.Texto("¡Esquiva!", Cabeza(v), new Color(0.8f, 0.95f, 1f), 24f, 0.9f);
            Sonido.Tocar("esquiva", 0.7f, 1f);
            float lado = v.MiraIzq ? 1f : -1f;
            Polvo(AMundo(v.Gx, v.Gy), 4);
            StartCoroutine(Anim(0.3f, delegate (float u) { float s = Mathf.Sin(u * Mathf.PI); v.OffX = lado * 0.3f * s; v.Alto = s * 0.1f; v.Alfa = 1f - 0.4f * s; }));
            yield return StartCoroutine(Esp(0.3f));
            v.Alfa = 1f; v.OffX = 0f; v.Alto = 0f;
        }

        private IEnumerator EvCura(Evento e)
        {
            VU v = Vis(e.Obj, true);
            if (v == null) yield break;
            v.VidaObj = Mathf.Min(v.U.VidaMax, v.VidaObj + e.N);
            Vector2 w = AMundo(v.Gx, v.Gy);
            Fx.Texto("+" + e.N, Cabeza(v), new Color(0.5f, 1f, 0.6f), 26f + Mathf.Min(e.N, 10) * 1.5f, 1.0f);
            for (int i = 0; i < 6; i++)
                Fx.Una(Arte.Cruz, w + new Vector2(Random.Range(-0.28f, 0.28f), Random.Range(0f, 0.3f)), new Vector2(0f, Random.Range(0.5f, 1f)), 0.8f, 0.17f, 0.1f, new Color(0.5f, 1f, 0.6f, 1f), new Color(0.8f, 1f, 0.8f, 0f), 0f, 3600);
            Sonido.Tocar("cura", 0.5f, 1.1f);
            StartCoroutine(Anim(0.25f, delegate (float u) { float s = Mathf.Sin(u * Mathf.PI); v.Sy = 1f + 0.1f * s; v.Sx = 1f - 0.05f * s; }));
            yield return StartCoroutine(Esp(e.U == null && !string.IsNullOrEmpty(e.Txt) ? 0.1f : 0.22f));
        }

        private IEnumerator EvMuerte(Evento e)
        {
            VU v = Vis(e.U, true);
            if (v == null) yield break;
            Vector2 w = AMundo(v.Gx, v.Gy);
            Sonido.Tocar("muerte", 0.8f, 1f);
            Fx.Sacudir(0.1f);
            Color c = ColorDe(e.U);
            Fx.Chorro(Arte.Estrella, w + new Vector2(0f, 0.4f), 10, 1f, 2.6f, 0.8f, 0.3f, c, new Color(c.r, c.g, c.b, 0f), -1.5f, 3600);
            Fx.Chorro(Arte.DiscoSuave, w + new Vector2(0f, 0.3f), 6, 0.3f, 1.0f, 0.8f, 0.5f, new Color(0.9f, 0.9f, 0.95f, 0.8f), new Color(0.9f, 0.9f, 0.95f, 0f), 0.2f, 3590);
            Fx.Anillo(w, 0.1f, 1.0f, 0.5f, new Color(1f, 1f, 1f, 0.8f), Ord(v.Gx, v.Gy) + 4);
            bool gato = e.U.EsGato;
            v.VidaObj = 0;
            yield return StartCoroutine(Anim(0.55f, delegate (float u)
            {
                if (gato) { v.Tinte = Color.Lerp(Color.white, new Color(0.6f, 0.6f, 0.75f), u); v.OffY = u * 0.5f; v.Alfa = 1f - u; }
                else { v.Sx = 1f - 0.25f * u; v.Sy = Mathf.Lerp(1f, 0.1f, u * u); v.Rot = u * 200f; v.Alfa = 1f - u * u; }
            }));
            v.Visible = false; v.Muerto = true;
            v.Tinte = Color.white; v.Alfa = 1f; v.OffY = 0f; v.Rot = 0f; v.Sx = 1f; v.Sy = 1f;
            yield return StartCoroutine(Esp(0.1f));
        }

        private IEnumerator EvEmpuje(Evento e)
        {
            VU v = Vis(e.Obj, true);
            if (v == null) yield break;
            float ax = e.A.X, ay = e.A.Y, bx = e.B.X, by = e.B.Y;
            Sonido.Tocar("golpe", 0.5f, 0.8f);
            yield return StartCoroutine(Anim(0.2f, delegate (float u)
            {
                v.Gx = Mathf.Lerp(ax, bx, u); v.Gy = Mathf.Lerp(ay, by, u);
                v.Alto = Mathf.Sin(u * Mathf.PI) * 0.12f; v.Rot = Mathf.Sin(u * Mathf.PI) * 14f * (v.MiraIzq ? 1f : -1f);
            }));
            v.Rot = 0f; v.Alto = 0f;
            Polvo(AMundo(bx, by), 5);
            yield return StartCoroutine(Esp(0.05f));
        }

        private IEnumerator EvSalto(Evento e)
        {
            VU v = Vis(e.U, true);
            if (v == null) yield break;
            float ax = e.A.X, ay = e.A.Y, bx = e.B.X, by = e.B.Y;
            Mirar(v, e.B);
            Fx.Texto("¡Salto!", Cabeza(v), new Color(1f, 0.7f, 0.5f), 22f, 0.9f);
            yield return StartCoroutine(Anim(0.2f, delegate (float u) { v.Sy = 1f - 0.25f * u; v.Sx = 1f + 0.2f * u; }));
            Sonido.Tocar("salto", 0.8f, 0.5f);
            yield return StartCoroutine(Anim(0.55f, delegate (float u)
            {
                v.Gx = Mathf.Lerp(ax, bx, u); v.Gy = Mathf.Lerp(ay, by, u);
                v.Alto = Mathf.Sin(u * Mathf.PI) * 1.6f; v.Sy = 1f + 0.2f * Mathf.Sin(u * Mathf.PI); v.Sx = 1f - 0.12f * Mathf.Sin(u * Mathf.PI);
            }));
            v.Alto = 0f;
            Sonido.Tocar("bomba", 0.9f, 0.8f);
            Fx.Sacudir(0.25f);
            if (e.Area != null)
                foreach (Pos p in e.Area)
                {
                    Polvo(AMundo(p), 5);
                    Fx.Anillo(AMundo(p), 0.1f, 0.8f, 0.45f, new Color(1f, 0.9f, 0.7f, 0.9f), Ord(p.X, p.Y) + 4);
                }
            yield return StartCoroutine(Anim(0.2f, delegate (float u) { float s = Mathf.Sin(u * Mathf.PI); v.Sy = 1f - 0.3f * s; v.Sx = 1f + 0.25f * s; }));
            v.Sx = 1f; v.Sy = 1f;
            yield return StartCoroutine(Esp(0.1f));
        }

        private IEnumerator EvInvocar(Evento e)
        {
            VU v = Vis(e.U, false);
            if (v == null) v = CrearVU(e.U, false);
            v.Visible = true; v.Go.SetActive(true); v.Aparicion = 0f;
            Vector2 w = AMundo(e.U.Pos);
            Sonido.Tocar("invocar", 0.8f, 1f);
            Fx.Una(Arte.Runa, w, Vector2.zero, 0.7f, 0.5f, 1.4f, new Color(0.7f, 0.4f, 1f, 0.9f), new Color(0.7f, 0.4f, 1f, 0f), 0f, 0f, 200f, Ord(e.U.Pos.X, e.U.Pos.Y) + 3, 0.5f);
            Fx.Chorro(Arte.Chispa, w + new Vector2(0f, 0.3f), 10, 0.6f, 1.8f, 0.7f, 0.22f, new Color(0.8f, 0.6f, 1f, 1f), new Color(0.8f, 0.6f, 1f, 0f), 0f, 3600);
            yield return StartCoroutine(Anim(0.4f, delegate (float u) { v.Aparicion = AtrasSuave(u); }));
            v.Aparicion = 1f;
            v.VidaVis = v.VidaRetraso = e.U.Vida; v.VidaObj = e.U.Vida;
            yield return StartCoroutine(Esp(0.1f));
        }

        private IEnumerator EvZona(Evento e)
        {
            if (e.Area == null || _tiles == null) yield break;
            bool fuego = e.Zona == ZonaTipo.Fuego;
            Sonido.Tocar(fuego ? "fuego" : (e.Zona == ZonaTipo.Veneno ? "veneno" : "magia"), 0.5f, 1f);
            foreach (Pos p in e.Area)
            {
                if (!C.Tab.Dentro(p)) continue;
                VC c = _tiles[p.X, p.Y];
                c.TieneZona = true; c.ZT = e.Zona; c.PopT = 0f;
                if (e.Zona == ZonaTipo.Fuego && c.T == Terreno.HierbaAlta) PonerCasilla(c, C.Tab.TerrenoEn(p));
                yield return StartCoroutine(Esp(0.03f));
            }
            yield return StartCoroutine(Esp(0.1f));
        }

        private IEnumerator EvTerreno(Evento e)
        {
            if (e.Area == null || _tiles == null) yield break;
            bool muro = e.Txt == "muro";
            Sonido.Tocar(muro ? "muro" : "barro", 0.8f, 1f);
            Fx.Sacudir(muro ? 0.15f : 0.06f);
            foreach (Pos p in e.Area)
            {
                if (!C.Tab.Dentro(p)) continue;
                VC c = _tiles[p.X, p.Y];
                PonerCasilla(c, muro ? Terreno.Muro : Terreno.Barro);
                c.PopT = 0f;
                Polvo(AMundo(p), 4);
                yield return StartCoroutine(Esp(0.05f));
            }
            yield return StartCoroutine(Esp(0.15f));
        }

        private IEnumerator EvTexto(Evento e)
        {
            Color c = new Color(1f, 0.95f, 0.6f);
            if (e.Obj != null)
            {
                VU v = Vis(e.Obj);
                if (v != null) Fx.Texto(e.Txt, Cabeza(v), c, 22f, 1.0f);
                Sonido.Tocar("texto", 0.6f, 1f);
            }
            else
            {
                Fx.Texto(e.Txt, AMundo(e.B) + new Vector2(0f, 0.5f), c, 22f, 1.0f);
                // obstáculo dañado: se oscurece según la vida que le queda y suelta un poco de polvo
                if (_tiles != null && C != null && C.Tab.Dentro(e.B) && C.Tab.EsRompible(e.B))
                {
                    VC t = _tiles[e.B.X, e.B.Y];
                    float f = Mathf.Clamp01((float)C.Tab.VidaObstaculo(e.B) / Mathf.Max(1, Tablero.VidaInicial(C.Tab.TerrenoEn(e.B))));
                    t.Bloque.color = new Color(0.62f + 0.38f * f, 0.55f + 0.45f * f, 0.5f + 0.5f * f, 1f);
                    t.PopT = 0f;
                    Polvo(AMundo(e.B), 3);
                    Sonido.Tocar("romper", 0.4f, 1.5f);
                }
            }
            yield return StartCoroutine(Esp(0.2f));
        }

        private IEnumerator EvTormenta(Evento e)
        {
            Anunciar("¡Tormenta!", new Color(0.7f, 0.85f, 1f));
            Sonido.Tocar("tormenta", 1f, 1f);
            for (int i = 0; i < 3; i++) { _flash = 0.9f; Fx.Sacudir(0.2f); yield return StartCoroutine(Esp(0.2f)); }
            foreach (VU v in Unidades)
                if (v.Visible && v.U.EsGato) StartCoroutine(Rayo(AMundo(v.Gx, v.Gy)));
            yield return StartCoroutine(Esp(0.4f));
        }

        private IEnumerator EvFin(Evento e)
        {
            bool victoria = e.N == 1;
            ActivoVisual = null;
            _focoObj = Vector2.zero;
            if (victoria)
            {
                Anunciar("¡VICTORIA!", new Color(0.7f, 1f, 0.7f));
                Sonido.Tocar("victoria", 1f, 1f);
                StartCoroutine(Confeti());
            }
            else
            {
                Anunciar("DERROTA", new Color(1f, 0.55f, 0.55f));
                Sonido.Tocar("derrota", 1f, 1f);
            }
            yield return StartCoroutine(Esp(victoria ? 0.9f : 0.7f));
        }

        private IEnumerator Confeti()
        {
            Color[] cols = { new Color(1f, 0.4f, 0.5f), new Color(1f, 0.85f, 0.3f), new Color(0.4f, 0.85f, 1f), new Color(0.6f, 1f, 0.5f), new Color(0.85f, 0.55f, 1f) };
            for (int i = 0; i < 70; i++)
            {
                Vector2 c = Cam.transform.position;
                float ancho = Cam.orthographicSize * 2f * Mathf.Max(0.5f, (float)Screen.width / Mathf.Max(1, Screen.height));
                Color col = cols[Random.Range(0, cols.Length)];
                Fx.Una(Random.value < 0.5f ? Arte.Estrella : Arte.Cuadrado, new Vector2(c.x + Random.Range(-ancho / 2, ancho / 2), c.y + Cam.orthographicSize + 0.2f), new Vector2(Random.Range(-0.8f, 0.8f), Random.Range(-1.5f, -0.5f)), 3.2f, 0.14f, 0.12f, col, new Color(col.r, col.g, col.b, 0.7f), -0.3f, Random.Range(0f, 360f), Random.Range(-360f, 360f), 3800, 1f);
                if (i % 5 == 0) yield return StartCoroutine(Esp(0.03f));
            }
        }

        // ---------------------------------------------------------------- eventos nuevos: saltos, embestidas, monedas, obstáculos
        private IEnumerator EvTeletransporte(Evento e)
        {
            VU v = Vis(e.U, true);
            if (v == null) yield break;
            string id = e.Txt ?? "";
            bool arco = id == "salto" || id == "voltereta" || id == "pirueta" || id == "burbuja" || id == "protector";
            Color c = id == "paso_sombrio" ? new Color(0.45f, 0.2f, 0.6f) : (id == "paso_divino" ? new Color(1f, 0.9f, 0.45f) : (id == "viaje_raiz" ? new Color(0.35f, 0.85f, 0.4f) : (id == "burbuja" ? new Color(0.85f, 0.55f, 1f) : new Color(0.6f, 0.55f, 1f))));
            Vector2 wa = AMundo(e.A), wb = AMundo(e.B);
            Mirar(v, e.B);
            if (arco)
            {
                Sonido.Tocar("salto", 0.7f, 0.8f);
                float ax = e.A.X, ay = e.A.Y, bx = e.B.X, by = e.B.Y;
                yield return StartCoroutine(Anim(0.08f, delegate (float u) { v.Sy = 1f - 0.2f * u; v.Sx = 1f + 0.15f * u; }));
                yield return StartCoroutine(Anim(0.38f, delegate (float u)
                {
                    v.Gx = Mathf.Lerp(ax, bx, u); v.Gy = Mathf.Lerp(ay, by, u);
                    v.Alto = Mathf.Sin(u * Mathf.PI) * 0.9f; v.Rot = (id == "voltereta" || id == "pirueta") ? u * 360f * (v.MiraIzq ? 1f : -1f) : 0f;
                    v.Sy = 1f + 0.12f * Mathf.Sin(u * Mathf.PI);
                }));
                v.Gx = bx; v.Gy = by; v.Alto = 0f; v.Rot = 0f; v.Sx = 1f; v.Sy = 1f;
                Polvo(wb, 5);
                Fx.Anillo(wb, 0.1f, 0.6f, 0.35f, new Color(c.r, c.g, c.b, 0.8f), Ord(e.B.X, e.B.Y) + 4);
                Sonido.Tocar("paso", 0.6f, 0.8f);
                yield return StartCoroutine(Esp(0.05f));
            }
            else
            {
                Sonido.Tocar("teleport", 0.7f, id == "paso_sombrio" ? 0.8f : 1f);
                Fx.Chorro(Arte.DiscoSuave, wa + new Vector2(0f, 0.35f), 8, 0.3f, 1.0f, 0.5f, 0.3f, new Color(c.r, c.g, c.b, 0.9f), new Color(c.r, c.g, c.b, 0f), 0.2f, 3600);
                Fx.Anillo(wa, 0.1f, 0.8f, 0.4f, new Color(c.r, c.g, c.b, 0.9f), Ord(e.A.X, e.A.Y) + 4);
                yield return StartCoroutine(Anim(0.14f, delegate (float u) { v.Aparicion = 1f - u; v.Sx = 1f + 0.4f * u; v.Sy = 1f - 0.2f * u; }));
                v.Gx = e.B.X; v.Gy = e.B.Y; v.Sx = 1f; v.Sy = 1f;
                Fx.Chorro(Arte.Estrella, wb + new Vector2(0f, 0.35f), 8, 0.5f, 1.4f, 0.5f, 0.22f, c, new Color(c.r, c.g, c.b, 0f), -0.8f, 3600);
                Fx.Anillo(wb, 0.1f, 0.8f, 0.4f, new Color(c.r, c.g, c.b, 0.9f), Ord(e.B.X, e.B.Y) + 4);
                yield return StartCoroutine(Anim(0.18f, delegate (float u) { v.Aparicion = AtrasSuave(u); }));
                v.Aparicion = 1f;
            }
        }

        private IEnumerator EvEmbestida(Evento e)
        {
            VU v = Vis(e.U, true);
            if (v == null) yield break;
            var ruta = new List<Pos>();
            ruta.Add(e.A);
            if (e.Camino != null) ruta.AddRange(e.Camino);
            if (ruta[ruta.Count - 1] != e.B) ruta.Add(e.B);
            Mirar(v, e.B);
            Sonido.Tocar("flecha", 0.6f, 0.6f);
            yield return StartCoroutine(Anim(0.08f, delegate (float u) { v.Sx = 1f + 0.18f * u; v.Sy = 1f - 0.12f * u; }));
            for (int i = 1; i < ruta.Count; i++)
            {
                float fx = ruta[i - 1].X, fy = ruta[i - 1].Y, tx = ruta[i].X, ty = ruta[i].Y;
                yield return StartCoroutine(Anim(0.055f, delegate (float u) { v.Gx = Mathf.Lerp(fx, tx, u); v.Gy = Mathf.Lerp(fy, ty, u); }));
                Polvo(AMundo(tx, ty), 2);
            }
            v.Gx = e.B.X; v.Gy = e.B.Y; v.Sx = 1f; v.Sy = 1f;
            yield return StartCoroutine(Esp(0.04f));
        }

        private IEnumerator EvMoneda(Evento e)
        {
            if (_tiles == null || !C.Tab.Dentro(e.B)) yield break;
            VC c = _tiles[e.B.X, e.B.Y];
            PonerMonedas(c, c.Monedas + Mathf.Max(1, e.N));
            c.PopT = 0f;
            Vector2 w = AMundo(e.B);
            Sonido.Tocar("moneda", 0.5f, 0.9f);
            Fx.Chorro(Arte.Chispa, w + new Vector2(0f, 0.3f), 6, 0.5f, 1.4f, 0.5f, 0.2f, new Color(1f, 0.9f, 0.3f), new Color(1f, 0.9f, 0.3f, 0f), -2f, 3600);
            yield return StartCoroutine(Esp(0.08f));
        }

        private IEnumerator EvRecoger(Evento e)
        {
            VU v = Vis(e.U, true);
            if (_tiles != null && C.Tab.Dentro(e.B)) PonerMonedas(_tiles[e.B.X, e.B.Y], 0);
            Vector2 w = AMundo(e.B);
            Sonido.Tocar("moneda", 0.7f, 1.1f);
            Fx.Chorro(Arte.Chispa, w + new Vector2(0f, 0.3f), 8, 0.6f, 1.6f, 0.5f, 0.2f, new Color(1f, 0.9f, 0.3f), new Color(1f, 0.9f, 0.3f, 0f), -2f, 3600);
            bool ladron = e.U != null && !e.U.EsGato;
            Fx.Texto((ladron ? "¡Roba " : "+") + e.N + (ladron ? "!" : " monedas"), (v != null ? Cabeza(v) : w + new Vector2(0f, 0.8f)) + new Vector2(0f, 0.15f), ladron ? new Color(1f, 0.6f, 0.5f) : new Color(1f, 0.9f, 0.3f), 20f, 0.9f);
            yield return StartCoroutine(Esp(0.1f));
        }

        private IEnumerator EvRomper(Evento e)
        {
            if (_tiles == null || !C.Tab.Dentro(e.B)) yield break;
            VC c = _tiles[e.B.X, e.B.Y];
            Vector2 w = AMundo(e.B);
            string que = e.Txt ?? "roca";
            Color col = que == "caja" ? new Color(0.72f, 0.5f, 0.25f) : (que == "barril" ? new Color(0.75f, 0.25f, 0.2f) : new Color(0.7f, 0.68f, 0.62f));
            Sonido.Tocar("romper", 0.9f, que == "caja" ? 1.2f : 0.9f);
            Fx.Sacudir(0.08f);
            Fx.Chorro(Arte.Cuadrado, w + new Vector2(0f, 0.3f), 12, 0.8f, 2.2f, 0.7f, 0.14f, col, new Color(col.r, col.g, col.b, 0f), -3f, 3600);
            Polvo(w, 6);
            PonerCasilla(c, C.Tab.TerrenoEn(e.B));
            c.PopT = 0f;
            yield return StartCoroutine(Esp(0.15f));
        }

        private IEnumerator EvExplosion(Evento e)
        {
            Color col = ColorElem(e.Elem, new Color(1f, 0.7f, 0.25f));
            Sonido.Tocar("bomba", 1f, 1f);
            Fx.Sacudir(0.22f);
            _flash = Mathf.Max(_flash, 0.35f);
            if (e.Area != null)
                foreach (Pos p in e.Area)
                {
                    Vector2 w = AMundo(p);
                    Fx.Chorro(e.Elem == Elemento.Fisico || e.Elem == Elemento.Fuego ? Arte.Llama : SpriteElem(e.Elem), w + new Vector2(0f, 0.15f), 9, 0.5f, 1.8f, 0.6f, 0.36f, new Color(1f, 0.9f, 0.4f, 1f), new Color(col.r, col.g * 0.3f, col.b * 0.2f, 0f), 0.3f, 3600);
                    Fx.Chorro(Arte.DiscoSuave, w + new Vector2(0f, 0.2f), 4, 0.2f, 0.7f, 0.9f, 0.4f, new Color(0.25f, 0.2f, 0.2f, 0.7f), new Color(0.25f, 0.2f, 0.2f, 0f), 0.4f, 3590);
                    Fx.Anillo(w, 0.1f, 0.8f, 0.45f, new Color(col.r, col.g, col.b, 0.9f), Ord(p.X, p.Y) + 4);
                }
            yield return StartCoroutine(Esp(0.25f));
        }

        private IEnumerator EvChoque(Evento e)
        {
            VU v = Vis(e.Obj, true);
            Vector2 w = AMundo(e.B);
            Sonido.Tocar("choque", 1f, 1f);
            Fx.Sacudir(0.12f);
            Fx.Chorro(Arte.Estrella, w + new Vector2(0f, 0.4f), 7, 0.8f, 2f, 0.5f, 0.24f, new Color(1f, 0.95f, 0.5f), new Color(1f, 0.6f, 0.2f, 0f), -1.5f, 3600);
            Fx.Texto("¡Choque!", (v != null ? Cabeza(v) : w + new Vector2(0f, 0.8f)) + new Vector2(0f, 0.3f), new Color(1f, 0.8f, 0.4f), 22f, 0.9f);
            if (v != null) StartCoroutine(Reaccion(v, Vector2.right * (v.MiraIzq ? 1f : -1f), 0.16f));
            yield return StartCoroutine(Esp(0.22f));
        }

        // ================================================================ efectos sueltos para pantallas
        /// <summary>Celebración al elegir algo en las pantallas de recompensa.</summary>
        public void Celebrar(bool bien)
        {
            if (bien) StartCoroutine(Confeti());
            Sonido.Tocar(bien ? "victoria" : "derrota", 0.7f, 1f);
        }
    }
}
