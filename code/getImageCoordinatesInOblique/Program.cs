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
            // Loading camera specifications (Back, Fwd, Left and Right)
            Console.WriteLine("Loading camera specifications...");
            var cameraSpecsList = await DatabaseHelper.GetCameraSpecsAsync();

            // Constructing imagePlanes from images in image_specs table
            Console.WriteLine("Loading image planes...");
            var imagePlanes = await DatabaseHelper.GetImagePlanesAsync(cameraSpecsList);

            // Printing imagePlanes
            Console.WriteLine($"There are {imagePlanes.Count} image planes constructed.");
            PrintHelper.PrintImagePlaneInformation(imagePlanes[0]);
            PrintHelper.PrintImagePlaneInformation(imagePlanes[^1]);
        }
    }
}
