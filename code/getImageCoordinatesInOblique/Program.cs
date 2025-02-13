// See https://aka.ms/new-console-template for more information
using BAG;
using NetTopologySuite.Geometries; // For handling geometric data
using System;
using System.Collections.Generic;
using Geodelta.Photogrammetry;
using Geodelta.Units;
using Geodelta.Units.Length;
using System.Security.Cryptography.X509Certificates;

namespace TerrainPointExample
{
    class Program
    {
        static void Main()
        {

            // 1. Load in BAG data for AOI

            // 2. Load in footprint data for AOI

            // 3. Load in interior and exterior parameters for photos in AOI


            Millimeter millimeter = new Millimeter();

            TerrainPoint<Millimeter> point = new TerrainPoint<Millimeter>(
                    new Quantity<Millimeter>(100.0, millimeter),
                    new Quantity<Millimeter>(200.0, millimeter),
                    new Quantity<Millimeter>(50.0, millimeter),
                    "SurveyPoint1");

            Console.WriteLine(point.ToString());

            Meter meter = new Meter();

            TerrainPoint<Meter> convertedPoint = point.As(meter);
            Console.WriteLine(convertedPoint.ToString());
        }
    }
}

