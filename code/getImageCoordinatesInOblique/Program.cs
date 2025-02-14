using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Helpers;
using Geodelta.Photogrammetry;

namespace TerrainPointExample
{
    class Program
    {
        static async Task Main()
        {
            Console.WriteLine("Loading camera specifications...");
            var cameraSpecsList = await DatabaseHelper.GetCameraSpecsAsync();

            Console.WriteLine("Loading image planes...");
            var imagePlanes = await DatabaseHelper.GetImagePlanesAsync(cameraSpecsList);

            Console.WriteLine($"There are {imagePlanes.Count} image planes constructed.");
            PrintHelper.PrintImagePlaneInformation(imagePlanes[0]);
            PrintHelper.PrintImagePlaneInformation(imagePlanes[^1]);
        }
    }
}
