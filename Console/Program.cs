﻿using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Fractals.Model;
using Fractals.Renderer;
using Fractals.Utility;
using log4net;

namespace Console
{
    class Program
    {
        private static ILog _log;

        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            new Program().Process(args);
        }

        public Program()
        {
            _log = LogManager.GetLogger(GetType());
        }

        private void Process(string[] args)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                args = GetDebuggingArguments();
            }

            var options = new Options();
            if (!CommandLine.Parser.Default.ParseArguments(args, options))
            {
                return;
            }

            _log.InfoFormat("Operation: {0}", options.Operation);

            switch (options.Operation)
            {
                case OperationType.RenderMandelbrot:
                    RenderMandelbrot<MandelbrotRenderer>(options);
                    break;
                case OperationType.RenderInterestingPointsMandelbrot:
                    RenderMandelbrot<InterestingPointsRenderer>(options);
                    break;
                case OperationType.FindPoints:
                    FindPoints(options);
                    break;
                case OperationType.PlotPoints:
                    PlotPoints(options);
                    break;
            }
        }

        private void RenderMandelbrot<T>(Options options)
            where T : MandelbrotRenderer, new()
        {
            var resolution = new Size(options.ResolutionWidth, options.ResolutionHeight);
            var realAxis = new InclusiveRange(-2, 1);
            var imaginaryAxis = new InclusiveRange(-1.5, 1.5);

            var renderer = Activator.CreateInstance<T>();

            Color[,] output = renderer.Render(resolution, realAxis, imaginaryAxis);

            Bitmap image = ImageUtility.ColorMatrixToBitmap(output);

            image.Save(Path.Combine(options.OutputDirectory, String.Format("{0}.png", options.Filename)));
        }

        private void FindPoints(Options options)
        {
            var finder = new PointFinder(options.OutputDirectory, options.Filename);

            System.Console.WriteLine("Press <ENTER> to stop...");

            Task.Factory.StartNew(() =>
            {
                System.Console.ReadLine();
                finder.Stop();
            });

            finder.Start();
        }

        private void PlotPoints(Options options)
        {
            var plotter = new Plotter(options.OutputDirectory, options.InputFilename, options.Filename, options.ResolutionWidth, options.ResolutionHeight);
            plotter.Plot();
        }

        private string[] GetDebuggingArguments()
        {
//            return new[]
//                {
//                    "-t", "RenderMandelbrot",
//                    "-w", "500",
//                    "-h", "500",
//                    "-d", "C:\\temp",
//                    "-f", "mandelbrot"
//                };
//            return new[]
//                {
//                    "-t", "RenderInterestingPointsMandelbrot",
//                    "-w", "500",
//                    "-h", "500",
//                    "-d", "C:\\temp",
//                    "-f", "mandelbrot-areas"
//                };
//                return new[]
//                    {
//                        "-t", "FindPoints",
//                        "-d", "C:\\temp",
//                        "-f", "points"
//                    };
                return new[]
                    {
                        "-t", "PlotPoints",
                        "-w", "2000",
                        "-h", "2000",
                        "-d", "C:\\temp",
                        "-f", "buddhabrot",
                        "-i", "points"
                    };
        }
    }
}