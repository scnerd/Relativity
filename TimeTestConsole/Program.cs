/*
 * David Maxson
 * Relativity console example
 * 1/13/13
 * scnerd@gmail.com
 * 
 * TO USE: Compile and run.
 * You will have 10 seconds to type in anything.
 * After that, the computer will automatically jump to various
 * points in the past (9 seconds, then 8 sec, and so on) and
 * recreate your typing from back then.
 * NOTE: To get a demonstration of modifying the past, comment
 * out the line "Console.In.Close();" Doing this will allow you
 * to continue typing after the initial 10 seconds. Any changes
 * made in the past will remain there and affect the future.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using TimeRelativity;

namespace TimeTestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();
            string CurrentText = "";
            EventManager Manager = new EventManager(
                (k, t) => { CurrentText += (char)k; Console.Write((char)k); return true; },
                () => CurrentText,
                (s) => { Console.Clear(); CurrentText = (string)s; Console.Write(CurrentText); },
                100);
            stopwatch.Start();
            Manager.Start();
            char Typed;

            Action Process = () =>
                {
                    Typed = (char)0;
                    if (Console.KeyAvailable)
                    {
                        Typed = Console.ReadKey(true).KeyChar;
                        Manager.AddEvent(Typed);
                    }
                    Manager.ProcessEvents();
                };

            while (stopwatch.Elapsed.Seconds < 10) Process();
            Console.In.Close();

            while (stopwatch.Elapsed.Seconds < 11) Process();
            Manager.JumpToTime(9000);

            while (stopwatch.Elapsed.Seconds < 12) Process();
            Manager.JumpToTime(8000);

            while (stopwatch.Elapsed.Seconds < 13) Process();
            Manager.JumpToTime(7000);

            while (stopwatch.Elapsed.Seconds < 15) Process();
            Manager.JumpToTime(6000);

            while (stopwatch.Elapsed.Seconds < 17) Process();
            Manager.JumpToTime(5000);

            while (stopwatch.Elapsed.Seconds < 19) Process();
            Manager.JumpToTime(4000);

            while (stopwatch.Elapsed.Seconds < 22) Process();
            Manager.JumpToTime(3000);

            while (stopwatch.Elapsed.Seconds < 25) Process();
            Manager.JumpToTime(2000);

            while (stopwatch.Elapsed.Seconds < 30) Process();
            Manager.JumpToTime(1000);

            while (stopwatch.Elapsed.Seconds < 35) Process();
            Manager.JumpToTime(0000);

            while (true) Process();
        }
    }
}
