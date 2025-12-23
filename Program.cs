using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace TragaMonedas
{
    class Program
    {
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var game = new Game(startingCredits: 100, minBet: 1, maxBet: 20);
            Console.CursorVisible = false;
            game.Run();            
        }
    }

    // Orquesta flujo principal
    public class Game
    {
        private readonly SlotMachine _machine;
        private readonly PayTable _payTable;
        private readonly ConsoleUI _ui;
        private int _credits;
        private readonly int _minBet;
        private readonly int _maxBet;

        private CancellationTokenSource? _blinkCts;

        public Game(int startingCredits, int minBet, int maxBet)
        {
            _credits = startingCredits;
            _minBet = minBet;
            _maxBet = maxBet;

            var symbols = new[] {"🍒", "🍋", "🍉", "🍓", "🍐"};
            _machine = new SlotMachine(symbols, reelsCount: 3);
            _payTable = PayTable.Default();

            _ui= new ConsoleUI(
            @"                                                
    ,---.    ,---. _______ ,---.    .---.         
    | .-.\   | .-'|__   __|| .-.\  / .-. )        
    | `-'/   | `-.  )| |   | `-'/  | | |(_)       
    |   (    | .-' (_) |   |   (   | | | |        
    | |\ \   |  `--. | |   | |\ \  \ `-' /        
    |_| \)\  /( __.' `-'   |_| \)\  )---'         
        (__)(__)               (__)(_)            
       .---. ,-.    .---.  _______  .---.         
      ( .-._)| |   / .-. )|__   __|( .-._)        
     (_) \   | |   | | |(_) )| |  (_) \           
     _  \ \  | |   | | | | (_) |  _  \ \          
    ( `-'  ) | `--.\ `-' /   | | ( `-'  )         
     `----'  |( __.')---'    `-'  `----'          
             (_)   (_)                          ");
        }

        public void Run()
        {
            while (true)
            {
                Console.Clear();
                _ui.DrawHeader();
                _ui.DrawCredits(_credits);
                _ui.DrawMenu(new[]
                {
                    "Enter.: Jugar                                  ",
                    "A.....: Cambiar apuesta                        ",
                    "H.....: Tabla de Pagos                         ",
                    "X.....: Salir                                  "                                        
                });

                Console.WriteLine();
                _ui.DrawDivider();
                Console.WriteLine("Desarrollado Por: Carlos Amaro   ");
                _ui.DrawDivider();

                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.X) break;
                if (key.Key == ConsoleKey.H) { ShowPayTable(); continue; }
                if (key.Key == ConsoleKey.A) { ChangeBet(); continue; }
                if (key.Key == ConsoleKey.Enter) { PlayRound(); continue; }
            }

            _ui.DrawFooter("Gracias por jugar. ¡Hasta pronto!");
        }

        private int _bet = 5;

        private void ChangeBet()
        {
            Console.Clear();
            _ui.DrawHeader();
            _ui.WriteInfo($"Apuesta actual: {_bet} | Rango permitido: {_minBet}–{_maxBet}");
            _ui.WriteInfo("Ingresa nueva apuesta o presiona ESC para volver.");

            var input = InputHelper.ReadLineWithEsc();
            if (input.IsEsc) return;

            if (!int.TryParse(input.Value, out int newBet))
            {
                _ui.WriteError("Valor inválido. Usa números enteros.");
                InputHelper.PressAnyKey();
                return;
            }
            if (newBet < _minBet || newBet > _maxBet)
            {
                _ui.WriteError($"Fuera de rango. Debe estar entre {_minBet} y {_maxBet}.");
                InputHelper.PressAnyKey();
                return;
            }
            _bet = newBet;
            _ui.WriteSuccess($"Apuesta actualizada a {_bet}.");
            InputHelper.PressAnyKey();
        }

        private void ShowPayTable()
        {
            Console.Clear();
            _ui.DrawHeader();
            _ui.WriteInfo("Tabla de pagos (multiplicadores sobre la apuesta):");
            _ui.DrawDivider();
            foreach (var entry in _payTable.GetEntries())
            {
                Console.WriteLine($"  {entry.Description,-30} X {entry.Multiplier}");
            }
            _ui.DrawDivider();
            InputHelper.PressAnyKey("Presiona una tecla para volver al menú.");
        }
                
        private void PlayRound()
        {            
            if (_credits < _bet)
            {
                _ui.WriteError("Créditos insuficientes para la apuesta.");
                InputHelper.PressAnyKey();
                return;
            }

            _credits -= _bet;
            Console.Clear();
            _ui.DrawHeader();
            _ui.DrawCredits(_credits);
            _ui.WriteInfo($"Apuesta: {_bet}");
            _ui.DrawDivider();

            // Animación de giro
            var result = _machine.SpinWithAnimation(animationMillis: 850, frameDelayMillis: 65);

            string combinationType;
            string? winningSymbol;
            double multiplier;

            var payout = _payTable.CalculatePayout(result.Symbols, _bet, out combinationType, out winningSymbol, out multiplier);

            var spinResult = new SpinResult(result.Symbols)
            {
                IsWin = payout > 0,
                CombinationType = combinationType,
                WinningSymbol = winningSymbol,
                Multiplier = multiplier,
                Payout = payout
            };

            string message;
            if (payout > 0)
            {               
                message = $"🎉 {combinationType} con {winningSymbol} Ganaste {payout} créditos (x{multiplier}).";
                //message = $"🎉 ¡Ganaste {payout} créditos!";
                _blinkCts?.Cancel();
                _blinkCts = new CancellationTokenSource();
                Console.Beep(1200, 200); // tono agudo para ganar
                Task.Run(() => BlinkText(message, ConsoleColor.Yellow, 2, 200, _blinkCts.Token));
                _credits += payout;
            }
            else
            {
                message = "😢 No hubo premio...";
                _blinkCts?.Cancel();
                _blinkCts = new CancellationTokenSource();
                Console.Beep(400, 200); // tono grave para perder
                Task.Run(() => BlinkText(message, ConsoleColor.Red, 2, 200, _blinkCts.Token));
            }

            _ui.DrawDivider();
            //_ui.WriteHighlight(message);
            _ui.DrawCredits(_credits);
            _ui.WriteInfo("Enter: Jugar | Esc: Regresar");          
            while (true)
            {
                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Enter)
                {
                    _blinkCts?.Cancel();
                    PlayRound();
                    return;
                }

                if (key.Key == ConsoleKey.Escape)
                {
                    _blinkCts?.Cancel();
                    return;
                }

                if (key.Key == ConsoleKey.Q)
                {
                    _blinkCts?.Cancel();
                    Environment.Exit(0);
                }
            }
        }

        static void BlinkText(string text, ConsoleColor color, int times, int delay, CancellationToken token)
        {
            // Línea fija debajo de los rodillos
            int row = Console.CursorTop + 2;

            for (int i = 0; i < times; i++)
            {
                if (token.IsCancellationRequested) return;

                Console.SetCursorPosition(0, row);
                Console.ForegroundColor = color;
                Console.Write(text.PadRight(Console.WindowWidth));
                Console.ResetColor();

                Thread.Sleep(delay);

                Console.SetCursorPosition(0, row);
                Console.Write(new string(' ', Console.WindowWidth));

                Thread.Sleep(delay);
            }

            if (!token.IsCancellationRequested)
            {
                Console.SetCursorPosition(0, row);
                Console.ForegroundColor = color;
                Console.Write(text.PadRight(Console.WindowWidth));
                Console.ResetColor();
            }

            if (!token.IsCancellationRequested)
            {
                Console.SetCursorPosition(0, row);
                Console.ForegroundColor = color;
                Console.Write(text.PadRight(Console.WindowWidth));
                Console.ResetColor();
            }

        }
    }

    // Lógica de rodillos y spin
    public class SlotMachine
    {
        private readonly Reel[] _reels;
        private readonly Random _rng = new Random();

        public SlotMachine(string[] symbols, int reelsCount)
        {
            _reels = Enumerable.Range(0, reelsCount)
                .Select(_ => new Reel(symbols))
                .ToArray();
        }

        public SpinResult Spin()
        {
            var symbols = _reels.Select(r => r.RandomSymbol(_rng)).ToArray();
            return new SpinResult(symbols);
        }

        public SpinResult SpinWithAnimation(int animationMillis = 800, int frameDelayMillis = 70)
        {
            var start = DateTime.UtcNow;
            int[] stagger = { 0, 120, 240 }; // desfase de rodillos

            string[] lastFrame = _reels.Select(r => r.RandomSymbol(_rng)).ToArray();

            while ((DateTime.UtcNow - start).TotalMilliseconds < animationMillis)
            {
                for (int i = 0; i < _reels.Length; i++)
                {
                    if ((DateTime.UtcNow - start).TotalMilliseconds >= stagger[i])
                    {
                        lastFrame[i] = _reels[i].RandomSymbol(_rng);
                    }
                }
                ConsoleUI.DrawInlineReels(lastFrame);
                Thread.Sleep(frameDelayMillis);
            }

            var final = _reels.Select(r => r.RandomSymbol(_rng)).ToArray();
            ConsoleUI.DrawInlineReels(final);
            return new SpinResult(final);
        }
    }

    // Un rodillo con su lista de símbolos
    public class Reel
    {
        private readonly string[] _symbols;

        public Reel(string[] symbols)
        {
            _symbols = symbols;
        }

        public string RandomSymbol(Random rng) => _symbols[rng.Next(_symbols.Length)];
    }

    // Cálculo de pagos y reglas
    public class PayTable
    {
        private readonly Dictionary<string, int> _threeOfKindMultiplier;
        private readonly double _twoOfKindMultiplier;

        private PayTable(Dictionary<string, int> threeOfKindMultiplier, double twoOfKindMultiplier)
        {
            _threeOfKindMultiplier = threeOfKindMultiplier;
            _twoOfKindMultiplier = twoOfKindMultiplier;
        }

        public static PayTable Default()
        {
            return new PayTable(
                new Dictionary<string, int>
                {                    
                    { "🍐", 20 },
                    { "🍓", 10 },
                    { "🍉", 5 },
                    { "🍒", 3 },
                    { "🍋", 3 },                    
                },
                twoOfKindMultiplier: 1.5
            );
        }

        public int CalculatePayout(string[] symbols, int bet, out string combinationType, out string? winningSymbol, out double multiplier)
        {
            // Tres iguales
            if (symbols.Distinct().Count() == 1)
            {
                var s = symbols[0];
                multiplier = _threeOfKindMultiplier.TryGetValue(s, out var m) ? m : 2;
                combinationType = "Tres iguales";
                winningSymbol = s;
                return (int)(multiplier * bet);
            }

            // Dos iguales
            bool twoOfKind = symbols.GroupBy(s => s).Any(g => g.Count() == 2);
            if (twoOfKind)
            {
                multiplier = _twoOfKindMultiplier;
                combinationType = "Dos iguales";
                winningSymbol = symbols.GroupBy(s => s).First(g => g.Count() == 2).Key;
                return (int)Math.Floor(multiplier * bet);
            }

            // Sin premio
            multiplier = 0;
            combinationType = "Sin premio";
            winningSymbol = null;
            return 0;
        }

        public IEnumerable<(string Description, double Multiplier)> GetEntries()
        {
            foreach (var kv in _threeOfKindMultiplier.OrderByDescending(k => k.Value))
            {
                yield return ($"Tres iguales {kv.Key}", kv.Value);
            }
            yield return ("Dos iguales", _twoOfKindMultiplier);
        }

    }

    // UI retro con helpers
    public class ConsoleUI
    {
        private readonly string _title;

        public ConsoleUI(string title) => _title = title;

        public void DrawHeader()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine("┌" + new string('─', 48) + "┐");
            Console.WriteLine($"│ {_title,-48} │");
            Console.WriteLine("└" + new string('─', 48) + "┘");
            Console.ResetColor();
        }

        public void DrawFooter(string text)
        {            
            Console.ForegroundColor = ConsoleColor.Yellow;
            DrawDivider();
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public void DrawCredits(int credits)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Créditos: {credits}");
            Console.ResetColor();
        }

        public void DrawMenu(IEnumerable<string> items)
        {
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            DrawDivider();            
            foreach (var item in items)
                Console.WriteLine($" - {item}");
            DrawDivider();            
            Console.ResetColor();
        }

        public void DrawDivider()
        {
            Console.WriteLine(new string('─', 50));
        }

        public void WriteInfo(string text)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public void WriteError(string text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public void WriteSuccess(string text)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public void WriteHighlight(string text)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public void DrawReels(string[] symbols)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;            
            Console.WriteLine($"[{symbols[0]}] [{symbols[1]}] [{symbols[2]}]");
            Console.ResetColor();
        }

        // Dibuja en línea durante animación (no salta de renglón)
        public static void DrawInlineReels(string[] symbols)
        {            
            Console.SetCursorPosition(0, Math.Max(Console.CursorTop - 1, 0));
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"[{symbols[0]}] [{symbols[1]}] [{symbols[2]}]    ");
            Console.ResetColor();
        }
    }

    // Entrada con soporte ESC
    public static class InputHelper
    {
        public struct InputResult
        {
            public bool IsEsc { get; init; } 
            public string Value { get; init; }             
        }

        public static InputResult ReadLineWithEsc()
        {
            var buffer = "";
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape) return new InputResult { IsEsc = true, Value = "" };
                if (key.Key == ConsoleKey.Enter) return new InputResult { IsEsc = false, Value = buffer };
                if (key.Key == ConsoleKey.Backspace && buffer.Length > 0)
                {
                    buffer = buffer.Substring(0, buffer.Length - 1);
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    buffer += key.KeyChar;
                    Console.Write(key.KeyChar);
                }
            }
        }

        public static void PressAnyKey(string message = "Presiona cualquier tecla para continuar...")
        {
            Console.WriteLine();
            Console.WriteLine(message);
            Console.ReadKey(true);
        }        
    }

    public record SpinResult(string[] Symbols)
    {
        public bool IsWin { get; init; }
        public string CombinationType { get; init; } = "Sin premio";
        public string? WinningSymbol { get; init; }
        public double Multiplier { get; init; }
        public int Payout { get; init; }
    }
}