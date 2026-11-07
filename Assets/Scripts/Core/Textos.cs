// Gatomate - núcleo del juego (sin dependencias de Unity).
// Textos provisionales (placeholders) de objetos, bendiciones y desafíos.
// La persona a cargo de estética e historia puede cambiarlos aquí sin tocar las reglas.
namespace Gatomate.Core
{
    public static partial class Textos
    {
        public static string Nombre(EfectoParty e)
        {
            switch (e)
            {
                case EfectoParty.Furia: return "Furia";
                case EfectoParty.Debilidad: return "Debilidad";
                case EfectoParty.PielDePiedra: return "Piel de piedra";
                case EfectoParty.Fragilidad: return "Fragilidad";
                case EfectoParty.PiesLigeros: return "Pies ligeros";
                case EfectoParty.Lentitud: return "Lentitud";
                case EfectoParty.MenteClara: return "Mente clara";
                default: return "Sed";
            }
        }

        public static string Descripcion(EfectoParty e)
        {
            switch (e)
            {
                case EfectoParty.Furia: return "+2 de daño infligido";
                case EfectoParty.Debilidad: return "2 menos de daño infligido";
                case EfectoParty.PielDePiedra: return "2 menos de daño recibido";
                case EfectoParty.Fragilidad: return "+2 de daño recibido";
                case EfectoParty.PiesLigeros: return "+1 casilla de movimiento";
                case EfectoParty.Lentitud: return "1 casilla menos de movimiento";
                case EfectoParty.MenteClara: return "+1 de maná al empezar el turno";
                default: return "1 de maná menos al empezar el turno";
            }
        }

        public static string Nombre(Consumible c)
        {
            switch (c)
            {
                case Consumible.PocionVida: return "Poción de vida";
                case Consumible.PocionMana: return "Poción de maná";
                case Consumible.PocionVelocidad: return "Poción de velocidad";
                case Consumible.Bomba: return "Bomba";
                case Consumible.BombaFuego: return "Bomba de fuego";
                case Consumible.BombaHielo: return "Bomba de hielo";
                case Consumible.FrascoVeneno: return "Frasco de veneno";
                default: return "Antídoto";
            }
        }

        public static string Descripcion(Consumible c)
        {
            switch (c)
            {
                case Consumible.PocionVida: return "Cura 8 de vida (a sí mismo o a un vecino)";
                case Consumible.PocionMana: return "Da 4 de maná (a sí mismo o a un vecino)";
                case Consumible.PocionVelocidad: return "+1 de movimiento y de velocidad durante 3 turnos";
                case Consumible.Bomba: return "5 de daño en la casilla elegida (rango 4) y sus 4 vecinas";
                case Consumible.BombaFuego: return "4 de fuego en la casilla elegida (rango 4) y sus 4 vecinas; prende la hierba alta";
                case Consumible.BombaHielo: return "4 de hielo en la casilla elegida (rango 4) y sus 4 vecinas; ralentiza (congela en el agua)";
                case Consumible.FrascoVeneno: return "Envenena (2 por turno, 3 turnos) en la casilla elegida (rango 4) y sus 4 vecinas";
                default: return "Quita un estado negativo (también el veneno)";
            }
        }

        public static string Nombre(EquipoTipo e) { return Objetos.Def(e).Nombre; }

        public static string Descripcion(EquipoTipo e) { return Objetos.Descripcion(e); }

        public static string Nombre(StatTipo s)
        {
            switch (s)
            {
                case StatTipo.Fuerza: return "Fuerza";
                case StatTipo.Inteligencia: return "Inteligencia";
                case StatTipo.Destreza: return "Destreza";
                case StatTipo.Velocidad: return "Velocidad";
                case StatTipo.Suerte: return "Suerte";
                default: return "Ninguna";
            }
        }

        public static string Nombre(Terreno t)
        {
            switch (t)
            {
                case Terreno.HierbaAlta: return "Hierba alta (25% de esquivar; se incendia)";
                case Terreno.Flores: return "Flores (curan 2 al empezar el turno)";
                case Terreno.Barro: return "Barro (entrar cuesta 2 de movimiento)";
                case Terreno.Agua: return "Agua (entrar cuesta 3 de movimiento; el rayo pega +3 a quien está dentro; el hielo congela)";
                case Terreno.Caja: return "Caja (se rompe atacándola; suelta monedas)";
                case Terreno.RocaFragil: return "Roca agrietada (se rompe con 8 de daño)";
                case Terreno.Barril: return "Barril explosivo (al romperse explota: 6 de daño en cruz)";
                case Terreno.Roca: return "Roca (bloquea el paso; se rompe con 16 de daño)";
                case Terreno.Muro: return "Roca grande (bloquea el paso; se rompe con 24 de daño)";
                default: return "Suelo";
            }
        }

        public static string Nombre(ZonaTipo z)
        {
            switch (z)
            {
                case ZonaTipo.Fuego: return "Fuego (2 de daño al empezar el turno)";
                case ZonaTipo.Enredadera: return "Enredadera (los enemigos del druida solo se mueven 1 casilla)";
                case ZonaTipo.Veneno: return "Nube de veneno (envenena a los enemigos al empezar su turno)";
                default: return "Frasco (3 de daño a enemigos, cura 3 a aliados)";
            }
        }

        public static string Nombre(EstadoTipo e)
        {
            switch (e)
            {
                case EstadoTipo.Provocado: return "Provocado";
                case EstadoTipo.Inmovilizado: return "Inmovilizado";
                case EstadoTipo.DefensaMas: return "Defensa +";
                case EstadoTipo.Envenenado: return "Envenenado";
                case EstadoTipo.Ralentizado: return "Ralentizado";
                case EstadoTipo.DanoMas: return "Daño +";
                default: return "Velocidad +";
            }
        }

        public static string Nombre(Elemento e)
        {
            switch (e)
            {
                case Elemento.Fuego: return "Fuego";
                case Elemento.Hielo: return "Hielo";
                case Elemento.Rayo: return "Rayo";
                case Elemento.Veneno: return "Veneno";
                default: return "Físico";
            }
        }

        public static string Efecto(Elemento e)
        {
            switch (e)
            {
                case Elemento.Fuego: return "prende la hierba alta cercana";
                case Elemento.Hielo: return "ralentiza (congela si está en el agua)";
                case Elemento.Rayo: return "+" + Config.BonusRayoEnAgua + " si el objetivo está en el agua";
                case Elemento.Veneno: return "envenena (" + Config.DanoVeneno + " por turno, " + Config.TurnosVeneno + " turnos)";
                default: return "";
            }
        }

        private static string Rango(HabilidadDef h)
        {
            if (h.RangoMax == 0) return "Alrededor";
            if (h.RangoMax >= 50) return "Cualquier distancia";
            if (h.RangoMin == h.RangoMax) return "Rango " + h.RangoMax;
            if (h.RangoMin == 0) return "Rango " + h.RangoMax;
            return "Rango " + h.RangoMin + "-" + h.RangoMax;
        }

        private static string Dano(HabilidadDef h)
        {
            string s = h.Poder.ToString();
            if (h.Stat != StatTipo.Ninguna) s += " + " + (h.Basico ? "la mitad de " : "") + Nombre(h.Stat);
            return s;
        }

        /// <summary>Descripción automática de una habilidad de gato, generada a partir de sus datos (siempre coincide con las reglas).</summary>
        public static string Descripcion(HabilidadDef h)
        {
            string s = Rango(h) + ", " + (h.CosteMana == 0 ? "gratis" : h.CosteMana + " de maná") + ". ";
            string elem = h.Elemento != Elemento.Fisico ? " de " + Nombre(h.Elemento).ToLower() : "";
            string extra = h.Elemento != Elemento.Fisico ? " (" + Efecto(h.Elemento) + ")" : "";
            string empuje = h.EmpujeDist > 0 ? "; empuja " + h.EmpujeDist + (h.EmpujeDist == 1 ? " casilla" : " casillas") + " (choca = daño)" : "";
            switch (h.Efecto)
            {
                case EfectoHab.Golpe:
                    s += "Daño " + Dano(h) + elem + empuje + extra + ".";
                    if (h.BonusSiAislado > 0) s += " +" + h.BonusSiAislado + " si el objetivo no tiene aliados vecinos.";
                    if (h.BonusEspalda > 0) s += " +" + h.BonusEspalda + " si lo golpea por la espalda.";
                    break;
                case EfectoHab.AreaCruz:
                    s += "Daño " + Dano(h) + elem + " en la casilla elegida y sus 4 vecinas" + extra + ". Fuego amigo: también hiere a tus gatos dentro del área (mitad de daño).";
                    break;
                case EfectoHab.AreaAlrededor:
                    s += "Daño " + Dano(h) + elem + " a todos los enemigos a " + h.Radio + (h.Radio == 1 ? " casilla" : " casillas") + " de distancia" + empuje + extra + ". Fuego amigo: también hiere a tus gatos dentro del área (mitad de daño).";
                    break;
                case EfectoHab.Carga:
                    s += "Corre en línea recta hacia un enemigo y lo golpea: daño " + Dano(h) + elem + empuje + ".";
                    break;
                case EfectoHab.Curar:
                    s += "Cura " + Dano(h) + " a un aliado" + (h.RangoMin == 0 ? " (o a sí mismo)" : "") + (h.QuitaEstado ? " y le quita un estado negativo" : "") + ".";
                    break;
                case EfectoHab.CuraArea:
                    s += "Cura " + Dano(h) + " a los aliados a " + h.Radio + " casillas o menos (incluido él).";
                    break;
                case EfectoHab.Provocar:
                    s += "Elige a cualquier enemigo, sin importar la distancia: debe atacarlo en su próximo turno si puede, y gana +" + h.DanoDefensaProp + " de defensa hasta su próximo turno.";
                    break;
                case EfectoHab.ZonaLinea:
                    s += "Crea 3 casillas contiguas de " + ZonaNombre(h.Zona) + " durante " + h.ZonaTurnos + " turnos.";
                    break;
                case EfectoHab.Teletransporte:
                    s += (h.SoloVegetacion ? "Se teletransporta a una casilla de hierba alta o flores" : "Se teletransporta a una casilla libre") + ".";
                    break;
                case EfectoHab.TrasEnemigo:
                    s += "Se teletransporta detrás de un enemigo (si lo ataca por la espalda hace más daño).";
                    break;
                case EfectoHab.Intercambio:
                    s += "Cambia de lugar con un aliado.";
                    break;
                case EfectoHab.Embestida:
                    s += "Avanza en línea recta por casillas libres, sin importar el terreno.";
                    break;
                case EfectoHab.Escudo:
                    s += "Un aliado gana +" + h.AplicaValor + " de defensa hasta su próximo turno.";
                    break;
                case EfectoHab.Potenciar:
                    s += "Un aliado hace +" + h.AplicaValor + " de daño durante " + h.AplicaTurnos + " turnos.";
                    break;
            }
            if (h.Movilidad) s += " No gasta la acción del turno.";
            return s;
        }

        public static string ZonaNombre(ZonaTipo z)
        {
            switch (z)
            {
                case ZonaTipo.Fuego: return "fuego";
                case ZonaTipo.Enredadera: return "enredadera";
                case ZonaTipo.Veneno: return "nube de veneno";
                default: return "frasco";
            }
        }
    }
}
