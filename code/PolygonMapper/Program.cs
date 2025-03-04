using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Geodelta.Photogrammetry;
using Helpers;

namespace PolygonMapper
{
    class Program
    {
        static async Task Main()
        {
            Console.WriteLine("Starting PolygonMapper Program");

            // 0. Test the connection to the database
            DatabaseHelper.TestDatabaseConnection();

            // 1. Loading imagePlanes into memory
            List<ImagePlane> imagePlanes = await DatabaseHelper.GetImagePlanes();
            Console.WriteLine($"{imagePlanes.Count} image planes loaded into memory");

            // 2. Proccess 2D terrain polygons to 2D image polygons and load them back to database
            await DatabaseHelper.ProcessIntersectionGeometries(imagePlanes);

        }
    }
}