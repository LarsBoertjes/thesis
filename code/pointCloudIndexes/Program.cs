using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Accord;
using Accord.Collections;
using Accord.Math;
using Accord.Math.Decompositions;

class Program
{
    static void Main()
    {
        var config = AppConfig.LoadDefaults();
        var denseCloud = PointCloud.LoadFromOBJ(config.InputFilePath);
        var currentCloud = PointCloud.LoadFromOBJ(config.InputFileSimplified);

        string baseOutputDir = "../../../../../data/processed/4_large_building_aerial/adaptive_point_cloud/";
          

        // 1. Compute high entropy points (combined)
        var analyzer = new PointAnalyzer(currentCloud, 10);
        var (curv, dens, edge, comb) = analyzer.Analyze();

        // 2. Sample new halfway points
        var cumulativePoints = new List<double[]>(currentCloud.Points);
        var sampledPoints = new List<double[]>();
        Console.WriteLine($"Number of high entropy points: {comb.Count}");

        foreach (var point in comb)
        {
            // Find the half way point with nearest neighbor and add it to sampledPoints
            var halfway = findHalfwayPoint(currentCloud, denseCloud, point.point);
            sampledPoints.Add(halfway);
        }

        cumulativePoints.AddRange(sampledPoints);

        string outputPath = Path.Combine(baseOutputDir, $"halfway_points.obj");
        PointCloudWriter.WriteOBJ(outputPath, cumulativePoints.Select(pt => (pt, 0.0)).ToList());
        Console.WriteLine($"Written as {outputPath}");

    }


    static double[] findHalfwayPoint(PointCloud Cloud, PointCloud denseCloud, double[] point)
    {
        // Find nearest neighbor for high entropy point
        var kdTree = KDTree.FromData<float>(Cloud.Points.ToArray());
        var nearest = kdTree.Nearest(point, 2);

        var nearestPoint = nearest[1].Node.Position;

        // Console.WriteLine($"POINT X: {point[0].ToString()}, Y: {point[1].ToString()}, Z: {point[2].ToString()}");
        // Console.WriteLine($"NN    X: {nearestPoint[0].ToString()}, Y: {nearestPoint[1].ToString()}, Z: {nearestPoint[2].ToString()}");

        // Compute halfway point
        var halfway = new double[point.Length];
        for (int i = 0; i < point.Length; i++)
        {
            halfway[i] = (point[i] + nearestPoint[i]) / 2.0;
        }

        // Get dense point cloud tree
        var kdTreeDense = KDTree.FromData<float>(denseCloud.Points.ToArray());
        var halfwayNeighbor = kdTreeDense.Nearest(halfway, 1)[0];

        double[] halfwayPoint = halfwayNeighbor.Node.Position;

        return halfwayPoint;
    }

    static List<double[]> FindNearestNeighbors(PointCloud denseCloud, double[] point, int k)
    {
        var kdTree = KDTree.FromData<float>(denseCloud.Points.ToArray());
        var nearestNeighbors = kdTree.Nearest(point, k);
        return nearestNeighbors.Select(n => n.Node.Position).ToList();
    }

}

class AppConfig
{
    public string InputFilePath { get; set; }
    public string InputFileSimplified { get; set; }
    public string OutputCurvaturePath { get; set; }
    public string OutputDensityPath { get; set; }
    public string OutputEdgePath { get; set; }
    public int NumNeighbors { get; set; } = 25;
    public int NumIterations { get; set; } = 5;
    public double CurvatureWeight { get; set; } = 1.0 / 3;
    public double DensityWeight { get; set; } = 1.0 / 3;
    public double EdgeWeight { get; set; } = 1.0 / 3;
    public string DensificationMethod { get; set; } = "knn"; 

    public static AppConfig LoadDefaults()
    {
        return new AppConfig
        {
            InputFilePath = "../../../../../data/processed/4_large_building_aerial/adaptive_point_cloud/dense_cut_out.obj",
            InputFileSimplified = "../../../../../data/processed/4_large_building_aerial/adaptive_point_cloud/simplified_100.obj",
            OutputCurvaturePath = "../../../../../data/processed/4_large_building_aerial/entropy_point_cloud/curvature/high_curvature_points.obj",
            OutputDensityPath = "../../../../../data/processed/4_large_building_aerial/entropy_point_cloud/density/high_density_points.obj",
            OutputEdgePath = "../../../../../data/processed/4_large_building_aerial/entropy_point_cloud/edge/high_edge_points.obj",
        };
    }
}

class PointCloud
{
    public List<double[]> Points { get; }

    public PointCloud(List<double[]> points) => Points = points;

    public static PointCloud LoadFromOBJ(string path)
    {
        var points = File.ReadLines(path)
            .Where(line => line.StartsWith("v "))
            .Select(line =>
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return new double[]
                {
                    double.Parse(parts[1], CultureInfo.InvariantCulture),
                    double.Parse(parts[2], CultureInfo.InvariantCulture),
                    double.Parse(parts[3], CultureInfo.InvariantCulture)
                };
            }).ToList();

        return new PointCloud(points);
    }
}

class PointAnalyzer
{
    private readonly KDTree<float> _tree;
    private readonly int _numNeighbors;

    public PointAnalyzer(PointCloud cloud, int numNeighbors)
    {
        _numNeighbors = numNeighbors;
        _tree = KDTree.FromData<float>(cloud.Points.ToArray());
    }

    public (List<(double[] point, double score)> TopCurvature,
            List<(double[] point, double score)> TopDensity,
            List<(double[] point, double score)> TopEdge,
            List<(double[] point, double score)> TopCombined) Analyze()
    {
        var curvatureMap = _tree.ToDictionary(
            node => node,
            node => ComputePointCurvature(node)
        );

        var curvatures = new List<(double[], double)>();
        var densities = new List<(double[], double)>();
        var edges = new List<(double[], double)>();
        var combined = new List<(double[], double)>();

        double wCurvature = 1.0 / 3;
        double wDensity = 1.0 / 3;
        double wEdges = 1.0 / 3;

        foreach (var node in _tree)
        {
            var curvatureValue = ComputeCurvature(node, curvatureMap);
            var densityValue = ComputeDensity(node);
            var edgeValue = ComputeEdge(node);

            curvatures.Add((node.Position, curvatureValue));
            densities.Add((node.Position, densityValue));
            edges.Add((node.Position, edgeValue));

            // Calculate the combined score using the weights
            var combinedScore = curvatureValue * wCurvature + densityValue * wDensity + edgeValue * wEdges;
            combined.Add((node.Position, combinedScore));
        }

        // Sort the scores and take the top 50% of the points based on the combined score
        return (
            curvatures.OrderByDescending(p => p.Item2).TakePercent(50).ToList(),
            densities.OrderByDescending(p => p.Item2).TakePercent(50).ToList(),
            edges.OrderByDescending(p => p.Item2).TakePercent(50).ToList(),
            combined.OrderByDescending(p => p.Item2).TakePercent(50).ToList()
        );
    }



    private double ComputePointCurvature(KDTreeNode<float> point)
    {
        var neighbors = _tree.Nearest(point.Position, _numNeighbors);
        if (neighbors.Count == 0) return 0;

        var centroid = neighbors.Select(n => n.Node.Position).Aggregate(new double[3], (sum, p) => sum.Add(p)).Divide(neighbors.Count);

        var covariance = new double[3, 3];
        foreach (var neighbor in neighbors)
        {
            var diff = neighbor.Node.Position.Subtract(centroid);
            covariance = covariance.Add(diff.Outer(diff));
        }
        covariance = covariance.Divide(neighbors.Count);

        var evd = new EigenvalueDecomposition(covariance);
        var eigenvalues = evd.RealEigenvalues;
        var sum = eigenvalues.Sum();

        return sum == 0 ? 0 : eigenvalues.Min() / sum;
    }

    private double ComputeCurvature(KDTreeNode<float> point, Dictionary<KDTreeNode<float>, double> curvatureMap)
    {
        var neighbors = _tree.Nearest(point.Position, _numNeighbors);
        if (!curvatureMap.ContainsKey(point)) return 0;

        var values = new List<double> { curvatureMap[point] };
        values.AddRange(neighbors.Select(n => curvatureMap.GetValueOrDefault(n.Node, 0)));

        var sum = values.Sum();
        if (sum == 0) return 0;

        var probabilities = values.Select(v => v / sum).ToList();
        var entropy = -probabilities.Where(p => p > 0).Sum(p => p * Math.Log(p));
        var maxEntropy = Math.Log(probabilities.Count);

        return maxEntropy == 0 ? 0 : entropy / maxEntropy;
    }

    private double ComputeDensity(KDTreeNode<float> point)
    {
        var neighbors = _tree.Nearest(point.Position, 10);
        if (neighbors.Count == 0) return 0;

        var totalDistance = neighbors.Sum(n =>
            Math.Sqrt(point.Position.Zip(n.Node.Position, (a, b) => (a - b) * (a - b)).Sum())
        );

        return Math.Round(1.0 / (totalDistance / neighbors.Count), 6);
    }

    private double ComputeEdge(KDTreeNode<float> point)
    {
        var neighbors = _tree.Nearest(point.Position, 10);
        if (neighbors.Count == 0) return 0;

        var center = new float[point.Position.Length];
        foreach (var n in neighbors)
        {
            for (int i = 0; i < center.Length; i++)
                center[i] += (float)n.Node.Position[i];
        }
        for (int i = 0; i < center.Length; i++)
            center[i] /= neighbors.Count;

        return Math.Round(Math.Sqrt(point.Position.Zip(center, (a, b) => (a - b) * (a - b)).Sum()), 6);
    }
}

static class PointCloudWriter
{
    public static void WriteOBJ(string path, List<(double[] point, double score)> data)
    {
        using var writer = new StreamWriter(path);
        foreach (var (point, _) in data)
        {
            writer.WriteLine($"v {point[0].ToString(CultureInfo.InvariantCulture)} {point[1].ToString(CultureInfo.InvariantCulture)} {point[2].ToString(CultureInfo.InvariantCulture)}");
        }
    }
}

static class Extensions
{
    public static IEnumerable<T> TakePercent<T>(this IEnumerable<T> source, double percent)
    {
        var list = source.ToList();
        int count = (int)(list.Count * percent / 100);
        return list.Take(count);
    }

    public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue fallback)
    {
        return dict.TryGetValue(key, out var value) ? value : fallback;
    }
}

class CombinedScorer
{
    public static List<(double[] point, double score)> Compute(
        List<(double[] point, double score)> curvature,
        List<(double[] point, double score)> density,
        List<(double[] point, double score)> edge,
        double wCurvature, double wDensity, double wEdge)
    {
        // Create a dictionary to hold the combined scores
        var map = new Dictionary<string, double>();

        // Add curvature scores
        foreach (var (pt, val) in curvature)
            map[string.Join(",", pt)] = wCurvature * val;

        // Add density scores
        foreach (var (pt, val) in density)
            map[string.Join(",", pt)] += wDensity * val;

        // Add edge scores
        foreach (var (pt, val) in edge)
            map[string.Join(",", pt)] += wEdge * val;

        // Return the list of points with combined scores, ordered by the score
        return map.Select(kv => (kv.Key.Split(',').Select(double.Parse).ToArray(), kv.Value))
                  .OrderByDescending(p => p.Item2)
                  .ToList();
    }
}





