using System;
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
using NpgsqlTypes;

namespace Helpers
{
    public static class DatabaseHelper
    {
        // Connecting to PostgreSQL database
        private static string ConnectionString => $"Host=localhost;Username=postgres;Password={Environment.GetEnvironmentVariable("DB_PASSWORD")};Database=BagMapDB";

        public static void TestDatabaseConnection()
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    Console.WriteLine("Connection to the database was succesfull");

                    using (var command = new NpgsqlCommand("SELECT version();", connection))
                    {
                        var version = command.ExecuteScalar().ToString();
                        Console.WriteLine($"Database version: {version}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occured while connecting to the database: {ex.Message}");
                }
            }
            
        }

        // Constructing ImagePlane objects
        public static async Task<List<ImagePlane>> GetImagePlanes()
        {
            List<ImagePlane> imagePlanes = new List<ImagePlane>();
            Micrometer micrometer = new Micrometer();
            Millimeter millimeter = new Millimeter();
            Meter meter = new Meter();
            Degree degree = new Degree();

            // Load the images for the testarea
            List<String> imagePlaneNames = new List<String>();
            await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
            await using var cmd = dataSource.CreateCommand("SELECT i.image_id,i.x_m,i.y_m,i.z_m,i.omega_deg,i.phi_deg,i.kappa_deg,i.camera_id,c.pixel_size_um,c.width_px,c.height_px,c.focal_length_mm,c.ppx_mm,c.ppy_mm FROM image_specs i JOIN camera_specs c ON i.camera_id = c.camera_id;");
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                // read all the values from the database
                string image_id = reader.GetString(0);
                Quantity<Meter> X = new Quantity<Meter>(reader.GetDouble(1), meter);
                Quantity<Meter> Y = new Quantity<Meter>(reader.GetDouble(2), meter);
                Quantity<Meter> Z = new Quantity<Meter>(reader.GetDouble(3), meter);
                Quantity<Degree> Omega = new Quantity<Degree>(reader.GetDouble(4), degree);
                Quantity<Degree> Phi = new Quantity<Degree>(reader.GetDouble(5), degree);
                Quantity<Degree> Kappa = new Quantity<Degree>(reader.GetDouble(6), degree);
                string camera_id = reader.GetString(7);
                Quantity<Micrometer> pixelSize = new Quantity<Micrometer>(reader.GetDouble(8), micrometer);
                int widthPX = reader.GetInt32(9);
                int heightPX = reader.GetInt32(10);
                Quantity<Millimeter> focalLength = new Quantity<Millimeter>(reader.GetDouble(11), millimeter);
                Quantity<Millimeter> ppx = new Quantity<Millimeter>(reader.GetDouble(12), millimeter);
                Quantity<Millimeter> ppy = new Quantity<Millimeter>(reader.GetDouble(13), millimeter);

                // Constructing interiorOrientation
                ImageSensor imageSensor = new ImageSensor(pixelSize, widthPX, heightPX);
                InteriorOrientation interiorOrientation = new InteriorOrientation();
                interiorOrientation.FocalLength = focalLength;
                interiorOrientation.PrincipalPoint = new PrincipalPoint(ppx, ppy);
                interiorOrientation.Sensor = imageSensor;

                // Constructing exteriorOrientation
                ExteriorOrientation exteriorOrientation = new ExteriorOrientation();
                exteriorOrientation.Attitude = new RotationOPK(Omega, Phi, Kappa);
                exteriorOrientation.OpticalCenter = new TerrainPoint<Meter>(X, Y, Z);

                // Constructing imagePlane
                ImagePlane imagePlane = new ImagePlane(exteriorOrientation, interiorOrientation, image_id + ".jpg");
                
                // adding imagePlanes to the list
                imagePlanes.Add(imagePlane);
            }

            return imagePlanes;
        }

        // Load polygons into memory 
        public static async Task ProcessIntersectionGeometries(List<ImagePlane> imagePlanes)
        {
            // Load everything into memory
            var bagGeometries = await LoadGeometriesIntoMemory();
            var imageBagMap = await LoadImageBagRelationships();

            Console.WriteLine($"{bagGeometries.Count} Geometries loaded into memory");
            Console.WriteLine($"{imageBagMap.Count} images loaded into memory");

            var imagePlaneLookup = imagePlanes.ToDictionary(ip => ip.Name, ip => ip);

            // Dictionary to store results for updating the database
            var updatedImageBBoxes = new Dictionary<string, List<string>>();

            foreach (var (imageName, bagIds) in imageBagMap)
            {
                if (!imagePlaneLookup.TryGetValue(imageName, out var imagePlane))
                {
                    Console.WriteLine($"No image plane found for {imageName}");
                    continue;
                }

                var imageBoundingBoxes = new List<string>();

                foreach (var bagId in bagIds)
                {
                    if (!bagGeometries.TryGetValue(bagId, out var wktGeometry))
                    {
                        Console.WriteLine($"No geometry found for {bagId}");
                        continue;
                    }

                    List<TerrainPoint> worldPoints = ParseWKTToPoints(wktGeometry);
                    List<ImagePoint> imagePoints = ConvertWorldToImagePoints(worldPoints, imagePlane);
                    string bboxWKT = GetBoundingBoxWKT(imagePoints);
                    imageBoundingBoxes.Add(bboxWKT);
                }

                updatedImageBBoxes[imageName] = imageBoundingBoxes;
            }

            await UpdateBoundingBoxesInDatabase(updatedImageBBoxes);

        }

        private static List<TerrainPoint> ParseWKTToPoints(string wkt)
        {
            List<TerrainPoint> points = new();

            wkt = wkt.Replace("MULTIPOLYGON(((", "").Replace("POLYGON((", "").Replace("MULTILINESTRING((", "")
             .Replace("LINESTRING(", "").Replace("MULTIPOINT(", "").Replace("POINT(", "")
             .Replace(")))", "").Replace("))", "").Replace(")", "").Replace("(", "");

            string[] coordinatePairs = wkt.Split(',');

            foreach (string pair in coordinatePairs)
            {
                string[] coords = pair.Trim().Split(' ');

                if (coords.Length >= 2) 
                {
                    double x = double.Parse(coords[0], CultureInfo.InvariantCulture);
                    double y = double.Parse(coords[1], CultureInfo.InvariantCulture);


                    var point = new TerrainPoint(
                        x,
                        y,
                        0 // Setting Z to 0 for now
                    );

                    points.Add(point);
                }
            }

            return points;
        }

        private static List<ImagePoint> ConvertWorldToImagePoints(List<TerrainPoint> worldPoints, ImagePlane imagePlane)
        {
            List<ImagePoint> imagePoints = new();

            foreach (TerrainPoint point in worldPoints)
            {
                IIdealizedPointObservation IdealizedImagePoint = point.ProjectOntoImagePlane(imagePlane);
                var imagePoint = imagePlane.InteriorOrientation.UnapplyCorrection(IdealizedImagePoint);

                var imagePointPixel = imagePlane.InteriorOrientation.Sensor.ConvertToPixelCoordinates(imagePoint, PhotoXAxis.PositiveColumn);
                //var imagePointPixel = IdealizedToImagePoint(IdealizedImagePoint, imagePlane);

                imagePoints.Add(imagePointPixel);
            }

            return imagePoints;
        }

        public static ImagePoint IdealizedToImagePoint(IIdealizedPointObservation idealizedPoint, ImagePlane imageplane)
        {
            // ImagePlane parameter
            Quantity<Micrometer> pixelSize = imageplane.InteriorOrientation.Sensor.PixelHeight;
            Quantity<Millimeter> Width = imageplane.InteriorOrientation.Sensor.Width;
            Quantity<Millimeter> Height = imageplane.InteriorOrientation.Sensor.Height;
            Quantity<Millimeter> FocalLength = imageplane.InteriorOrientation.FocalLength;
            PrincipalPoint principalPoint = imageplane.InteriorOrientation.PrincipalPoint;
            Quantity<Millimeter> ppx = principalPoint.X;
            Quantity<Millimeter> ppy = principalPoint.Y;

            // Idealized Point parameters
            Quantity<Millimeter> IdealizedX = idealizedPoint.X;
            Quantity<Millimeter> IdealizedY = idealizedPoint.Y;

            // Convert Idealized coordinates to image coordinates (relative to principal point)
            Quantity<Millimeter> imageX = IdealizedX + ppx;
            Quantity<Millimeter> imageY = IdealizedY + ppy;

            // Convert from millimeters to pixels
            double pixelSizeInMillimeters = pixelSize.As<Millimeter>().Value;
            int pixelX = (int)(imageX.Value / pixelSizeInMillimeters);
            int pixelY = (int)(imageY.Value / pixelSizeInMillimeters);

            // Adjust for the origin (upper-left corner)
            int imageWidthInPixels = (int)(Width.Value / pixelSizeInMillimeters);
            int imageHeightInPixels = (int)(Height.Value / pixelSizeInMillimeters);

            // Shift the origin from the center (principal point) to the upper-left corner
            pixelX = pixelX + (imageWidthInPixels / 2);
            pixelY = (imageHeightInPixels / 2) - pixelY;

            // TODO: Finish Conversion function.
            return new ImagePoint(pixelX, pixelY);
        }

        public static async Task<Dictionary<string, string>> LoadGeometriesIntoMemory()
        {
            var bagGeometries = new Dictionary<string, string>();

            await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
            await using var cmd = dataSource.CreateCommand("SELECT bagid, ST_AsText(geom) FROM bag_in_utrecht;");
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                string bagId = reader.GetString(0);
                string wktGeometry = reader.GetString(1);
                bagGeometries[bagId] = wktGeometry;
            }

            return bagGeometries;
        }

        public static async Task<Dictionary<string, List<string>>> LoadImageBagRelationships()
        {
            var imageBagMap = new Dictionary<string, List<string>>();

            await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
            await using var cmd = dataSource.CreateCommand("SELECT image_name, bag_ids FROM bag_in_image_utrecht;");
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                string imageName = reader.GetString(0);
                List<string> bagIds = reader.GetFieldValue<string[]>(1).ToList(); 

                imageBagMap[imageName] = bagIds;
            }

            return imageBagMap;
        }

        public static string GetBoundingBoxWKT(List<ImagePoint> imagePoints)
        {
            // Clamp min and max values within bounds
            double minX = Math.Max(0, imagePoints.Min(p => p.Row));
            double minY = Math.Max(0, imagePoints.Min(p => p.Column));
            double maxX = Math.Min(14204, imagePoints.Max(p => p.Row));
            double maxY = Math.Min(10652, imagePoints.Max(p => p.Column));

            // Return a properly formatted WKT string for a polygon
            return $"POLYGON(({minX.ToString(CultureInfo.InvariantCulture)} {minY.ToString(CultureInfo.InvariantCulture)}, {maxX.ToString(CultureInfo.InvariantCulture)} {minY.ToString(CultureInfo.InvariantCulture)}, {maxX.ToString(CultureInfo.InvariantCulture)} {maxY.ToString(CultureInfo.InvariantCulture)}, {minX.ToString(CultureInfo.InvariantCulture)} {maxY.ToString(CultureInfo.InvariantCulture)}, {minX.ToString(CultureInfo.InvariantCulture)} {minY.ToString(CultureInfo.InvariantCulture)}))";
        }


        private static async Task UpdateBoundingBoxesInDatabase(Dictionary<string, List<string>> updatedImageBBoxes)
        {
            await using var dataSource = NpgsqlDataSource.Create(ConnectionString);

            foreach (var (imageName, bboxes) in updatedImageBBoxes)
            {
                await using var cmd = dataSource.CreateCommand(@"
            UPDATE bag_in_image_utrecht
            SET bboxes = @bboxes
            WHERE image_name = @image_name;");

                cmd.Parameters.AddWithValue("bboxes", bboxes.ToArray());
                cmd.Parameters.AddWithValue("image_name", imageName);

                await cmd.ExecuteNonQueryAsync();
            }
        }

    }
}
