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

            // Reading database for TerrainPoints and converting them to ImagePoints using imagePlanes
            Dictionary<string, List<ImagePoint>> imagePrompts = await DatabaseHelper.GetImageBagCoordinates(imagePlanes);

            // Upload all the ImagePoints (2D prompts for SAM) to database by Imageid
            await DatabaseHelper.UploadImagePrompts(imagePrompts);
            
        }
    }
}
