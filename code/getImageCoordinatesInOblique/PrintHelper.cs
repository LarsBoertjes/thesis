using System;
using Geodelta.Photogrammetry;
using Geodelta.LinearAlgebra.Rotation;

namespace Helpers
{
    public static class PrintHelper
    {
        public static void PrintImagePlaneInformation(ImagePlane imagePlane)
        {
            Console.WriteLine($"Image: {imagePlane.Name}");
            Console.WriteLine($"Optical Center: X={imagePlane.ExteriorOrientation.OpticalCenter.X}, " +
                              $"Y={imagePlane.ExteriorOrientation.OpticalCenter.Y}, " +
                              $"Z={imagePlane.ExteriorOrientation.OpticalCenter.Z}");

            if (imagePlane.ExteriorOrientation.Attitude is IRotation3D rotation)
            {
                var opk = rotation.ToOPK();
                Console.WriteLine($"Rotation Values: Omega={opk.Omega}, Phi={opk.Phi}, Kappa={opk.Kappa}");
            }
            else
            {
                Console.WriteLine("Rotation values could not be extracted.");
            }

            if (imagePlane.InteriorOrientation.Sensor != null)
            {
                var sensor = imagePlane.InteriorOrientation.Sensor;
                Console.WriteLine($"Sensor Width: {sensor.Width}");
                Console.WriteLine($"Sensor Height: {sensor.Height}");
                Console.WriteLine($"Pixel Size: {sensor.PixelWidth}");
                Console.WriteLine($"Resolution: {sensor.WidthInPixels} x {sensor.HeightInPixels} pixels");
            }
        }
    }
}
