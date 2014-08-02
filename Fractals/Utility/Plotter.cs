﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fractals.Model;
using log4net;

namespace Fractals.Utility
{
    public sealed class Plotter
    {
        private readonly string _inputDirectory;
        private readonly string _inputFilenamePattern;
        private readonly string _directory;
        private readonly string _filename;
        private readonly Size _resolution;

        private int[][] _plot;

        private static ILog _log;

        public Plotter(string inputDirectory, string inputFilenamePattern, string directory, string filename, int width, int height)
        {
            _inputDirectory = inputDirectory;
            _inputFilenamePattern = inputFilenamePattern;
            _directory = directory;
            _filename = filename;
            _resolution = new Size(width, height);

            _log = LogManager.GetLogger(GetType());

            InitializeHitPlot();
        }

        #region Hit Plot Array Operations

        private void InitializeHitPlot()
        {
            _plot = new int[_resolution.Width][];
            for (int col = 0; col < _resolution.Height; col++)
            {
                _plot[col] = new int[_resolution.Height];
            }
        }

        private void IncrementPoint(Point p)
        {
            Interlocked.Increment(ref _plot[p.X][p.Y]);
        }

        private int GetHitsForPoint(int x, int y)
        {
            return _plot[x][y];
        }

        private int FindMaximumHit()
        {
            int max = 0;
            for (int x = 0; x < _resolution.Width; x++)
            {
                for (int y = 0; y < _resolution.Height; y++)
                {
                    var temp = _plot[x][y];
                    if (temp > max)
                    {
                        max = temp;
                    }
                }
            }

            return max;
        }

        #endregion Hit Plot Array Operations

        public void Plot()
        {
            _log.InfoFormat("Plotting image ({0}x{1})", _resolution.Width, _resolution.Height);

            var list = new ComplexNumberListReader(_inputDirectory, _inputFilenamePattern);

            var viewPort = new Area(
                            realRange: new InclusiveRange(-1.75, 1),
                            imagRange: new InclusiveRange(-1.3, 1.3));

            var rotatedResolution = new Size(_resolution.Height, _resolution.Width);

            _log.Debug("Calculating trajectories");

            Parallel.ForEach(list.GetNumbers(), number =>
            {
                foreach (var c in GetTrajectory(number))
                {
                    var point = viewPort.GetPointFromNumber(rotatedResolution, c).Rotate();

                    if (!_resolution.IsInside(point))
                    {
                        continue;
                    }

                    IncrementPoint(point);
                }
            });

            _log.InfoFormat("Done plotting trajectories...");

            var max = FindMaximumHit();            

            _log.InfoFormat("Found max: {0}", max);

            var outputImg = new Bitmap(_resolution.Width, _resolution.Height);

            for (int x = 0; x < _resolution.Width; x++)
            {
                for (int y = 0; y < _resolution.Height; y++)
                {
                    var current = GetHitsForPoint(x, y);

                    var exp = Gamma(1.0 - Math.Pow(Math.E, -10.0 * current / max));

                    outputImg.SetPixel(x, y, new HsvColor(
                        hue: 196.0 / 360.0,
                        saturation: (exp < 0.5) ? 1 : 1 - (2 * (exp - 0.5)),
                        value: (exp < 0.5) ? 2 * exp : 1
                    ).ToColor());
                }
            }

            _log.Debug("Storing image");
            outputImg.Save(Path.Combine(_directory, String.Format("{0}.png", _filename)));
        }

        private const double DefaultGamma = 1.2;

        private static double Gamma(double x, double exp = DefaultGamma)
        {
            return Math.Pow(x, 1.0 / exp);
        }

        private const int Bailout = 30000;

        static IEnumerable<Complex> GetTrajectory(Complex c)
        {
            var rePrev = c.Real;
            var imPrev = c.Imag;

            double re = 0;
            double im = 0;

            for (int i = 0; i < Bailout; i++)
            {
                var reTemp = re * re - im * im + rePrev;
                im = 2 * re * im + imPrev;
                re = reTemp;

                yield return new Complex(re, im);

                var magnitudeSquared = re * re + im * im;
                if (magnitudeSquared > 4)
                {
                    yield break;
                }
            }
        }
    }
}