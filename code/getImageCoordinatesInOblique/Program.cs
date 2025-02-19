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

            Dictionary<string, List<ImagePoint>> imagePrompts = await DatabaseHelper.GetImageBagCoordinates(imagePlanes);

            foreach (string imageId in imagePrompts.Keys)
            {
                Console.WriteLine($"Image {imageId} has {imagePrompts[imageId].Count} buildings in the image. ");
            }

        }
    }
}
