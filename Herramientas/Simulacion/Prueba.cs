// Muestra el registro de un combate completo (bot contra IA) para revisar las reglas a ojo.
// Uso: dotnet bin/Prueba.dll mundo nivel semilla clase1,clase2,clase3,clase4
using System;
using System.Collections.Generic;
using Gatomate.Core;

public static class Prueba
{
    public static int Correr(string[] args)
    {
        int mundo = int.Parse(args[0]), nivel = int.Parse(args[1]), semilla = int.Parse(args[2]);
        var gatos = new List<Gato>();
        int id = 1;
        foreach (string cid in args[3].Split(','))
        {
            var g = new Gato { Id = id++, ClaseId = cid, Nombre = GameData.Clase(cid).Nombre };
            g.Vida = g.VidaMax; gatos.Add(g);
        }
        var inv = new Dictionary<Consumible, int>();
        foreach (Consumible c in Enum.GetValues(typeof(Consumible))) inv[c] = 1;
        Combate cb = GeneradorNivel.Crear(gatos, mundo, nivel, semilla, new List<EfectoActivo>(), inv);
        for (int y = 0; y < cb.Tab.Alto; y++)
        {
            string fila = "";
            for (int x = 0; x < cb.Tab.Ancho; x++)
            {
                Unidad u = cb.UnidadEn(new Pos(x, y));
                if (u != null) fila += u.EsGato ? "G" : (u.Def.EsJefe ? "J" : "E");
                else switch (cb.Tab.TerrenoEn(new Pos(x, y))) { case Terreno.HierbaAlta: fila += "\""; break; case Terreno.Flores: fila += "*"; break; case Terreno.Barro: fila += "~"; break; case Terreno.Roca: fila += "#"; break; default: fila += "."; break; }
            }
            Console.WriteLine(fila);
        }
        cb.Iniciar();
        BotJugador.JugarCombate(cb);
        foreach (string l in cb.Log) Console.WriteLine(l);
        Console.WriteLine("Resultado: " + cb.Estado + " en " + cb.Ronda + " rondas.");
        foreach (Unidad u in cb.Unidades) if (u.EsGato) Console.WriteLine("  " + u.Nombre + " " + u.Vida + "/" + u.VidaMax);
        return 0;
    }
}
