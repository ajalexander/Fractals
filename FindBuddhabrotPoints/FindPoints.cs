﻿using System.IO;
using Fractals.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fractals.Renderer;
using Fractals.Utility;

namespace FindBuddhabrotPoints
{
    class FindPoints
    {
        private static bool _shouldStop = false;
        private readonly static object ShouldStopLock = new object();

        private static bool ShouldStop
        {
            get
            {
                lock (ShouldStopLock)
                {
                    return _shouldStop;
                }
            }
            set
            {
                lock (ShouldStopLock)
                {
                    _shouldStop = value;
                }
            }
        }

        static void Main(string[] args)
        {
            var bailout = new BailoutRange(
                min: 20000,
                max: 30000);

            var viewPort = new Area(
                realRange: new InclusiveRange(-0.5, 2),
                imagRange: new InclusiveRange(-1.3, 1.3));

            var list = new ComplexNumberList("output.list");

            Console.WriteLine("Press any key to cancel...");

            Task.Factory.StartNew(() =>
            {
                Console.ReadKey();
                ShouldStop = true;
            });

            int num = 0;

            Parallel.ForEach(GetRandomComplexNumbers(viewPort),
                (number, state) =>
                {
                    if (BuddhabrotPointGenerator.IsPointInBuddhabrot(number, bailout))
                    {
                        Interlocked.Increment(ref num);
                        Console.Out.WriteLine(num);
                        list.SaveNumber(number);
                    }

                    if (ShouldStop)
                    {
                        state.Break();
                    }
                });
        }

        private static IEnumerable<Complex> GetRandomComplexNumbers(Area viewPort)
        {
            var rand = new CryptoRandom();
            while (true)
            {
                yield return BuddhabrotPointGenerator.GetPossiblePoint(rand, viewPort);
            }
        }
    }
}