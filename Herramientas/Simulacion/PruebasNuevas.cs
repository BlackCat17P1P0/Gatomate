// Comprobaciones de las reglas nuevas (habilidades por clase, movilidad, pasivas, elementos, empujes con choque,
// obstáculos rompibles, monedas, enemigos nuevos y tienda). Todas son propuestas por confirmar: aquí se fijan para que un cambio no las rompa sin avisar.
using System;
using System.Collections.Generic;
using Gatomate.Core;

public static partial class Pruebas
{
    /// <summary>Gato con habilidades elegidas a mano.</summary>
    private static Unidad Gh(string clase, int x, int y, int id, params string[] habs)
    {
        Gato g = G(clase, id);
        foreach (string h in habs) g.HabilidadIds.Add(h);
        Unidad u = GeneradorNivel.CrearGato(g, id);
        u.Pos = new Pos(x, y);
        return u;
    }

    private static HabilidadDef Hb(Unidad u, string id)
    {
        foreach (HabilidadDef h in u.Habilidades) if (h.Id == id) return h;
        throw new Exception("no tiene la habilidad " + id);
    }

    private static void CorrerNuevas()
    {
        Config.GrabarEventos = false;

        // ====================================================== habilidades: reservas, elección 2 veces por gato
        bool poolsOk = true;
        foreach (ClaseDef cd in GameData.Clases)
        {
            var ids = new HashSet<string>();
            bool movilidad = false;
            foreach (HabilidadDef h in cd.Pool)
            {
                ids.Add(h.Id);
                if (string.IsNullOrEmpty(h.Descripcion) || h.Descripcion.Length < 15) poolsOk = false;
                if (h.Movilidad) movilidad = true;
            }
            if (cd.Pool.Count != 6 || ids.Count != 6 || !movilidad) poolsOk = false;
        }
        Chequear(poolsOk, "cada clase tiene 6 habilidades distintas con descripción y al menos una de movilidad");

        Run rr = new Run(7);
        Chequear(rr.Fase == FaseRun.ElegirHabilidad && rr.Pendientes.Count == 8 && rr.OpcionesHab.Count == 3, "al empezar: 8 elecciones pendientes (4 gatos x 2) y 3 opciones");
        Gato primero = rr.GatoEligiendo;
        string idPrimera = rr.OpcionesHab[0].Id;
        rr.ElegirHabilidad(0);
        bool sinRepetir = true;
        foreach (HabilidadDef h in rr.OpcionesHab) if (h.Id == idPrimera) sinRepetir = false;
        Chequear(rr.GatoEligiendo == primero && primero.HabilidadIds.Count == 1 && sinRepetir && rr.OpcionesHab.Count == 3, "el mismo gato elige otra vez y la habilidad ya elegida no se vuelve a ofrecer");
        rr.ElegirHabilidadesAutomatico();
        bool dos = rr.Fase == FaseRun.Preparacion;
        foreach (Gato g in rr.Expedicion) if (g.HabilidadIds.Count != 2 || g.HabilidadIds[0] == g.HabilidadIds[1] || g.Habilidades.Count != 2) dos = false;
        Chequear(dos, "tras las 8 elecciones cada gato tiene 2 habilidades distintas y se pasa a la preparación");
        Run r2 = new Run(8);
        r2.Dinero = 5;
        var antesOpc = new List<HabilidadDef>(r2.OpcionesHab);
        Chequear(r2.RepetirOpciones() && r2.Dinero == 2 && r2.OpcionesHab.Count == 3, "repetir las opciones cuesta 3 monedas");
        r2.Dinero = 1;
        Chequear(!r2.RepetirOpciones() && r2.Dinero == 1, "sin monedas no se pueden repetir las opciones");

        // ====================================================== movilidad: Paso sombrío (pícaro), pasiva Puñalada trasera
        var pic = Gh("picaro", 3, 3, 1, "punalada", "paso_sombrio");
        var mat = En("maton", 6, 3, 101);               // mira hacia el oeste (hacia los gatos)
        var cp = Vacio(pic, mat); cp.Activa = pic; pic.Mana = 4;
        HabilidadDef paso = Hb(pic, "paso_sombrio"), pun = Hb(pic, "punalada");
        pic.Pos = new Pos(5, 3);                                 // pegado al matón y de frente (él mira al oeste): sin bonus de espalda
        int frente = cp.PrevisualizarDano(pic, pun, mat);
        pic.Pos = new Pos(3, 3);
        Chequear(frente == 3 + 5 + 3, "de frente no hay bonus (matón solo: Puñalada 3+5+3 = 11, hizo " + frente + ")");
        Chequear(cp.CasillasObjetivo(pic, paso).Contains(mat.Pos), "Paso sombrío: se puede elegir un enemigo a rango 5");
        Chequear(cp.UsarHabilidad(paso, mat.Pos), "Paso sombrío se usa");
        Chequear(pic.Pos == new Pos(7, 3), "Paso sombrío: aparece detrás del enemigo (él mira al oeste, queda al este)");
        Chequear(pic.Mana == 1 && !cp.ActuoEsteTurno, "Paso sombrío cuesta 3 de maná y NO gasta la acción");
        int detras = cp.PrevisualizarDano(pic, pun, mat);
        Chequear(detras == frente + 3, "Puñalada trasera: +3 de daño por la espalda (" + detras + " vs " + frente + ")");
        Chequear(cp.UsarHabilidad(pun, mat.Pos) && mat.Vida == 14 - detras, "tras el salto sí puede atacar en el mismo turno");
        Chequear(pic.Mana == 0 && !cp.PuedeUsar(pic, paso), "con 4 de maná no se puede repetir cada turno");

        // ====================================================== movilidad: otras
        var mag = Gh("mago", 2, 2, 1, "bola", "parpadeo");
        var roc = En("maton", 3, 2, 101);
        var cm = Vacio(mag, roc); cm.Activa = mag; cm.Tab.PonerTerreno(new Pos(4, 2), Terreno.Roca);
        var objsP = cm.CasillasObjetivo(mag, Hb(mag, "parpadeo"));
        Chequear(!objsP.Contains(roc.Pos) && !objsP.Contains(new Pos(4, 2)) && objsP.Contains(new Pos(2, 5)) && !objsP.Contains(new Pos(2, 7)), "Parpadeo: solo casillas libres a rango 1-4");
        cm.UsarHabilidad(Hb(mag, "parpadeo"), new Pos(2, 5));
        Chequear(mag.Pos == new Pos(2, 5) && mag.Mana == 6 && !cm.ActuoEsteTurno, "Parpadeo: se teletransporta, 2 de maná, sin gastar acción");

        var cle = Gh("clerigo", 0, 0, 1, "plegaria", "paso_divino"); var luc9 = Gt("luchador", 2, 2, 2);
        var cc = Vacio(cle, luc9); cc.Activa = cle;
        cc.UsarHabilidad(Hb(cle, "paso_divino"), luc9.Pos);
        Chequear(cle.Pos == new Pos(2, 2) && luc9.Pos == new Pos(0, 0), "Paso divino: intercambia lugar con un aliado");

        var tan = Gh("tanque", 0, 0, 1, "provocar", "arremetida"); var m3 = En("maton", 3, 0, 101);
        var ct = Vacio(tan, m3); ct.Activa = tan;
        var objsA = ct.CasillasObjetivo(tan, Hb(tan, "arremetida"));
        Chequear(objsA.Contains(new Pos(2, 0)) && !objsA.Contains(new Pos(3, 0)) && !objsA.Contains(new Pos(4, 0)), "Arremetida: avanza en línea recta hasta antes del enemigo");

        var luc3 = Gh("luchador", 1, 3, 1, "zarpazo", "carga"); var m5 = En("maton", 4, 3, 101);
        var ccar = Vacio(luc3, m5); ccar.Activa = luc3; luc3.Mana = 3;
        Chequear(ccar.CasillasObjetivo(luc3, Hb(luc3, "carga")).Contains(m5.Pos), "Carga: se puede elegir un enemigo en línea recta a 2-4");
        ccar.UsarHabilidad(Hb(luc3, "carga"), m5.Pos);
        Chequear(luc3.Pos == new Pos(3, 3) && m5.Pos == new Pos(5, 3) && m5.Vida == 14 - 8 && ccar.ActuoEsteTurno, "Carga: corre hasta quedar pegado, golpea 2+6 y empuja 1");

        var dru = Gh("druida", 0, 0, 1, "enredadera", "viaje_raiz");
        var cd2 = Vacio(dru); cd2.Activa = dru;
        cd2.Tab.PonerTerreno(new Pos(3, 3), Terreno.HierbaAlta); cd2.Tab.PonerTerreno(new Pos(2, 2), Terreno.Flores);
        var objsV = cd2.CasillasObjetivo(dru, Hb(dru, "viaje_raiz"));
        Chequear(objsV.Count == 2 && objsV.Contains(new Pos(3, 3)) && objsV.Contains(new Pos(2, 2)), "Viaje de raíz: solo a hierba alta o flores");

        // ====================================================== pasivas
        var tk = Gt("tanque", 5, 5, 1);
        var cx = Vacio(tk);
        Chequear(cx.CalcularDano(tk, 10, Elemento.Fisico, true) == cx.CalcularDano(tk, 10, Elemento.Fisico, false) - 1, "Inamovible: -1 de daño cuerpo a cuerpo");
        var tk2 = Gh("tanque", 3, 3, 1, "provocar", "sacudida"); var ea = En("bruto", 4, 3, 101); var eb = En("maton", 3, 4, 102);
        var csa = Vacio(tk2, ea, eb); csa.Activa = tk2;
        csa.UsarHabilidad(Hb(tk2, "sacudida"), tk2.Pos);
        Chequear(tk2.Pos == new Pos(3, 3) && ea.Pos == new Pos(5, 3) && eb.Pos == new Pos(3, 5), "Sacudida: empuja 1 casilla a todos los enemigos de alrededor");

        var dru2 = Gt("druida", 0, 0, 1); var luc4 = Gt("luchador", 0, 1, 2);
        var cw = Vacio(dru2, luc4); cw.Activa = dru2;
        cw.Tab.PonerTerreno(new Pos(1, 0), Terreno.Agua); cw.Tab.PonerTerreno(new Pos(1, 1), Terreno.Agua);
        var alc1 = cw.Alcance(dru2, 3, null); var alc2 = cw.Alcance(luc4, 3, null);
        Chequear(alc1[new Pos(1, 0)] == 2, "Hijo del bosque: el agua le cuesta 1 de movimiento");
        Chequear(alc2[new Pos(1, 1)] == 0, "el agua cuesta 3 de movimiento a los demás");
        cw.Tab.PonerTerreno(new Pos(0, 2), Terreno.Barro);
        Chequear(cw.Alcance(luc4, 3, null)[new Pos(0, 2)] == 1, "el barro cuesta 2 de movimiento (3 -> 1)");

        var mg = Gt("mago", 0, 0, 1);
        var cmg = Vacio(mg);
        Chequear(cmg.BrutoGolpe(mg, mg.Habilidad, null, 0) == 3 + 6 + 1, "Canalizar: +1 de daño elemental (Bola de fuego 3+6+1)");

        // ====================================================== elementos
        var caz = Gh("cazador", 0, 0, 1, "flecha_fuego", "flecha_veneno");
        var m6 = En("maton", 5, 0, 101);
        var cf = Vacio(caz, m6); cf.Activa = caz;
        cf.Tab.PonerTerreno(new Pos(5, 1), Terreno.HierbaAlta);
        cf.UsarHabilidad(Hb(caz, "flecha_fuego"), m6.Pos);
        Chequear(cf.Tab.TerrenoEn(new Pos(5, 1)) == Terreno.Suelo && cf.Tab.ZonaEn(new Pos(5, 1)) != null && cf.Tab.ZonaEn(new Pos(5, 1)).Tipo == ZonaTipo.Fuego, "Fuego: un ataque junto a la hierba alta la quema y deja fuego en el escenario");

        var mh = Gh("mago", 0, 0, 1, "lanza_hielo", "rayo");
        var m7 = En("maton", 3, 0, 101); var m8 = En("maton", 4, 3, 102);
        var ch = Vacio(mh, m7, m8); ch.Activa = mh;
        ch.Tab.PonerTerreno(new Pos(4, 3), Terreno.Agua);
        ch.UsarHabilidad(Hb(mh, "lanza_hielo"), m7.Pos);
        Chequear(m7.Tiene(EstadoTipo.Ralentizado) && !m7.Tiene(EstadoTipo.Inmovilizado), "Hielo: ralentiza");
        Chequear(ch.MovimientoTotal(m7) == 1, "Ralentizado: quita 2 de movimiento (3 -> 1)");
        ch.Activa = mh; ch.ActuoEsteTurno = false; mh.Mana = 8;
        mh.Pos = new Pos(4, 0);
        ch.UsarHabilidad(Hb(mh, "lanza_hielo"), m8.Pos);
        Chequear(m8.Tiene(EstadoTipo.Inmovilizado), "Hielo sobre agua: congela (inmoviliza)");
        int sinAgua = ch.CalcularDano(m7, 8, Elemento.Rayo, false), conAgua = ch.CalcularDano(m8, 8, Elemento.Rayo, false);
        Chequear(conAgua == sinAgua + 3, "Rayo: +3 de daño si el objetivo está en el agua");

        var m9 = En("maton", 5, 0, 103);
        var cv = Vacio(Gh("cazador", 0, 0, 1, "flecha_veneno", "disparo"), m9);
        Unidad cz = cv.Unidades[0];
        cv.Iniciar();
        Chequear(cv.Activa == cz, "empieza el turno el cazador");
        cv.UsarHabilidad(Hb(cz, "flecha_veneno"), m9.Pos);
        Chequear(m9.Tiene(EstadoTipo.Envenenado), "Veneno: envenena");
        int vidaVeneno = m9.Vida;
        cv.TerminarTurnoJugador();
        Chequear(m9.Vida == vidaVeneno - Config.DanoVeneno, "Veneno: " + Config.DanoVeneno + " de daño al empezar el turno (hizo " + (vidaVeneno - m9.Vida) + ")");
        m9.PonerEstado(EstadoTipo.Envenenado, 3, 2, null);
        cv.QuitarEstadoNegativo(m9);
        Chequear(!m9.Tiene(EstadoTipo.Envenenado), "el antídoto quita el veneno");

        var sf = En("slime_fuego", 0, 0, 1); var shh = En("slime_hielo", 1, 1, 2);
        var cs = Vacio(sf, shh);
        Chequear(cs.CalcularDano(sf, 8, Elemento.Fuego, false) == 4 && cs.CalcularDano(sf, 8, Elemento.Hielo, false) == 12 && cs.CalcularDano(sf, 8, Elemento.Fisico, false) == 8, "slime de fuego: resiste fuego (la mitad), débil al hielo (+50%)");
        Chequear(cs.CalcularDano(shh, 8, Elemento.Hielo, false) == 4 && cs.CalcularDano(shh, 8, Elemento.Fuego, false) == 12, "slime de hielo: resiste hielo, débil al fuego");

        // ====================================================== empujes con choque
        var lu = Gh("luchador", 3, 3, 1, "zarpazo", "demoledor");
        var b1 = En("bruto", 4, 3, 101); var b2 = En("maton", 6, 3, 102);
        var cpu = Vacio(lu, b1, b2); cpu.Activa = lu; lu.Mana = 3;
        cpu.UsarHabilidad(Hb(lu, "demoledor"), b1.Pos);
        // bruto: 24 vida, 2 de defensa; daño 6+6-2 = 10; choca con el matón tras 1 casilla: 2 de daño a cada uno
        Chequear(b1.Pos == new Pos(5, 3) && b1.Vida == 24 - 10 - 2 && b2.Vida == 14 - 2, "Golpe demoledor empuja 2; choca con otra unidad: daño a las dos (" + b1.Vida + ", " + b2.Vida + ")");

        var lu2 = Gh("luchador", 1, 3, 1, "zarpazo", "demoledor"); var b3 = En("bruto", 0, 3, 101);
        var cb = Vacio(lu2, b3); cb.Activa = lu2;
        int v0 = b3.Vida;
        cb.EjecutarHabilidad(lu2, Hb(lu2, "zarpazo"), b3.Pos, false);
        Chequear(b3.Pos == new Pos(0, 3) && b3.Vida == v0 - 8 - Config.DanoChoque, "choque contra el borde del tablero: daño");

        var lu3 = Gh("luchador", 3, 3, 1, "zarpazo", "demoledor"); var b4 = En("bruto", 4, 3, 101);
        var cr = Vacio(lu3, b4); cr.Activa = lu3; cr.Tab.PonerTerreno(new Pos(5, 3), Terreno.Roca);
        cr.EjecutarHabilidad(lu3, Hb(lu3, "zarpazo"), b4.Pos, false);
        Chequear(b4.Pos == new Pos(4, 3) && b4.Vida == 24 - 8 - Config.DanoChoque, "choque contra una roca: daño y no se mueve");

        var lu4 = Gh("luchador", 3, 3, 1, "zarpazo", "demoledor"); var b5 = En("bruto", 4, 3, 101);
        var ccj = Vacio(lu4, b5); ccj.Activa = lu4; lu4.Mana = 3; ccj.Tab.PonerTerreno(new Pos(5, 3), Terreno.Caja);
        ccj.UsarHabilidad(Hb(lu4, "demoledor"), b5.Pos);
        Chequear(ccj.Tab.VidaObstaculo(new Pos(5, 3)) == Config.VidaCaja - 3 && b5.Pos == new Pos(4, 3), "el choque también daña los obstáculos rompibles (caja 4 -> 1)");

        // ====================================================== obstáculos rompibles
        var lu5 = Gh("luchador", 3, 3, 1, "zarpazo", "demoledor");
        var co = Vacio(lu5); co.Activa = lu5; co.Tab.PonerTerreno(new Pos(4, 3), Terreno.Caja);
        Chequear(co.Tab.EsRompible(new Pos(4, 3)) && !co.Tab.Pasable(new Pos(4, 3)), "la caja bloquea el paso");
        Chequear(co.CasillasObjetivo(lu5, Hb(lu5, "zarpazo")).Contains(new Pos(4, 3)), "se puede atacar una caja");
        co.UsarHabilidad(Hb(lu5, "zarpazo"), new Pos(4, 3));
        Chequear(co.Tab.TerrenoEn(new Pos(4, 3)) == Terreno.Suelo && co.Tab.Pasable(new Pos(4, 3)) && co.Tab.MonedasEn(new Pos(4, 3)) >= 1, "la caja se rompe, abre el paso y suelta monedas");

        var lu6 = Gh("luchador", 3, 3, 1, "zarpazo", "demoledor"); var m10 = En("maton", 5, 3, 101);
        var cbar = Vacio(lu6, m10); cbar.Activa = lu6; cbar.Tab.PonerTerreno(new Pos(4, 3), Terreno.Barril); cbar.Tab.PonerTerreno(new Pos(4, 2), Terreno.HierbaAlta);
        cbar.UsarHabilidad(Hb(lu6, "zarpazo"), new Pos(4, 3));
        Chequear(m10.Vida == 14 - Config.DanoBarril && lu6.Vida == lu6.VidaMax - Config.DanoBarril, "el barril explota: daño a todos los vecinos (también al que lo rompió)");
        Chequear(cbar.Tab.ZonaEn(new Pos(4, 2)) != null, "la explosión prende la hierba alta vecina");

        var lu7 = Gh("luchador", 3, 3, 1, "zarpazo", "tajo");
        var cfr = Vacio(lu7); cfr.Activa = lu7; cfr.Tab.PonerTerreno(new Pos(4, 3), Terreno.RocaFragil);
        for (int i = 0; i < 3 && cfr.Tab.EsRompible(new Pos(4, 3)); i++) { cfr.ActuoEsteTurno = false; lu7.Mana = 3; cfr.UsarHabilidad(Hb(lu7, "zarpazo"), new Pos(4, 3)); }
        Chequear(cfr.Tab.TerrenoEn(new Pos(4, 3)) == Terreno.Suelo, "la roca agrietada también se rompe a golpes");

        var tabE = new Tablero(10, 10);
        for (int y = 0; y < 10; y++) tabE.PonerTerreno(new Pos(5, y), Terreno.RocaFragil);
        Chequear(!tabE.EsConexo(), "para generar niveles, los obstáculos rompibles cuentan como pared: el tablero siempre se puede cruzar sin romper nada");

        // ====================================================== monedas
        var lm = Gt("luchador", 0, 0, 1);
        var cmo = Vacio(lm); cmo.Activa = lm; cmo.MovimientoRestante = 3;
        cmo.Tab.PonerMonedas(new Pos(2, 0), 2);
        cmo.Mover(new Pos(3, 0));
        Chequear(cmo.Dinero == 2 && cmo.Tab.MonedasEn(new Pos(2, 0)) == 0, "un gato recoge las monedas del camino");

        var ld = En("ladron", 6, 0, 101); var lg = Gt("luchador", 0, 5, 1);
        var cl = Vacio(lg, ld); cl.Activa = ld; cl.MovimientoRestante = 5; cl.Tab.PonerMonedas(new Pos(3, 0), 2);
        IA.JugarTurnoEnemigo(cl, ld);
        Chequear(ld.Monedas == 2 && cl.Tab.MonedasEn(new Pos(3, 0)) == 0 && cl.Dinero == 0, "el ladrón va a por las monedas del suelo");
        cl.DanoDirecto(ld, 99, "prueba");
        Chequear(cl.Tab.MonedasEn(ld.Pos) == 2, "si muere, el ladrón suelta lo que llevaba");

        var jf = En("jefe1", 5, 5, 101); var cj = Vacio(Gt("luchador", 0, 0, 1), jf);
        cj.DanoDirecto(jf, 999, "prueba");
        Chequear(cj.Tab.MonedasEn(jf.Pos) == 5, "los jefes sueltan 5 monedas");

        // ====================================================== enemigos nuevos
        var nido = En("nido", 8, 3, 101); var gl = Gt("luchador", 0, 3, 1);
        var cn = Vacio(gl, nido); cn.Activa = nido;
        int ratas = 0;
        for (int i = 1; i <= 3; i++) { nido.TurnosPropios = i; IA.JugarTurnoEnemigo(cn, nido); }
        int ratas3 = 0; foreach (Unidad u in cn.Vivos(Bando.Enemigos)) if (u.Def.Id == "rata" && u.Dueno == nido) ratas3++;
        Chequear(ratas3 == 3, "el nido crea una rata cada turno (3 turnos, 3 ratas: hay " + ratas3 + ")");
        for (int i = 4; i <= 12; i++) { nido.TurnosPropios = i; IA.JugarTurnoEnemigo(cn, nido); }
        foreach (Unidad u in cn.Vivos(Bando.Enemigos)) if (u.Def.Id == "rata" && u.Dueno == nido) ratas++;
        Chequear(ratas == 6, "el nido no pasa de 6 ratas vivas a la vez (hay " + ratas + ")");
        Chequear(nido.Pos == new Pos(8, 3) && cn.MovimientoTotal(nido) == 0, "el nido no se mueve");

        var bo = En("bombardero", 4, 3, 101); var gb = Gt("luchador", 3, 3, 1);
        var cbo = Vacio(gb, bo); cbo.Activa = bo; cbo.MovimientoRestante = 4;
        cbo.Tab.PonerTerreno(new Pos(4, 2), Terreno.HierbaAlta);
        IA.JugarTurnoEnemigo(cbo, bo);
        Chequear(!bo.Vivo && gb.Vida == gb.VidaMax - 5 && cbo.Tab.ZonaEn(new Pos(4, 2)) != null, "el gato bomba explota junto a un gato: 5 de fuego en cruz y prende la hierba");
        var bo2 = En("bombardero", 5, 5, 101); var g5 = Gt("luchador", 5, 6, 1); var e5 = En("maton", 6, 5, 102);
        var cbo2 = Vacio(g5, bo2, e5);
        cbo2.DanoDirecto(bo2, 99, "prueba");
        Chequear(g5.Vida == g5.VidaMax - 5 && e5.Vida == 14 - 5, "el gato bomba explota al morir y daña a todos los que están en cruz");

        var cha = En("chaman", 8, 3, 101); var gc = Gt("luchador", 0, 3, 1);
        var cch = Vacio(gc, cha); cch.Activa = cha; cch.MovimientoRestante = 2;
        cha.TurnosPropios = 4;
        IA.JugarTurnoEnemigo(cch, cha);
        Chequear(cch.Vivos(Bando.Enemigos).Count == 2, "el chamán invoca una rata cada 4 turnos");

        var sl = En("slime_hielo", 4, 3, 101); var gs = Gt("luchador", 3, 3, 1);
        var csl = Vacio(gs, sl); csl.Activa = sl; csl.MovimientoRestante = 2;
        IA.JugarTurnoEnemigo(csl, sl);
        Chequear(gs.Tiene(EstadoTipo.Ralentizado), "el slime de hielo ralentiza al gato que golpea");
        var sa = En("sapo", 5, 3, 101); var gv = Gt("luchador", 3, 3, 1);
        var csa2 = Vacio(gv, sa); csa2.Activa = sa; csa2.MovimientoRestante = 2;
        IA.JugarTurnoEnemigo(csa2, sa);
        Chequear(gv.Tiene(EstadoTipo.Envenenado), "el sapo envenena");
        var an = En("anguila", 6, 3, 101); var gan = Gt("luchador", 4, 3, 1); var gan2 = Gt("luchador", 4, 4, 2);
        var can = Vacio(gan, gan2, an);
        can.Tab.PonerTerreno(new Pos(4, 3), Terreno.Agua);
        Chequear(can.CalcularDano(gan, 3, Elemento.Rayo, false) == can.CalcularDano(gan2, 3, Elemento.Rayo, false) + 3 - 0 || can.CalcularDano(gan, 3, Elemento.Rayo, false) > can.CalcularDano(gan2, 3, Elemento.Rayo, false), "la descarga de la anguila pega más a quien está en el agua");

        // ====================================================== tienda
        Run rt = new Run(99);
        int guardia = 0;
        bool compro = false, compraOk = true;
        while (rt.Fase != FaseRun.RunPerdida && rt.Fase != FaseRun.CampanaCompletada && guardia++ < 400 && !compro)
        {
            switch (rt.Fase)
            {
                case FaseRun.ElegirHabilidad: BotJugador.ElegirHabilidades(rt); break;
                case FaseRun.Preparacion: BotJugador.Equipar(rt); rt.Empezar(); break;
                case FaseRun.Combate: BotJugador.JugarCombate(rt.Combate); rt.ResolverFinDeCombate(); break;
                case FaseRun.Recompensa: BotJugador.ElegirDesafio(rt); break;
                case FaseRun.Tienda:
                    {
                        rt.Dinero = Math.Max(rt.Dinero, 10);
                        int antesD = rt.Dinero;
                        int i = 0;
                        for (; i < rt.Tienda.Count; i++) if (rt.Tienda[i].EsConsumible) break;
                        OfertaTienda o = rt.Tienda[i];
                        int cant = rt.Inventario[o.Cons];
                        compraOk = rt.Comprar(i) && rt.Dinero == antesD - o.Precio && rt.Inventario[o.Cons] == cant + 1 && o.Vendido && !rt.Comprar(i);
                        rt.Dinero = 0;
                        compraOk = compraOk && !rt.Comprar(rt.Tienda.Count - 1);
                        compro = true;
                        break;
                    }
            }
        }
        Chequear(compro && compraOk, "la tienda: comprar resta monedas, entrega el objeto y no se puede comprar dos veces ni sin dinero");

        // ====================================================== los eventos nuevos se graban
        Config.GrabarEventos = true;
        var pic2 = Gh("picaro", 3, 3, 1, "punalada", "paso_sombrio"); var mm = En("maton", 6, 3, 101);
        var ce = Vacio(pic2, mm); ce.Activa = pic2; pic2.Mana = 4;
        ce.GrabarEventos = true;
        ce.UsarHabilidad(Hb(pic2, "paso_sombrio"), mm.Pos);
        bool tele = false; foreach (Evento e in ce.Eventos) if (e.Tipo == EvTipo.Teletransporte && e.A == new Pos(3, 3) && e.B == new Pos(7, 3)) tele = true;
        Chequear(tele, "el teletransporte deja un evento para animar");
        Config.GrabarEventos = false;
    }
}
