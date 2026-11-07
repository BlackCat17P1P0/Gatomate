// Gatomate - núcleo del juego (sin dependencias de Unity).
// Guardar y cargar la partida como texto. La vista lo guarda (PlayerPrefs) cada vez que se llega a la preparación de un nivel,
// que es un punto tranquilo: no hay combate en curso, ni elecciones pendientes, ni tienda abierta.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Gatomate.Core
{
    public sealed partial class Run
    {
        private const string CabeceraGuardado = "GATOMATE-GUARDADO-2";

        /// <summary>Run vacía para cargar una partida (no sortea nada).</summary>
        private Run(int semilla, bool vacia)
        {
            Semilla = semilla;
            Azar = new Azar(semilla);
            foreach (Consumible c in Enum.GetValues(typeof(Consumible))) Inventario[c] = 0;
        }

        /// <summary>true si la partida se puede guardar ahora (solo en la preparación de un nivel).</summary>
        public bool PuedeGuardar { get { return Fase == FaseRun.Preparacion && Combate == null && Pendientes.Count == 0; } }

        private static string Limpio(string s) { return (s ?? "").Replace("|", " ").Replace("\n", " ").Replace("\r", " ").Replace(";", ",").Replace("=", ":"); }

        private static string Lista<T>(IEnumerable<T> l)
        {
            var sb = new StringBuilder();
            foreach (T x in l) { if (sb.Length > 0) sb.Append(';'); sb.Append(x.ToString()); }
            return sb.ToString();
        }

        private void EscribirGato(StringBuilder sb, Gato g, bool reserva)
        {
            sb.Append("gato=").Append(reserva ? "R" : "E").Append('|').Append(g.Id).Append('|').Append(g.ClaseId).Append('|').Append(Limpio(g.Nombre)).Append('|')
              .Append(g.Vida).Append('|').Append(Lista(g.Equipo)).Append('|').Append(Lista(g.HabilidadIds)).Append('|').Append(g.Nivel).Append('|').Append(g.Xp).Append('\n');
        }

        /// <summary>El estado de la run como texto, o null si ahora no es un buen momento para guardar.</summary>
        public string Serializar()
        {
            if (Config.Medidor == null) return SerializarInterno();
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            string r = SerializarInterno();
            Config.Medidor(Config.MedSerializar, System.Diagnostics.Stopwatch.GetTimestamp() - t0);
            return r;
        }

        private string SerializarInterno()
        {
            if (!PuedeGuardar) return null;
            var sb = new StringBuilder();
            var ci = CultureInfo.InvariantCulture;
            sb.Append(CabeceraGuardado).Append('\n');
            sb.Append("semilla=").Append(Semilla.ToString(ci)).Append('\n');
            sb.Append("azar=").Append(Azar.Estado.ToString(ci)).Append('\n');
            sb.Append("mundo=").Append(Mundo).Append('\n').Append("nivel=").Append(Nivel).Append('\n').Append("intento=").Append(Intento).Append('\n');
            sb.Append("vidas=").Append(Vidas).Append('\n').Append("dinero=").Append(Dinero).Append('\n');
            sb.Append("tutorial=").Append(EsTutorial ? 1 : 0).Append('\n');
            sb.Append("mundosJugables=").Append(Config.MundosJugables).Append('\n');
            sb.Append("sigGato=").Append(_sigGatoId).Append('\n');
            sb.Append("pendientes=").Append(Lista(_clasesPendientes)).Append('\n');
            var inv = new List<string>();
            foreach (var kv in Inventario) if (kv.Value > 0) inv.Add(kv.Key + ":" + kv.Value);
            sb.Append("inventario=").Append(Lista(inv)).Append('\n');
            sb.Append("equipo=").Append(Lista(EquipoSinAsignar)).Append('\n');
            var ef = new List<string>();
            foreach (EfectoActivo e in Efectos) ef.Add(e.Tipo + ":" + e.Turnos);
            sb.Append("efectos=").Append(Lista(ef)).Append('\n');
            foreach (Gato g in Expedicion) EscribirGato(sb, g, false);
            foreach (Gato g in Reserva) EscribirGato(sb, g, true);
            foreach (RegistroNivel r in Historial)
                sb.Append("hist=").Append(r.Mundo).Append('|').Append(r.Nivel).Append('|').Append(r.Semilla).Append('|').Append(r.Intento).Append('|')
                  .Append(Limpio(r.Resultado)).Append('|').Append(r.VidasRestantes).Append('|').Append(r.Rondas).Append('|').Append(Limpio(r.Desafio)).Append('\n');
            return sb.ToString();
        }

        private static List<T> LeerEnums<T>(string s) where T : struct
        {
            var l = new List<T>();
            if (string.IsNullOrEmpty(s)) return l;
            foreach (string p in s.Split(';'))
            {
                T v;
                if (!Enum.TryParse<T>(p, out v)) throw new FormatException("valor desconocido: " + p);
                l.Add(v);
            }
            return l;
        }

        /// <summary>Reconstruye una run desde el texto guardado. Devuelve null si el texto no es válido (archivo roto o de otra versión).</summary>
        public static Run Cargar(string texto)
        {
            if (Config.Medidor == null) return CargarInterno(texto);
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            Run r = CargarInterno(texto);
            Config.Medidor(Config.MedCargar, System.Diagnostics.Stopwatch.GetTimestamp() - t0);
            return r;
        }

        private static Run CargarInterno(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return null;
            try
            {
                var ci = CultureInfo.InvariantCulture;
                string[] lineas = texto.Split('\n');
                if (lineas.Length == 0 || lineas[0].Trim() != CabeceraGuardado) return null;
                var kv = new Dictionary<string, string>();
                var gatos = new List<string>(); var hist = new List<string>();
                for (int i = 1; i < lineas.Length; i++)
                {
                    string l = lineas[i].TrimEnd('\r');
                    int ig = l.IndexOf('=');
                    if (ig <= 0) continue;
                    string k = l.Substring(0, ig), v = l.Substring(ig + 1);
                    if (k == "gato") gatos.Add(v); else if (k == "hist") hist.Add(v); else kv[k] = v;
                }
                Run r = new Run(int.Parse(kv["semilla"], ci), true);
                r.Azar.Estado = uint.Parse(kv["azar"], ci);
                r.Mundo = int.Parse(kv["mundo"], ci); r.Nivel = int.Parse(kv["nivel"], ci); r.Intento = int.Parse(kv["intento"], ci);
                r.Vidas = int.Parse(kv["vidas"], ci); r.Dinero = int.Parse(kv["dinero"], ci);
                r.EsTutorial = kv["tutorial"] == "1";
                string mj;
                if (kv.TryGetValue("mundosJugables", out mj) && !r.EsTutorial) Config.MundosJugables = Math.Max(1, Math.Min(GameData.Mundos.Count, int.Parse(mj, ci)));
                r._sigGatoId = int.Parse(kv["sigGato"], ci);
                if (r.Mundo < 1 || r.Mundo > GameData.Mundos.Count || r.Nivel < 1 || r.Nivel > Config.NivelesPorMundo) return null;
                foreach (string c in kv["pendientes"].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!GameData.ClasePorId.ContainsKey(c)) return null;
                    r._clasesPendientes.Add(c);
                }
                foreach (string par in kv["inventario"].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] p = par.Split(':');
                    Consumible cons;
                    if (!Enum.TryParse<Consumible>(p[0], out cons)) return null;
                    r.Inventario[cons] = int.Parse(p[1], ci);
                }
                r.EquipoSinAsignar.AddRange(LeerEnums<EquipoTipo>(kv["equipo"]));
                foreach (string par in kv["efectos"].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] p = par.Split(':');
                    EfectoParty t;
                    if (!Enum.TryParse<EfectoParty>(p[0], out t)) return null;
                    r.Efectos.Add(new EfectoActivo { Tipo = t, Turnos = int.Parse(p[1], ci) });
                }
                foreach (string lg in gatos)
                {
                    string[] p = lg.Split('|');
                    if (!GameData.ClasePorId.ContainsKey(p[2])) return null;
                    var g = new Gato { Id = int.Parse(p[1], ci), ClaseId = p[2], Nombre = p[3], Vida = int.Parse(p[4], ci) };
                    g.Equipo.AddRange(LeerEnums<EquipoTipo>(p[5]));
                    if (p.Length > 8) { g.Nivel = Math.Max(1, Math.Min(Config.NivelMaximo, int.Parse(p[7], ci))); g.Xp = int.Parse(p[8], ci); }
                    foreach (string h in p[6].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (g.Clase.Hab(h) == null) return null;
                        g.HabilidadIds.Add(h);
                    }
                    if (p[0] == "E") r.Expedicion.Add(g); else r.Reserva.Add(g);
                }
                if (r.Expedicion.Count == 0) return null;
                foreach (string lh in hist)
                {
                    string[] p = lh.Split('|');
                    r.Historial.Add(new RegistroNivel
                    {
                        Mundo = int.Parse(p[0], ci), Nivel = int.Parse(p[1], ci), Semilla = int.Parse(p[2], ci), Intento = int.Parse(p[3], ci),
                        Resultado = p[4], VidasRestantes = int.Parse(p[5], ci), Rondas = int.Parse(p[6], ci), Desafio = p.Length > 7 ? p[7] : ""
                    });
                }
                r.GuardarEstado();
                r.Fase = FaseRun.Preparacion;
                return r;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
