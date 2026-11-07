// Gatomate - vista. Sonido 100% procedural: los efectos y las músicas se sintetizan por código al arrancar,
// así el prototipo no necesita archivos de audio. Quien haga el audio real puede reemplazar los clips.
// El generador de muestras (Sintetizador) es C# puro; solo "Sonido" toca Unity.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gatomate.View
{
    /// <summary>Generación de muestras (sin Unity).</summary>
    public static class Sintetizador
    {
        public const int Hz = 22050;

        private static float Fase(double f, double t) { double x = f * t; return (float)(x - Math.Floor(x)); }

        public static float Onda(string forma, double f, double t)
        {
            float p = Fase(f, t);
            switch (forma)
            {
                case "cuadrada": return p < 0.5f ? 1f : -1f;
                case "pulso": return p < 0.25f ? 1f : -1f;
                case "sierra": return 2f * p - 1f;
                case "triangulo": return 4f * Math.Abs(p - 0.5f) - 1f;
                default: return (float)Math.Sin(2 * Math.PI * f * t);
            }
        }

        public static float[] Crear(float dur, Func<float, float> f)
        {
            int n = Math.Max(1, (int)(dur * Hz));
            var d = new float[n];
            for (int i = 0; i < n; i++) d[i] = f(i / (float)Hz);
            return d;
        }

        /// <summary>Envolvente rápida: ataque corto y caída exponencial.</summary>
        public static float Env(float t, float dur, float ataque, float caida)
        {
            float a = ataque <= 0 ? 1f : Math.Min(1f, t / ataque);
            float r = Math.Min(1f, (dur - t) / 0.02f);
            return a * (float)Math.Exp(-caida * t) * Math.Max(0f, r);
        }

        public static float Ruido(ref uint estado)
        {
            estado ^= estado << 13; estado ^= estado >> 17; estado ^= estado << 5;
            return (estado & 0xFFFF) / 32768f - 1f;
        }

        // ------------------------------------------------------------------ efectos
        /// <summary>Efecto ya normalizado para que no recorte.</summary>
        public static float[] Efecto(string id)
        {
            float[] d = EfectoCrudo(id);
            float pico = 0f;
            for (int i = 0; i < d.Length; i++) pico = Math.Max(pico, Math.Abs(d[i]));
            if (pico > 0.9f) { float k = 0.9f / pico; for (int i = 0; i < d.Length; i++) d[i] *= k; }
            return d;
        }

        private static float[] EfectoCrudo(string id)
        {
            uint rs = 12345u;
            switch (id)
            {
                case "clic": return Crear(0.06f, t => 0.5f * Onda("cuadrada", 880 + 600 * t * 10, t) * Env(t, 0.06f, 0.001f, 40));
                case "hover": return Crear(0.03f, t => 0.25f * Onda("triangulo", 1200, t) * Env(t, 0.03f, 0.001f, 60));
                case "error": return Crear(0.16f, t => 0.4f * Onda("cuadrada", 140, t) * Env(t, 0.16f, 0.002f, 12));
                case "paso": return Crear(0.09f, t => 0.55f * (Onda("seno", 160 - 800 * t, t) * 0.8f + Ruido(ref rs) * 0.25f) * Env(t, 0.09f, 0.002f, 40));
                case "barro": return Crear(0.18f, t => 0.5f * (Onda("seno", 90 + 60 * Math.Abs(Math.Sin(t * 40)), t) * 0.7f + Ruido(ref rs) * 0.3f) * Env(t, 0.18f, 0.004f, 14));
                case "hierba": return Crear(0.14f, t => 0.35f * Ruido(ref rs) * (float)Math.Sin(Math.PI * t / 0.14f));
                case "salto": return Crear(0.16f, t => 0.45f * Onda("triangulo", 280 + 1500 * t, t) * Env(t, 0.16f, 0.002f, 10));
                case "golpe": return Crear(0.22f, t => 0.8f * (Onda("seno", 150 - 400 * t, t) * 0.8f + Ruido(ref rs) * 0.6f * (float)Math.Exp(-40 * t)) * Env(t, 0.22f, 0.001f, 14));
                case "dano": return Crear(0.28f, t => 0.7f * (Onda("cuadrada", 420 - 1100 * t, t) * 0.5f + Ruido(ref rs) * 0.4f) * Env(t, 0.28f, 0.001f, 11));
                case "esquiva": return Crear(0.26f, t => 0.4f * Ruido(ref rs) * (float)Math.Sin(Math.PI * t / 0.26f) * (0.3f + 3f * t));
                case "cura": return Arpegio(new[] { 523.25, 659.25, 783.99, 1046.5 }, 0.07f, "triangulo", 0.5f, 0.12f);
                case "magia":
                    return Crear(0.45f, t =>
                    {
                        float v = 0;
                        for (int k = 0; k < 4; k++) v += Onda("seno", 1500 + 700 * k + 900 * Math.Sin(t * (9 + k * 3)), t) * 0.2f * (float)Math.Max(0, Math.Sin(Math.PI * Math.Min(1, (t * 2.2f + k * 0.12f))));
                        return v * Env(t, 0.45f, 0.01f, 4);
                    });
                case "fuego": return Crear(0.5f, t => 0.65f * (Ruido(ref rs) * (0.5f + 0.5f * (float)Math.Exp(-6 * t)) + Onda("seno", 70, t) * 0.7f) * Env(t, 0.5f, 0.004f, 6));
                case "flecha": return Crear(0.2f, t => 0.45f * (Ruido(ref rs) * (float)Math.Sin(Math.PI * t / 0.2f) * 0.6f + Onda("triangulo", 900 - 2500 * t, t) * 0.3f));
                case "provocar": return Crear(0.45f, t => 0.5f * (Onda("sierra", 150 + 100 * t, t) * 0.6f + Onda("cuadrada", 225 + 150 * t, t) * 0.3f) * Env(t, 0.45f, 0.03f, 5));
                case "muerte": return Crear(0.6f, t => 0.6f * Onda("triangulo", 520 * Math.Exp(-3.6 * t) + 50, t) * Env(t, 0.6f, 0.002f, 3.5f));
                case "invocar": return Crear(0.4f, t => 0.5f * Onda("seno", 200 + 900 * t + 30 * Math.Sin(t * 60), t) * Env(t, 0.4f, 0.01f, 5));
                case "bomba": return Crear(0.75f, t => 0.9f * (Ruido(ref rs) * (float)Math.Exp(-5 * t) + Onda("seno", 60 - 40 * t, t) * 1.1f) * Env(t, 0.75f, 0.001f, 4));
                case "pocion": return Crear(0.3f, t => 0.45f * Onda("seno", 400 + 500 * Math.Abs(Math.Sin(t * 25)), t) * Env(t, 0.3f, 0.01f, 6));
                case "turno": return Arpegio(new[] { 783.99, 1046.5 }, 0.09f, "cuadrada", 0.3f, 0.1f);
                case "victoria": return Arpegio(new[] { 523.25, 659.25, 783.99, 1046.5, 783.99, 1046.5, 1318.5 }, 0.12f, "cuadrada", 0.35f, 0.5f);
                case "derrota": return Arpegio(new[] { 392.0, 349.23, 311.13, 261.63, 196.0 }, 0.2f, "triangulo", 0.55f, 0.5f);
                case "carta": return Arpegio(new[] { 880.0, 1174.7 }, 0.05f, "triangulo", 0.4f, 0.08f);
                case "tormenta": return Crear(1.0f, t => 0.8f * (Ruido(ref rs) * (0.5f + 0.5f * (float)Math.Sin(t * 37))) * Env(t, 1.0f, 0.02f, 2.2f) + 0.5f * Onda("seno", 45, t) * Env(t, 1.0f, 0.01f, 2));
                case "muro": return Crear(0.35f, t => 0.8f * (Onda("seno", 80 - 40 * t, t) + Ruido(ref rs) * 0.5f * (float)Math.Exp(-25 * t)) * Env(t, 0.35f, 0.001f, 9));
                case "moneda": return Arpegio(new[] { 1318.5, 1760.0 }, 0.05f, "cuadrada", 0.3f, 0.09f);
                case "romper": return Crear(0.32f, t => 0.8f * (Ruido(ref rs) * (float)Math.Exp(-14 * t) + Onda("seno", 110 - 60 * t, t) * 0.6f * (float)Math.Exp(-8 * t)));
                case "hielo": return Arpegio(new[] { 2093.0, 1568.0, 2637.0 }, 0.04f, "triangulo", 0.4f, 0.12f);
                case "veneno": return Crear(0.3f, t => 0.4f * Onda("seno", 260 + 240 * Math.Abs(Math.Sin(t * 28)), t) * Env(t, 0.3f, 0.01f, 8));
                case "teleport": return Crear(0.28f, t => 0.45f * Onda("seno", 1500 - 4200 * t, t) * Env(t, 0.28f, 0.003f, 9));
                case "choque": return Crear(0.2f, t => 0.9f * (Onda("seno", 90 - 100 * t, t) + Ruido(ref rs) * 0.5f * (float)Math.Exp(-30 * t)) * Env(t, 0.2f, 0.001f, 16));
                case "rayo": return Crear(0.4f, t => 0.7f * (Ruido(ref rs) * (float)Math.Exp(-9 * t) + Onda("sierra", 900 - 1800 * t, t) * 0.3f * (float)Math.Exp(-6 * t)));
                case "texto": return Crear(0.06f, t => 0.3f * Onda("seno", 700 + 800 * t * 10, t) * Env(t, 0.06f, 0.001f, 30));
                default: return Crear(0.05f, t => 0f);
            }
        }

        private static float[] Arpegio(double[] notas, float dur, string forma, float vol, float cola)
        {
            float total = notas.Length * dur + cola;
            return Crear(total, t =>
            {
                float v = 0;
                for (int i = 0; i < notas.Length; i++)
                {
                    float t0 = i * dur;
                    if (t < t0) continue;
                    float lt = t - t0;
                    v += Onda(forma, notas[i], lt) * (float)Math.Exp(-8 * lt) * Math.Min(1f, lt / 0.004f);
                }
                return v * vol * Math.Min(1f, (total - t) / 0.03f);
            });
        }

        // ------------------------------------------------------------------ músicas (chiptune)
        private static double Nota(int grado, int escalaBase, bool menor)
        {
            int[] mayor = { 0, 2, 4, 5, 7, 9, 11 };
            int[] men = { 0, 2, 3, 5, 7, 8, 10 };
            int[] esc = menor ? men : mayor;
            int oct = (int)Math.Floor(grado / 7.0);
            int g = ((grado % 7) + 7) % 7;
            int semitono = escalaBase + esc[g] + 12 * oct;
            return 440.0 * Math.Pow(2.0, (semitono - 69) / 12.0);
        }

        /// <summary>Una pista de música en bucle. "calma", "combate" o "jefe".</summary>
        public static float[] Musica(string id)
        {
            bool menor = id != "calma";
            float bpm = id == "calma" ? 96f : (id == "combate" ? 132f : 152f);
            int baseNota = id == "calma" ? 60 : (id == "combate" ? 57 : 52);   // C4, A3, E3
            int compases = 8;
            float beat = 60f / bpm;
            float dur = compases * 4 * beat;
            int[] prog = menor ? new[] { 0, 5, 2, 6, 0, 5, 3, 4 } : new[] { 0, 4, 5, 3, 0, 4, 5, 4 };   // raíz de cada compás (grados)
            var rnd = new System.Random(id == "calma" ? 7 : (id == "combate" ? 21 : 33));
            // melodía pregenerada: 16 corcheas por 2 compases, se repite con variación
            int[] mel = new int[compases * 8];
            int cur = 4;
            for (int i = 0; i < mel.Length; i++)
            {
                int compas = i / 8;
                int[] acorde = { prog[compas], prog[compas] + 2, prog[compas] + 4, prog[compas] + 7 };
                if (rnd.NextDouble() < 0.55) cur = acorde[rnd.Next(acorde.Length)] + 7;
                else cur += rnd.Next(-1, 2);
                cur = Math.Max(3, Math.Min(14, cur));
                mel[i] = (rnd.NextDouble() < (id == "calma" ? 0.3 : 0.15)) ? int.MinValue : cur;
            }
            uint rs = 99u;
            float vol = id == "calma" ? 0.5f : 0.55f;
            return Crear(dur, t =>
            {
                float b = t / beat;                       // tiempo en pulsos
                int compas = Math.Min(compases - 1, (int)(b / 4));
                float v = 0;
                // bajo: octava abajo, un golpe por pulso (combate/jefe) o por dos pulsos (calma)
                float stepB = id == "calma" ? 2f : 1f;
                float lb = (b % stepB);
                double fb = Nota(prog[compas] - 7, baseNota - 12, menor);
                if (id == "jefe" && ((int)(b * 2) % 2 == 1)) fb *= 1.5;
                v += Onda("pulso", fb, t) * 0.28f * (float)Math.Exp(-2.5 * lb);
                // arpegio de acompañamiento en corcheas (combate/jefe) o semicorcheas suaves (calma)
                float ar = id == "calma" ? 0.5f : 0.5f;
                int ia = (int)(b / ar);
                int[] patron = { 0, 2, 4, 2 };
                double fa = Nota(prog[compas] + patron[ia % 4], baseNota, menor);
                float la = (b % ar) * beat;
                v += Onda(id == "calma" ? "triangulo" : "cuadrada", fa, t) * (id == "calma" ? 0.12f : 0.07f) * (float)Math.Exp(-9 * la);
                // melodía en corcheas
                int im = Math.Min(mel.Length - 1, (int)(b * 2));
                if (mel[im] != int.MinValue)
                {
                    float lm = ((b * 2) % 1f) * beat * 0.5f;
                    double fm = Nota(mel[im], baseNota, menor);
                    v += Onda(id == "calma" ? "seno" : "pulso", fm, t) * 0.2f * (float)Math.Exp(-5 * lm) * Math.Min(1f, lm / 0.006f);
                }
                // percusión (combate/jefe)
                if (id != "calma")
                {
                    float fp = b % 1f;
                    if (((int)b % 2 == 0) && fp < 0.12f) v += 0.38f * Onda("seno", 110 - 400 * fp, t) * (float)Math.Exp(-14 * fp);          // bombo
                    if (((int)b % 2 == 1) && fp < 0.1f) v += 0.16f * Ruido(ref rs) * (float)Math.Exp(-22 * fp);                              // caja
                    float fh = (b * 2) % 1f;
                    if (fh < 0.05f) v += 0.05f * Ruido(ref rs);                                                                             // charles
                }
                // fundido de los bordes para que el bucle no haga clic
                float borde = Math.Min(1f, Math.Min(t / 0.01f, (dur - t) / 0.01f));
                return v * vol * borde;
            });
        }
    }

    /// <summary>Reproduce los sonidos. Se crea sola con Iniciar().</summary>
    public static class Sonido
    {
        private static GameObject _go;
        private static AudioSource _sfx, _mus;
        private static readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private static readonly Dictionary<string, float> _ultimo = new Dictionary<string, float>();
        private static string _pista = "", _pendiente = "";
        private static readonly Dictionary<string, float[]> _musicaLista = new Dictionary<string, float[]>();
        private static readonly object _candado = new object();
        private static float _volMusObj = 0.35f, _volMus = 0f;
        public static bool Silencio;
        public static float VolSfx = 0.8f;

        public static void Iniciar()
        {
            if (_go != null) return;
            _go = new GameObject("Sonido Gatomate");
            UnityEngine.Object.DontDestroyOnLoad(_go);
            _sfx = _go.AddComponent<AudioSource>();
            _mus = _go.AddComponent<AudioSource>();
            _mus.loop = true;
            _mus.volume = 0f;
            // las músicas se calculan en segundos planos para no congelar el arranque
            foreach (string m in new[] { "calma", "combate", "jefe" })
            {
                string nombre = m;
                System.Threading.ThreadPool.QueueUserWorkItem(delegate (object o)
                {
                    float[] datos = Sintetizador.Musica(nombre);
                    lock (_candado) { _musicaLista[nombre] = datos; }
                });
            }
        }

        private static AudioClip Clip(string clave, float[] datos, int hz)
        {
            AudioClip c;
            if (_clips.TryGetValue(clave, out c)) return c;
            c = AudioClip.Create(clave, datos.Length, 1, hz, false);
            c.SetData(datos, 0);
            _clips[clave] = c;
            return c;
        }

        public static void Tocar(string id) { Tocar(id, 1f, 1f); }

        public static void Tocar(string id, float vol, float tono)
        {
            if (Silencio || _sfx == null) return;
            float t;
            // evita que el mismo sonido se apile muchas veces en el mismo instante
            if (_ultimo.TryGetValue(id, out t) && Time.realtimeSinceStartup - t < 0.035f) return;
            _ultimo[id] = Time.realtimeSinceStartup;
            AudioClip c;
            if (!_clips.TryGetValue("sfx_" + id, out c)) c = Clip("sfx_" + id, Sintetizador.Efecto(id), Sintetizador.Hz);
            _sfx.pitch = tono;
            _sfx.PlayOneShot(c, vol * VolSfx);
        }

        /// <summary>Cambia de música con fundido. "" = silencio.</summary>
        public static void Musica(string pista)
        {
            if (_mus == null || pista == _pista) return;
            _pista = pista;
            if (pista == "") { _volMusObj = 0f; _pendiente = ""; return; }
            _pendiente = pista;
            IntentarMusica();
        }

        private static void IntentarMusica()
        {
            if (_pendiente == "") return;
            float[] datos = null;
            lock (_candado) { _musicaLista.TryGetValue(_pendiente, out datos); }
            if (datos == null) return;                      // todavía se está calculando
            AudioClip c = Clip("mus_" + _pendiente, datos, Sintetizador.Hz);
            _mus.clip = c;
            _mus.Play();
            _volMusObj = 0.35f;
            _pendiente = "";
        }

        public static void Actualizar(float dt)
        {
            if (_mus == null) return;
            if (_pendiente != "") IntentarMusica();
            float obj = Silencio ? 0f : _volMusObj;
            _volMus = Mathf.MoveTowards(_volMus, obj, dt * 0.6f);
            _mus.volume = _volMus;
        }
    }
}
