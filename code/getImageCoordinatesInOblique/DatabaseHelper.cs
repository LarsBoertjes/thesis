﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using Geodelta.Photogrammetry;
using Geodelta.Photogrammetry.IO.Exif;
using Geodelta.LinearAlgebra.Rotation;
using Geodelta.Units;
using Geodelta.Units.Length;
using Geodelta.Units.Angle;
using Npgsql;
using Geodelta.CoordinateReferenceSystems.Grids;
using NetTopologySuite.IO;
using NetTopologySuite.Geometries;
using Geodelta.IO.Xml;

namespace Helpers
{
    public static class DatabaseHelper
    {
        // Connecting to PostgreSQL database
        private static string ConnectionString => $"Host=localhost;Username=postgres;Password={Environment.GetEnvironmentVariable("DB_PASSWORD")};Database=aerial_bag_db";

        // Get camera specifications for Back, Fwd, Left and Right
        public static async Task<List<CameraSpecs>> GetCameraSpecsAsync()
        {
            // Initialize custom cameraSpecList that reads in all columns from camera_specs table
            List<CameraSpecs> cameraSpecsList = new();
            Micrometer micrometer = new();
            Millimeter millimeter = new();

            // Connecting to database and querying all rows from camera_specs
            await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
            await using var cmd = dataSource.CreateCommand("SELECT * FROM camera_specs");
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var cameraId = reader.GetString(0);
                var pixelSize = new Quantity<Micrometer>(reader.GetDouble(1), micrometer);
                var widthPx = reader.GetInt32(2);
                var heightPx = reader.GetInt32(3);
                var focalLength = new Quantity<Millimeter>(reader.GetDouble(4), millimeter);
                var ppx = new Quantity<Millimeter>(reader.GetDouble(5), millimeter);
                var ppy = new Quantity<Millimeter>(reader.GetDouble(6), millimeter);

                cameraSpecsList.Add(new CameraSpecs(cameraId, pixelSize, widthPx, heightPx, focalLength, ppx, ppy));
            }

            return cameraSpecsList;
        }

        // Constructing ImagePlane objects
        public static async Task<List<ImagePlane>> GetImagePlanesAsync(List<CameraSpecs> cameraSpecsList)
        {
            List<ImagePlane> imagePlanes = new();
            Meter meter = new();
            Degree degree = new();
            Millimeter millimeter = new();

            // Connecting to database and querying all rows from image_specs
            await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
            await using var cmd = dataSource.CreateCommand("SELECT * FROM image_specs");
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var imageId = reader.GetString(0);
                var X = new Quantity<Meter>(reader.GetDouble(1), meter);
                var Y = new Quantity<Meter>(reader.GetDouble(2), meter);
                var Z = new Quantity<Meter>(reader.GetDouble(3), meter);
                var Omega = new Quantity<Degree>(reader.GetDouble(4), degree);
                var Phi = new Quantity<Degree>(reader.GetDouble(5), degree);
                var Kappa = new Quantity<Degree>(reader.GetDouble(6), degree);
                var cameraId = reader.GetString(7);

                // Creating ExteriorOrientation
                var opticalCenter = new TerrainPoint<Meter>(X, Y, Z);
                IRotation3D attitude = new RotationOPK(Omega, Phi, Kappa);
                var exteriorOrientation = new ExteriorOrientation { OpticalCenter = opticalCenter, Attitude = attitude };

                // Retrieving necessary information from camera_specs list
                var matchingCameraSpecs = cameraSpecsList.Find(c => c.CameraId == cameraId);

                // Creating InteriorOrientation
                var interiorOrientation = new InteriorOrientation();

                if (matchingCameraSpecs != null)
                {
                    var principalPoint = new PrincipalPoint(matchingCameraSpecs.PrincipalPointX, matchingCameraSpecs.PrincipalPointY);
                    var sensorWidth = new Quantity<Millimeter>(matchingCameraSpecs.PixelSize.Value * matchingCameraSpecs.WidthPx / 1000, millimeter);
                    var sensorHeight = new Quantity<Millimeter>(matchingCameraSpecs.PixelSize.Value * matchingCameraSpecs.HeightPx / 1000, millimeter);

                    interiorOrientation.FocalLength = matchingCameraSpecs.FocalLength;
                    interiorOrientation.PrincipalPoint = principalPoint;
                    interiorOrientation.Sensor = new ImageSensor(sensorWidth, sensorHeight, matchingCameraSpecs.PixelSize);
                }

                // Adding imagePlane to list
                imagePlanes.Add(new ImagePlane(exteriorOrientation, interiorOrientation, imageId));
            }

            return imagePlanes;
        }

        public static async Task<Dictionary<string, List<ImagePoint>>> GetImageBagCoordinates(List<ImagePlane> imagePlanes)
        {
            // Dictionary to store the result
            Dictionary<string, List<ImagePoint>> imageBagCoordinates = new();

            // Connecting to database and query image_id + BAG location point (3D with z=0)
            await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
            await using var cmd = dataSource.CreateCommand(@"
            SELECT imageid, ST_AsText(geom) 
            FROM image_prompts_3d, unnest(bag_coordinates) AS geom;");
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                
                string imageId = reader.GetString(0); // Read imageid
                string wkt = reader.GetString(1);     // Read 3D geometry as WKT (Well-Known Text)

                // Create a new Key if image_id is not already in dictionary
                if (!imageBagCoordinates.ContainsKey(imageId))
                    imageBagCoordinates[imageId] = new List<ImagePoint>();

                if (!string.IsNullOrWhiteSpace(wkt))
                {
                    if (wkt.StartsWith("POINT("))
                    {
                        // Store WKT coordinates in string list
                        string[] coordinates = wkt.Replace("POINT(", "").Replace(")", "").Trim().Split(' ');

                        double x = double.Parse(coordinates[0], CultureInfo.InvariantCulture);
                        double y = double.Parse(coordinates[1], CultureInfo.InvariantCulture);

                        // Get the corresponding imagePlane object for the Image
                        var matchingImagePlane = imagePlanes.Find(c => c.Name + ".jpg" == imageId);
                        if (matchingImagePlane == null)
                        {
                            Console.WriteLine($"Warning: no ImagePlane found for ImageId {imageId}");
                            continue;
                        } 

                        // Create 3D terrain point (TODO: fix z = 0.0)
                        TerrainPoint terrainpoint = new TerrainPoint(x, y, 0.0, "BagObject");

                        // Get PointObservation of a terrain point on the positive image plane
                        IIdealizedPointObservation IdealizedImagePoint2D = terrainpoint.ProjectOntoImagePlane(matchingImagePlane);

                        // TODO: Convert PointObservation to Imagepoint as point coordinates (origin upperleft corner)
                        
                        // TODO: Add Image point to corresponding image
                        // imageBagCoordinates[imageId].Add(ImagePoint2D);
                    }
                }
            }

            return imageBagCoordinates;
        }

        // TODO upload all image_prompts_2d to database table image_prompts_2d

        /*public static async Task UploadImagePrompts(Dictionary<string, List<ImagePoint>> imagePrompts)
        {
            // Connecting to the database
            await using var dataSource = NpgsqlDataSource.Create(ConnectionString);

            // Prepare the SQL command for inserting data
            await using var cmd = dataSource.CreateCommand(@"
                INSERT INTO image_prompts_2d (imageid, x_prompt, y_prompt)
                VALUES (@imageid, @x_prompt, @y_prompt);");

            // Add parameters to the command
            cmd.Parameters.Add(new NpgsqlParameter("imageid", NpgsqlTypes.NpgsqlDbType.Text));
            cmd.Parameters.Add(new NpgsqlParameter("x_prompt", NpgsqlTypes.NpgsqlDbType.Double));
            cmd.Parameters.Add(new NpgsqlParameter("y_prompt", NpgsqlTypes.NpgsqlDbType.Double));

            // Iterate over the dictionary and insert each ImagePoint
            foreach (var kvp in imagePrompts)
            {
                string imageId = kvp.Key;
                List<ImagePoint> imagePoints = kvp.Value;

                foreach (var imagePoint in imagePoints)
                {
                    // Set the parameter values
                    cmd.Parameters[0].Value = imageId;
                    cmd.Parameters[1].Value = imagePoint.X


                }
            }
        }*/

        public static ImagePoint ConvertMmToPixelCoordinates(double x_mm, double y_mm, double pixelSize, double imageWidth, double imageHeight, PrincipalPoint principalPoint)
        {
            // TODO: Finish Conversion function.
            return new ImagePoint(0, 0);
        }
    }
}
