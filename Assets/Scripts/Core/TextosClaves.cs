// Gatomate - núcleo del juego (sin dependencias de Unity).
// Palabras clave ("keywords") con icono: versión corta de las descripciones. La vista dibuja cada Clave como un
// icono + un texto muy corto (por ejemplo [empuje] 1). Pasar el ratón por una habilidad muestra estas claves; el clic muestra la descripción completa.
using System.Collections.Generic;

namespace Gatomate.Core
{
    /// <summary>Una palabra clave: el id de un icono ("dmg", "rango", "fuego"...) y un texto muy corto ("4+Fue", "1-3").</summary>
    public sealed class Clave
    {
        public string Icono;
        public string Texto;
        public Clave(string icono, string texto) { Icono = icono; Texto = texto ?? ""; }
        public override string ToString() { return Icono + (Texto.Length > 0 ? " " + Texto : ""); }
    }

    public static partial class Textos
    {
        /// <summary>Abreviatura de una estadística (Fue, Int, Des, Vel, Sue).</summary>
        public static string Abrev(StatTipo s)
        {
            switch (s)
            {
                case StatTipo.Fuerza: return "Fue";
                case StatTipo.Inteligencia: return "Int";
                case StatTipo.Destreza: return "Des";
                case StatTipo.Velocidad: return "Vel";
                case StatTipo.Suerte: return "Sue";
                default: return "";
            }
        }

        public static string IconoElemento(Elemento e)
        {
            switch (e)
            {
                case Elemento.Fuego: return "fuego";
                case Elemento.Hielo: return "hielo";
                case Elemento.Rayo: return "rayo";
                case Elemento.Veneno: return "veneno";
                default: return "fisico";
            }
        }

        private static string Num(HabilidadDef h)
        {
            string s = h.Poder > 0 || h.Stat == StatTipo.Ninguna ? h.Poder.ToString() : "";
            if (h.Stat != StatTipo.Ninguna) s += (s.Length > 0 ? "+" : "") + (h.Basico ? "\u00BD" : "") + Abrev(h.Stat);
            return s;
        }

        private static string RangoCorto(HabilidadDef h)
        {
            if (h.RangoMax == 0) return "0";
            if (h.RangoMax >= 50) return "\u221E";
            if (h.RangoMin == h.RangoMax || h.RangoMin <= 1 && h.RangoMax == 1) return h.RangoMax.ToString();
            if (h.RangoMin == 0) return h.RangoMax.ToString();
            return h.RangoMin + "-" + h.RangoMax;
        }

        /// <summary>Claves de una habilidad (sin el coste de maná, que la vista muestra aparte).</summary>
        public static List<Clave> Claves(HabilidadDef h)
        {
            var l = new List<Clave>();
            switch (h.Efecto)
            {
                case EfectoHab.Golpe:
                    l.Add(new Clave("dmg", Num(h)));
                    if (h.RangoMax > 0) l.Add(new Clave("rango", RangoCorto(h)));
                    break;
                case EfectoHab.AreaCruz:
                    l.Add(new Clave("dmg", Num(h)));
                    l.Add(new Clave("rango", RangoCorto(h)));
                    l.Add(new Clave("cruz", ""));
                    l.Add(new Clave("amigo", "\u00BD"));
                    break;
                case EfectoHab.AreaAlrededor:
                    l.Add(new Clave("dmg", Num(h)));
                    l.Add(new Clave("alrededor", h.Radio.ToString()));
                    l.Add(new Clave("amigo", "\u00BD"));
                    break;
                case EfectoHab.Carga:
                    l.Add(new Clave("dmg", Num(h)));
                    l.Add(new Clave("carga", RangoCorto(h)));
                    break;
                case EfectoHab.Curar:
                    l.Add(new Clave("cura", Num(h)));
                    l.Add(new Clave("rango", RangoCorto(h)));
                    if (h.QuitaEstado) l.Add(new Clave("limpiar", ""));
                    break;
                case EfectoHab.CuraArea:
                    l.Add(new Clave("cura", Num(h)));
                    l.Add(new Clave("alrededor", h.Radio.ToString()));
                    break;
                case EfectoHab.Provocar:
                    l.Add(new Clave("provocar", ""));
                    l.Add(new Clave("rango", RangoCorto(h)));
                    l.Add(new Clave("defensa", "+" + h.DanoDefensaProp));
                    break;
                case EfectoHab.ZonaLinea:
                    l.Add(new Clave("zona_" + h.Zona.ToString().ToLower(), "3x" + (h.ZonaTurnos) + "t"));
                    l.Add(new Clave("rango", RangoCorto(h)));
                    break;
                case EfectoHab.Teletransporte:
                    l.Add(new Clave("salto", RangoCorto(h)));
                    if (h.SoloVegetacion) l.Add(new Clave("hierba", ""));
                    break;
                case EfectoHab.TrasEnemigo:
                    l.Add(new Clave("salto", RangoCorto(h)));
                    l.Add(new Clave("espalda", ""));
                    break;
                case EfectoHab.Intercambio:
                    l.Add(new Clave("intercambio", RangoCorto(h)));
                    break;
                case EfectoHab.Embestida:
                    l.Add(new Clave("embestida", RangoCorto(h)));
                    break;
                case EfectoHab.Escudo:
                    l.Add(new Clave("defensa", "+" + h.AplicaValor + " " + h.AplicaTurnos + "t"));
                    l.Add(new Clave("rango", RangoCorto(h)));
                    break;
                case EfectoHab.Potenciar:
                    l.Add(new Clave("potenciar", "+" + h.AplicaValor + " " + h.AplicaTurnos + "t"));
                    l.Add(new Clave("rango", RangoCorto(h)));
                    break;
            }
            if (h.Elemento != Elemento.Fisico && h.EsAtaque) l.Add(new Clave(IconoElemento(h.Elemento), ""));
            if (h.EmpujeDist > 0) l.Add(new Clave("empuje", h.EmpujeDist.ToString()));
            if (h.BonusSiAislado > 0) l.Add(new Clave("aislado", "+" + h.BonusSiAislado));
            if (h.BonusEspalda > 0) l.Add(new Clave("espalda", "+" + h.BonusEspalda));
            if (h.Movilidad) l.Add(new Clave("libre", ""));
            return l;
        }

        /// <summary>Nombre corto de la pasiva (lo que va antes de los dos puntos en PasivaTexto).</summary>
        public static string PasivaNombre(ClaseDef c)
        {
            int i = c.PasivaTexto.IndexOf(':');
            return i > 0 ? c.PasivaTexto.Substring(0, i) : c.PasivaTexto;
        }

        /// <summary>Claves de la pasiva: por ejemplo, Puñalada trasera = [espalda +3][dmg].</summary>
        public static List<Clave> ClavesPasiva(Pasiva p)
        {
            var l = new List<Clave>();
            switch (p)
            {
                case Pasiva.Impulso: l.Add(new Clave("mov", "3+")); l.Add(new Clave("dmg", "+2")); break;
                case Pasiva.Canalizar: l.Add(new Clave("fuego", "")); l.Add(new Clave("hielo", "")); l.Add(new Clave("rayo", "")); l.Add(new Clave("dmg", "+1")); break;
                case Pasiva.Inamovible: l.Add(new Clave("empuje", "0")); l.Add(new Clave("defensa", "+1 cuerpo a cuerpo")); break;
                case Pasiva.Francotirador: l.Add(new Clave("rango", "+2")); l.Add(new Clave("rango", "min 2")); break;
                case Pasiva.Aura: l.Add(new Clave("cura", "1")); l.Add(new Clave("alrededor", "1")); break;
                case Pasiva.Apunalar: l.Add(new Clave("espalda", "+3")); l.Add(new Clave("dmg", "")); break;
                case Pasiva.HijoDelBosque: l.Add(new Clave("cura", "1")); l.Add(new Clave("hierba", "")); l.Add(new Clave("mov", "barro/agua")); break;
                case Pasiva.Maestria: l.Add(new Clave("potenciar", "+1")); l.Add(new Clave("consumible", "")); break;
            }
            return l;
        }

        /// <summary>Qué significa cada icono (se muestra al pasar el ratón por una clave). Devuelve "Nombre: explicación".</summary>
        public static string Glosario(string icono)
        {
            switch (icono)
            {
                case "dmg": return "Daño: lo que quita a la vida del objetivo (menos su defensa). 'Fue/Int/Des' = se suma esa estadística; ½ = la mitad.";
                case "rango": return "Rango: a cuántas casillas llega.";
                case "mana": return "Maná: lo que gasta usarla. Se recupera 1 por turno.";
                case "gratis": return "Gratis: no gasta maná.";
                case "libre": return "Movilidad: no gasta la acción del turno, solo maná.";
                case "empuje": return "Empuje: lo mueve esas casillas. Si choca con algo, recibe daño.";
                case "espalda": return "Espalda: daño extra si ataca al enemigo por el lado contrario a hacia donde mira.";
                case "aislado": return "Aislado: daño extra si el objetivo no tiene aliados al lado.";
                case "fuego": return "Fuego: prende la hierba alta; los slimes de hielo son débiles.";
                case "hielo": return "Hielo: ralentiza (congela si está en el agua).";
                case "rayo": return "Rayo: +3 de daño a quien está en el agua y salta por el agua.";
                case "veneno": return "Veneno: 2 de daño por turno durante 3 turnos, ignora la defensa.";
                case "fisico": return "Físico: sin elemento.";
                case "cruz": return "Cruz: golpea la casilla elegida y sus 4 vecinas.";
                case "amigo": return "Fuego amigo: el daño en área también hiere a tus otros gatos que estén dentro, pero solo la mitad. Quien lo lanza no se daña.";
                case "alrededor": return "Alrededor: afecta a todos los que estén a esa distancia de ti.";
                case "carga": return "Carga: corre en línea recta hacia el enemigo y lo golpea.";
                case "cura": return "Cura: recupera vida.";
                case "limpiar": return "Limpia: quita un estado negativo.";
                case "provocar": return "Provocar: el enemigo debe atacarte en su próximo turno.";
                case "defensa": return "Defensa: reduce el daño recibido.";
                case "potenciar": return "Potenciar: sube el daño de un aliado unos turnos.";
                case "salto": return "Salto: se teletransporta a una casilla libre.";
                case "intercambio": return "Intercambio: cambia de lugar con un aliado.";
                case "embestida": return "Avance: se mueve en línea recta por casillas libres, sin importar el terreno.";
                case "zona_fuego": return "Zona de fuego: 2 de daño al que empiece su turno ahí.";
                case "zona_enredadera": return "Enredadera: los enemigos que pasan solo se mueven 1 casilla.";
                case "zona_veneno": return "Nube de veneno: envenena a los enemigos que empiecen su turno ahí.";
                case "zona_frasco": return "Frasco: 3 de daño a enemigos y cura 3 a aliados.";
                case "hierba": return "Hierba: hierba alta o flores.";
                case "mov": return "Movimiento: casillas que te mueves.";
                case "consumible": return "Objetos: las pociones y bombas que usa.";
                default: return "";
            }
        }
    }
}
