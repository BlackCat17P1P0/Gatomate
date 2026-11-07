// Gatomate - núcleo del juego (sin dependencias de Unity).
// Niveles de los gatos (propuesto, por confirmar): ganan experiencia al derrotar enemigos; cada nivel da vida y una estadística
// de su clase, y en los niveles 3 y 6 pueden aprender una habilidad más (de las 6 de su clase).
using System;
using System.Collections.Generic;

namespace Gatomate.Core
{
    public static class Progreso
    {
        /// <summary>Experiencia que hace falta para pasar del nivel 'n' al siguiente.</summary>
        public static int XpParaSubir(int n) { return 5 + 2 * n; }

        /// <summary>Habilidades que debe tener un gato de ese nivel.</summary>
        public static int HabilidadesPara(int nivel)
        {
            int n = Config.HabilidadesPorGato;
            foreach (int l in Config.NivelesHabilidadExtra) if (nivel >= l) n++;
            return n;
        }

        /// <summary>Estadística que más crece en la clase: la de su ataque básico.</summary>
        public static StatTipo StatPrincipal(ClaseDef c)
        {
            return c.Basico != null && c.Basico.Stat != StatTipo.Ninguna ? c.Basico.Stat : StatTipo.Fuerza;
        }

        /// <summary>Suma a las estadísticas base lo que dan los niveles 2..nivel: +2 de vida y +1 a la estadística principal por nivel;
        /// +1 de defensa en los niveles 4 y 8; +1 de maná en los niveles 5 y 9.</summary>
        public static void AplicarNivel(Stats s, ClaseDef c, int nivel)
        {
            StatTipo principal = StatPrincipal(c);
            for (int k = 2; k <= nivel; k++)
            {
                s.Vida += 2;
                switch (principal)
                {
                    case StatTipo.Fuerza: s.Fuerza++; break;
                    case StatTipo.Inteligencia: s.Inteligencia++; break;
                    case StatTipo.Destreza: s.Destreza++; break;
                }
                if (k % 4 == 0) s.Defensa++;
                if (k == 5 || k == 9) s.Mana++;
            }
        }

        /// <summary>Suma experiencia. Devuelve los niveles nuevos que alcanzó (vacío si no subió).</summary>
        public static List<int> GanarXp(Gato g, int xp)
        {
            var subidos = new List<int>();
            if (xp <= 0) return subidos;
            g.Xp += xp;
            while (g.Nivel < Config.NivelMaximo && g.Xp >= XpParaSubir(g.Nivel))
            {
                g.Xp -= XpParaSubir(g.Nivel);
                g.Nivel++;
                g.Vida += 2;                // sube la vida máxima y la actual
                subidos.Add(g.Nivel);
            }
            if (g.Nivel >= Config.NivelMaximo) g.Xp = 0;
            return subidos;
        }

        /// <summary>Pone el nivel de un gato nuevo (la vida queda al máximo).</summary>
        public static void FijarNivel(Gato g, int nivel)
        {
            g.Nivel = Math.Max(1, Math.Min(Config.NivelMaximo, nivel));
            g.Xp = 0;
            g.Vida = g.VidaMax;
        }
    }
}
