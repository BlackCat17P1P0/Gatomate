// Gatomate - vista. Efectos visuales: partículas con pool de sprites, anillos, textos flotantes y temblor de cámara.
using System.Collections.Generic;
using UnityEngine;

namespace Gatomate.View
{
    public sealed class TextoFlotante
    {
        public string Txt;
        public Vector2 Mundo;       // posición en el mundo
        public Vector2 Vel;
        public float T, Dur, Tam;
        public Color Col;
        public bool Rebote;
    }

    public sealed class Fx
    {
        private sealed class P
        {
            public GameObject go;
            public SpriteRenderer sr;
            public Vector2 pos, vel;
            public float vida, max, grav, rot, vrot, k0, k1, escY;
            public Color c0, c1;
            public bool viva;
        }

        private readonly List<P> _ps = new List<P>();
        private readonly Transform _raiz;
        public readonly List<TextoFlotante> Textos = new List<TextoFlotante>();
        public float Temblor;                 // amplitud actual del temblor de cámara (unidades de mundo)

        public Fx(Transform raiz) { _raiz = raiz; }

        private P Libre()
        {
            for (int i = 0; i < _ps.Count; i++) if (!_ps[i].viva) return _ps[i];
            if (_ps.Count >= 700) return _ps[0];
            var p = new P();
            p.go = new GameObject("fx");
            p.go.transform.parent = _raiz;
            p.sr = p.go.AddComponent<SpriteRenderer>();
            _ps.Add(p);
            return p;
        }

        /// <summary>Una partícula. 'tam' = diámetro en unidades de mundo (al principio y al final).</summary>
        public void Una(Sprite s, Vector2 pos, Vector2 vel, float vida, float tam0, float tam1, Color c0, Color c1, float grav, float rot, float vrot, int orden, float escY)
        {
            P p = Libre();
            p.viva = true;
            p.pos = pos; p.vel = vel; p.vida = vida; p.max = Mathf.Max(0.01f, vida);
            float w = s.bounds.size.x;
            p.k0 = tam0 / w; p.k1 = tam1 / w;
            p.c0 = c0; p.c1 = c1; p.grav = grav; p.rot = rot; p.vrot = vrot; p.escY = escY;
            p.sr.sprite = s;
            p.sr.sortingOrder = orden;
            p.go.SetActive(true);
            Poner(p, 0f);
        }

        public void Una(Sprite s, Vector2 pos, Vector2 vel, float vida, float tam0, float tam1, Color c0, Color c1, float grav, int orden)
        {
            Una(s, pos, vel, vida, tam0, tam1, c0, c1, grav, 0f, 0f, orden, 1f);
        }

        /// <summary>Chorro de partículas hacia todos lados (con un poco de sesgo hacia arriba).</summary>
        public void Chorro(Sprite s, Vector2 pos, int n, float vmin, float vmax, float vida, float tam, Color c0, Color c1, float grav, int orden)
        {
            for (int i = 0; i < n; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                float v = Random.Range(vmin, vmax);
                Vector2 vel = new Vector2(Mathf.Cos(a) * v, Mathf.Sin(a) * v * 0.7f + v * 0.3f);
                Una(s, pos, vel, vida * Random.Range(0.7f, 1.2f), tam * Random.Range(0.7f, 1.2f), tam * 0.2f, c0, c1, grav, Random.Range(0f, 360f), Random.Range(-300f, 300f), orden, 1f);
            }
        }

        /// <summary>Anillo que se expande en el suelo (aplastado por la perspectiva isométrica).</summary>
        public void Anillo(Vector2 pos, float r0, float r1, float vida, Color c, int orden)
        {
            Color fin = c; fin.a = 0f;
            Una(Arte.Anillo, pos, Vector2.zero, vida, r0 * 2f, r1 * 2f, c, fin, 0f, 0f, 0f, orden, 0.5f);
        }

        public void Destello(Vector2 pos, float tam, float vida, Color c, int orden)
        {
            Color fin = c; fin.a = 0f;
            Una(Arte.DiscoSuave, pos, Vector2.zero, vida, tam, tam * 1.6f, c, fin, 0f, 0f, 0f, orden, 1f);
        }

        public void Texto(string txt, Vector2 mundo, Color col, float tam, float dur)
        {
            Textos.Add(new TextoFlotante { Txt = txt, Mundo = mundo, Vel = new Vector2(Random.Range(-0.15f, 0.15f), 0.9f), T = 0f, Dur = dur, Tam = tam, Col = col, Rebote = true });
        }

        public void Sacudir(float cantidad) { Temblor = Mathf.Max(Temblor, Mathf.Min(0.35f, cantidad)); }

        private static void Poner(P p, float u)
        {
            float k = Mathf.Lerp(p.k0, p.k1, u);
            p.go.transform.position = new Vector3(p.pos.x, p.pos.y, 0f);
            p.go.transform.localScale = new Vector3(k, k * p.escY, 1f);
            p.go.transform.eulerAngles = new Vector3(0f, 0f, p.rot);
            p.sr.color = Color.Lerp(p.c0, p.c1, u);
        }

        public void Actualizar(float dt)
        {
            for (int i = 0; i < _ps.Count; i++)
            {
                P p = _ps[i];
                if (!p.viva) continue;
                p.vida -= dt;
                if (p.vida <= 0f) { p.viva = false; p.go.SetActive(false); continue; }
                p.vel.y += p.grav * dt;
                p.pos += p.vel * dt;
                p.rot += p.vrot * dt;
                Poner(p, 1f - p.vida / p.max);
            }
            for (int i = Textos.Count - 1; i >= 0; i--)
            {
                TextoFlotante t = Textos[i];
                t.T += dt;
                t.Mundo += t.Vel * dt;
                t.Vel.y = Mathf.Max(0.15f, t.Vel.y - dt * 1.4f);
                if (t.T >= t.Dur) Textos.RemoveAt(i);
            }
            Temblor = Mathf.MoveTowards(Temblor, 0f, dt * 0.9f);
        }

        public void Limpiar()
        {
            for (int i = 0; i < _ps.Count; i++) { _ps[i].viva = false; _ps[i].go.SetActive(false); }
            Textos.Clear();
            Temblor = 0f;
        }
    }
}
