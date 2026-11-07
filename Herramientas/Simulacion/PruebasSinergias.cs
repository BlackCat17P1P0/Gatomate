// Comprobaciones de: ataque básico gratis, movimiento por clase, objetos de sinergia elemental y palabras clave.
using System;
using System.Collections.Generic;
using Gatomate.Core;

public static partial class Pruebas
{
    private static Unidad Ge(string clase, int x, int y, int id, EquipoTipo[] equipo, params string[] habs)
    {
        Gato g = G(clase, id);
        foreach (string h in habs) g.HabilidadIds.Add(h);
        foreach (EquipoTipo e in equipo) g.Equipo.Add(e);
        g.Vida = g.VidaMax;
        Unidad u = GeneradorNivel.CrearGato(g, id);
        u.Pos = new Pos(x, y);
        return u;
    }

    private static void CorrerSinergias()
    {
        Config.GrabarEventos = false;

        // ====================================================== ataque básico: todas las clases lo tienen, cuesta 0 y no es una de las 2 habilidades
        bool basicosOk = true;
        foreach (ClaseDef cd in GameData.Clases)
        {
            if (cd.Basico == null || !cd.Basico.Basico || cd.Basico.CosteMana != 0 || !cd.Basico.EsAtaque || cd.Basico.Movilidad) basicosOk = false;
            if (cd.Hab(cd.Basico.Id) != null) basicosOk = false;
            if (cd.Movimiento < 2 || cd.Movimiento > 6) basicosOk = false;
        }
        Chequear(basicosOk, "cada clase tiene un ataque básico de 0 de maná que no está en su reserva, y un movimiento entre 2 y 6");
        Chequear(GameData.Clase("picaro").Movimiento > GameData.Clase("cazador").Movimiento && GameData.Clase("luchador").Movimiento == 4, "pícaro es el que más se mueve y luchador es el medio (4)");
        int minMov = 99, cuantosMin = 0;
        foreach (ClaseDef cd in GameData.Clases) if (cd.Movimiento < minMov) minMov = cd.Movimiento;
        foreach (ClaseDef cd in GameData.Clases) if (cd.Movimiento == minMov) cuantosMin++;
        Chequear(GameData.Clase("tanque").Movimiento == minMov && cuantosMin == 1, "el tanque es el único que menos se mueve");

        // el luchador sin maná puede atacar con el básico: daño = poder + mitad de fuerza
        var luc = Ge("luchador", 3, 3, 1, new EquipoTipo[0], "zarpazo", "carga");
        var mat = En("maton", 4, 3, 101);
        var cb = Vacio(luc, mat); cb.Activa = luc; luc.Mana = 0;
        HabilidadDef zarpa = luc.Basico;
        Chequear(zarpa != null && cb.PuedeUsar(luc, zarpa) && !cb.PuedeUsar(luc, luc.Habilidades[0]), "sin maná: el básico se puede usar y las habilidades no");
        int esperado = zarpa.Poder + luc.Fuerza / 2 - mat.Defensa;
        Chequear(cb.PrevisualizarDano(luc, zarpa, mat) == esperado, "daño del básico = poder + ½ fuerza (" + esperado + ")");
        Chequear(cb.UsarHabilidad(zarpa, mat.Pos) && mat.Vida == 14 - esperado && luc.Mana == 0, "el básico hace daño y no gasta maná");
        Chequear(cb.ActuoEsteTurno && !cb.PuedeUsar(luc, zarpa), "el básico gasta la acción del turno");

        // el cazador mantiene su regla de francotirador también con el básico (no pega de frente)
        var caz = Ge("cazador", 3, 3, 2, new EquipoTipo[0]);
        var m2 = En("maton", 4, 3, 102); var m3 = En("maton", 6, 3, 103);
        var cc = Vacio(caz, m2, m3); cc.Activa = caz;
        var objs = cc.CasillasObjetivo(caz, caz.Basico);
        Chequear(!objs.Contains(m2.Pos) && objs.Contains(m3.Pos), "el básico del cazador no llega a un enemigo pegado, pero sí a distancia");

        // ====================================================== movimiento: el pícaro llega más lejos que el tanque
        var pic = Gt("picaro", 0, 0, 3); var tan = Gt("tanque", 0, 5, 4);
        var cm = Vacio(pic, tan);
        cm.Activa = pic; cm.MovimientoRestante = cm.MovimientoTotal(pic);
        int alcPic = cm.CasillasDeMovimiento().Count;
        cm.Activa = tan; cm.MovimientoRestante = cm.MovimientoTotal(tan);
        int alcTan = cm.CasillasDeMovimiento().Count;
        Chequear(alcPic > alcTan, "el área de movimiento del pícaro es mayor que la del tanque (" + alcPic + " vs " + alcTan + ")");
        var botas = Ge("tanque", 0, 0, 5, new[] { EquipoTipo.BotasCascabel });
        Chequear(Vacio(botas).MovimientoTotal(botas) == 3, "las botas de cascabel suman 1 de movimiento");
        var pv = Gt("luchador", 0, 0, 6); var cv = Vacio(pv);
        int antes = cv.MovimientoTotal(pv);
        pv.PonerEstado(EstadoTipo.VelocidadMas, 3, 1, pv);
        Chequear(cv.MovimientoTotal(pv) == antes + 1, "la poción de velocidad sigue sumando movimiento");

        // ====================================================== sinergias elementales
        // anillo de fuego: un ataque físico pasa a ser de fuego (prende la hierba alta vecina)
        var anillo = Ge("luchador", 3, 3, 7, new[] { EquipoTipo.AnilloFuego }, "zarpazo", "carga");
        var objetivo = En("maton", 4, 3, 107);
        var ca = Vacio(anillo, objetivo); ca.Activa = anillo; anillo.Mana = 3;
        ca.Tab.PonerTerreno(new Pos(5, 3), Terreno.HierbaAlta);
        Chequear(ca.ElementoDe(anillo, anillo.Basico) == Elemento.Fuego && ca.ElementoDe(anillo, anillo.Habilidades[0]) == Elemento.Fuego, "el anillo de fuego vuelve de fuego los ataques físicos");
        ca.UsarHabilidad(anillo.Basico, objetivo.Pos);
        Chequear(ca.Tab.ZonaEn(new Pos(5, 3)) != null && ca.Tab.ZonaEn(new Pos(5, 3)).Tipo == ZonaTipo.Fuego, "un ataque con anillo de fuego prende la hierba cercana");

        // anillo de hielo: ralentiza al objetivo
        var ah = Ge("luchador", 3, 3, 8, new[] { EquipoTipo.AnilloHielo });
        var ob2 = En("bruto", 4, 3, 108);
        var ch = Vacio(ah, ob2); ch.Activa = ah;
        ch.UsarHabilidad(ah.Basico, ob2.Pos);
        Chequear(ob2.Tiene(EstadoTipo.Ralentizado), "un ataque con anillo de hielo ralentiza");

        // anillo de veneno: envenena
        var av = Ge("picaro", 3, 3, 9, new[] { EquipoTipo.AnilloVeneno });
        var ob3 = En("maton", 4, 3, 109);
        var cvv = Vacio(av, ob3); cvv.Activa = av;
        cvv.UsarHabilidad(av.Basico, ob3.Pos);
        Chequear(ob3.Tiene(EstadoTipo.Envenenado), "un ataque con anillo de veneno envenena");

        // gema: +2 de daño solo con ese elemento; un ataque físico no cambia
        var mago = Ge("mago", 3, 3, 10, new EquipoTipo[0], "bola", "rayo");
        var magoG = Ge("mago", 3, 3, 11, new[] { EquipoTipo.GemaFuego }, "bola", "rayo");
        var diana = En("maton", 5, 3, 110);
        var cg = Vacio(mago, magoG, diana); cg.Activa = mago;
        int fuegoBase = cg.PrevisualizarDano(mago, Hb(mago, "bola"), diana);
        int fuegoGema = cg.PrevisualizarDano(magoG, Hb(magoG, "bola"), diana);
        int rayoBase = cg.PrevisualizarDano(mago, Hb(mago, "rayo"), diana);
        int rayoGema = cg.PrevisualizarDano(magoG, Hb(magoG, "rayo"), diana);
        Chequear(fuegoGema == fuegoBase + Config.BonusGema, "la gema de fuego suma " + Config.BonusGema + " al fuego (" + fuegoBase + " -> " + fuegoGema + ")");
        Chequear(rayoGema == rayoBase, "la gema de fuego no cambia el rayo");

        // anillo + gema a la vez: el luchador con anillo y gema de fuego pega más que con solo el anillo
        var l1 = Ge("luchador", 3, 3, 12, new[] { EquipoTipo.AnilloFuego }); var l2 = Ge("luchador", 3, 3, 13, new[] { EquipoTipo.AnilloFuego, EquipoTipo.GemaFuego });
        var dd = En("maton", 4, 3, 112);
        var cs = Vacio(l1, l2, dd);
        Chequear(cs.PrevisualizarDano(l2, l2.Basico, dd) == cs.PrevisualizarDano(l1, l1.Basico, dd) + Config.BonusGema, "anillo + gema del mismo elemento se acumulan");

        // el slime de hielo (débil al fuego) sufre más con el anillo de fuego que sin él
        var sl = En("slime_hielo", 4, 3, 113);
        var l3 = Ge("luchador", 3, 3, 14, new EquipoTipo[0]);
        var cdd = Vacio(l1, l3, sl);
        Chequear(cdd.PrevisualizarDano(l1, l1.Basico, sl) > cdd.PrevisualizarDano(l3, l3.Basico, sl), "un anillo de fuego aprovecha la debilidad de un slime de hielo");

        // la gema de fuego alarga el muro de fuego un turno
        var mg = Ge("mago", 3, 3, 15, new EquipoTipo[0], "muro_fuego", "bola"); var mg2 = Ge("mago", 3, 4, 16, new[] { EquipoTipo.GemaFuego }, "muro_fuego", "bola");
        var lejos = En("maton", 9, 9, 120); var cz = Vacio(mg, mg2, lejos); cz.Activa = mg; mg.Mana = 8; mg2.Mana = 8;
        cz.UsarHabilidad(Hb(mg, "muro_fuego"), new Pos(6, 3));
        int t1 = cz.Tab.ZonaEn(new Pos(6, 3)).Turnos;
        cz.ActuoEsteTurno = false; cz.Activa = mg2;
        cz.UsarHabilidad(Hb(mg2, "muro_fuego"), new Pos(3, 7));
        int t2 = cz.Tab.ZonaEn(new Pos(3, 7)).Turnos;
        Chequear(t2 == t1 + Config.TurnosExtraZonaGema, "la gema de fuego alarga las zonas de fuego (" + t1 + " -> " + t2 + ")");

        // los objetos nuevos aparecen en las recompensas: todos tienen nombre y descripción propios
        bool textosOk = true; var nombres = new HashSet<string>();
        foreach (EquipoTipo e in Enum.GetValues(typeof(EquipoTipo)))
        {
            string n = Textos.Nombre(e), d = Textos.Descripcion(e);
            if (string.IsNullOrEmpty(n) || string.IsNullOrEmpty(d) || !nombres.Add(n)) textosOk = false;
        }
        Chequear(textosOk && nombres.Count == Enum.GetValues(typeof(EquipoTipo)).Length, "cada objeto tiene un nombre y una descripción distintos");

        // ====================================================== palabras clave
        bool clavesOk = true;
        foreach (ClaseDef cd2 in GameData.Clases)
        {
            var todas = new List<HabilidadDef>(cd2.Pool); todas.Add(cd2.Basico);
            foreach (HabilidadDef h in todas)
            {
                var cl = Textos.Claves(h);
                if (cl.Count == 0) clavesOk = false;
                foreach (Clave k in cl) if (string.IsNullOrEmpty(Textos.Glosario(k.Icono))) { clavesOk = false; Console.WriteLine("  sin glosario: " + k.Icono + " en " + h.Id); }
            }
            foreach (Clave k in Textos.ClavesPasiva(cd2.Pasiva)) if (string.IsNullOrEmpty(Textos.Glosario(k.Icono))) { clavesOk = false; Console.WriteLine("  sin glosario (pasiva): " + k.Icono); }
            if (Textos.ClavesPasiva(cd2.Pasiva).Count == 0 || string.IsNullOrEmpty(Textos.PasivaNombre(cd2))) clavesOk = false;
        }
        Chequear(clavesOk, "toda habilidad y pasiva tiene claves con icono y cada icono tiene su explicación");
        var punal = GameData.Clase("picaro").Hab("punalada");
        string fila = ""; foreach (Clave k in Textos.Claves(punal)) fila += k + "|";
        Chequear(fila.Contains("dmg 3+Des") && fila.Contains("aislado +3"), "claves de la Puñalada: " + fila);
        string pn = ""; foreach (Clave k in Textos.ClavesPasiva(Pasiva.Apunalar)) pn += k + "|";
        Chequear(Textos.PasivaNombre(GameData.Clase("picaro")) == "Puñalada trasera" && pn.StartsWith("espalda +3"), "pasiva del pícaro = 'Puñalada trasera' + espalda +3 + daño");
    }

    private static void CorrerTutorialYGuardado()
    {
        Config.GrabarEventos = false;

        // ====================================================== guardar y cargar
        Run a = new Run(321);
        a.ElegirHabilidadesAutomatico();
        a.Dinero = 9; a.AgregarConsumible(Consumible.Bomba); a.AgregarEfecto(EfectoParty.Furia, 5);
        a.EquipoSinAsignar.Add(EquipoTipo.AnilloHielo);
        a.Expedicion[0].Equipo.Add(EquipoTipo.GemaRayo);
        a.Expedicion[1].Vida = 3;
        string txt = a.Serializar();
        Run b = Run.Cargar(txt);
        Chequear(txt != null && b != null, "una run en la preparación se guarda y se carga");
        if (b != null)
        {
            Chequear(b.Semilla == a.Semilla && b.Dinero == 9 && b.Vidas == a.Vidas && b.Mundo == a.Mundo && b.Nivel == a.Nivel && b.Fase == FaseRun.Preparacion, "la run cargada conserva semilla, monedas, vidas y nivel");
            bool gatosOk = b.Expedicion.Count == a.Expedicion.Count && b.Reserva.Count == a.Reserva.Count;
            for (int i = 0; gatosOk && i < a.Expedicion.Count; i++)
                gatosOk = a.Expedicion[i].ClaseId == b.Expedicion[i].ClaseId && a.Expedicion[i].Vida == b.Expedicion[i].Vida
                    && Lista(a.Expedicion[i].HabilidadIds) == Lista(b.Expedicion[i].HabilidadIds) && Lista(a.Expedicion[i].Equipo) == Lista(b.Expedicion[i].Equipo);
            Chequear(gatosOk, "los gatos conservan clase, vida, habilidades y equipo");
            Chequear(b.Inventario[Consumible.Bomba] == a.Inventario[Consumible.Bomba] && b.EquipoSinAsignar.Contains(EquipoTipo.AnilloHielo) && b.Efectos.Count == 1 && b.Efectos[0].Tipo == EfectoParty.Furia, "inventario, objetos sin asignar y bendiciones se conservan");
            Chequear(b.Serializar() == txt, "guardar, cargar y volver a guardar da el mismo texto");
            // la partida cargada sigue igual que la original: mismo combate y misma secuencia de azar
            a.Empezar(); b.Empezar();
            BotJugador.JugarCombate(a.Combate); BotJugador.JugarCombate(b.Combate);
            Chequear(a.Combate.Estado == b.Combate.Estado && a.Combate.Ronda == b.Combate.Ronda && a.Azar.Estado == b.Azar.Estado, "una partida cargada sigue exactamente igual que la original");
        }
        Chequear(Run.Cargar("basura") == null && Run.Cargar("") == null && Run.Cargar(txt.Replace("mundo=1", "mundo=99")) == null, "un guardado roto no se carga (devuelve null)");
        Run c = new Run(5);
        Chequear(c.Serializar() == null, "no se guarda mientras se eligen habilidades");

        // ====================================================== tutorial
        Run t = Run.NuevoTutorial();
        Chequear(t.EsTutorial && t.Expedicion.Count == 4 && t.Reserva.Count == 4 && t.Fase == FaseRun.ElegirHabilidad && t.Pendientes.Count == 2, "el tutorial empieza con 8 gatos y el Luchador eligiendo 2 habilidades");
        bool paso = true;
        foreach (string clave in new[] { "hab", "prep1", "combate1", "recompensa1", "tienda1", "prep2", "combate2", "recompensa2", "tienda2", "prep3", "combate3", "fin" })
            if (Tutorial.Pasos(clave).Count == 0) { paso = false; Console.WriteLine("  sin pasos: " + clave); }
        Chequear(paso, "hay pasos de tutorial para cada momento");
        bool tabsOk = true;
        for (int n = 1; n <= 3; n++)
        {
            Run rn = Run.NuevoTutorial(); rn.ElegirHabilidadesAutomatico();
            Combate cb = GeneradorNivel.CrearTutorial(n, rn.Expedicion, new List<EfectoActivo>(), rn.Inventario);
            if (!cb.Tab.EsConexo() || cb.Vivos(Bando.Gatos).Count != 4 || cb.Vivos(Bando.Enemigos).Count != 3) tabsOk = false;
            foreach (Unidad u in cb.Unidades) if (!cb.Tab.Pasable(u.Pos) && true) { tabsOk = false; Console.WriteLine("  unidad en casilla bloqueada nivel " + n + ": " + u); }
        }
        Chequear(tabsOk, "los 3 tableros del tutorial son conexos, con 4 gatos y 3 enemigos sobre casillas pisables");
        t.ElegirHabilidadesAutomatico();
        Chequear(t.Fase == FaseRun.Preparacion && t.Expedicion[0].HabilidadIds.Count == 2 && t.Expedicion[1].HabilidadIds.Count == 2, "tras elegir, el tutorial pasa a la preparación con todos los gatos con 2 habilidades");
        Chequear(t.Equipar(t.Expedicion[0], EquipoTipo.CollarFuerza) && t.Expedicion[0].Equipo.Count == 1, "se puede equipar el collar de fuerza del tutorial");
        Chequear(t.Intercambiar(t.Expedicion[3], t.Reserva[0]), "se puede intercambiar un gato con la reserva");

        // recorrer los 3 niveles con el bot: el tutorial se puede superar y no gasta vidas
        Run tb = Run.NuevoTutorial(); tb.ElegirHabilidadesAutomatico();
        int guardia = 0; bool tiendaOk = false;
        while (tb.Fase != FaseRun.CampanaCompletada && guardia++ < 60)
        {
            switch (tb.Fase)
            {
                case FaseRun.Preparacion: tb.Empezar(); BotJugador.JugarCombate(tb.Combate); tb.ResolverFinDeCombate(); break;
                case FaseRun.Recompensa: BotJugador.ElegirDesafio(tb); break;
                case FaseRun.Tienda:
                    {
                        tiendaOk = tb.Dinero >= Run.MonedasMinimasTiendaTutorial && tb.Tienda[3].EsEquipo && tb.Comprar(3);
                        tb.SalirDeTienda();
                        break;
                    }
                default: guardia = 999; break;
            }
        }
        Chequear(tb.Fase == FaseRun.CampanaCompletada && tb.Vidas == Config.VidasPorRun && tiendaOk, "el bot supera el tutorial de 3 niveles sin perder vidas, y la tienda vende un objeto (guardia " + guardia + ", fase " + tb.Fase + ")");
        // el tutorial también se guarda y se carga
        Run tc = Run.NuevoTutorial(); tc.ElegirHabilidadesAutomatico();
        Run td = Run.Cargar(tc.Serializar());
        Chequear(td != null && td.EsTutorial, "el tutorial se guarda y se carga");
    }

    private static string Lista<T>(IEnumerable<T> l) { return string.Join(";", new List<T>(l).ConvertAll(x => x.ToString()).ToArray()); }
}
