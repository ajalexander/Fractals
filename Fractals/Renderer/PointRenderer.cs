﻿using Fractals.Utility;
using log4net;
using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Fractals.Renderer
{
    public sealed class PointRenderer
    {
        private readonly string _inputInputDirectory;
        private readonly string _inputFilename;

        private readonly Size _resolution;

        private static ILog _log;

        private readonly HitPlot _hitPlot;

        public PointRenderer(string inputDirectory, string inputFilename, int width, int height)
        {
            _inputInputDirectory = inputDirectory;
            _inputFilename = inputFilename;

            _resolution = new Size(width, height);

            _hitPlot = new HitPlot(_resolution);

            _log = LogManager.GetLogger(GetType());
        }

        public void Render(string outputDirectory, string outputFilename)
        {
            _log.Info("Loading trajectory...");

            _hitPlot.LoadTrajectories(Path.Combine(_inputInputDirectory, _inputFilename));

            _log.Info("Done loading; finding maximum...");

            var max = _hitPlot.FindMaximumHit();

            _log.DebugFormat("Found maximum: {0}", max);

            _log.Info("Starting to render");

            var outputImg = new Bitmap(_resolution.Width, _resolution.Height);

            var processedPixels =
                _resolution.GetAllPoints().
                AsParallel().
                Select(p => ComputeColor(p, max)).
                AsEnumerable();

            foreach (var result in processedPixels)
            {
                outputImg.SetPixel(result.Item1.X, result.Item1.Y, result.Item2);
            }

            _log.Info("Finished rendering");

            _log.Debug("Saving image");
            outputImg.Save(Path.Combine(outputDirectory, String.Format("{0}.png", outputFilename)));
            _log.Debug("Done saving image");

        }

        private Tuple<Point, Color> ComputeColor(Point p, int max)
        {
            var current = _hitPlot.GetHitsForPoint(p);

            var exp = Gamma(1.0 - Math.Pow(Math.E, -10.0 * current / max));

            return Tuple.Create(p,
                new HsvColor(
                    hue: 196.0 / 360.0,
                    saturation: (exp < 0.5) ? 1 : 1 - (2 * (exp - 0.5)),
                    value: (exp < 0.5) ? 2 * exp : 1
                ).ToColor());
        }

        private static double Gamma(double x, double exp = 1.2)
        {
            return Math.Pow(x, 1.0 / exp);
        }
    }
}