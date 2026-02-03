#nullable disable
using Aglomera;
using Medley.Domain.Enums;

namespace Medley.Application.Services;

/// <summary>
/// Wrapper for fragment embeddings to work with Aglomera clustering library
/// Implements IDissimilarityMetric for distance calculation
/// </summary>
public class DataPoint : IEquatable<DataPoint>, IDissimilarityMetric<DataPoint>, IComparable<DataPoint>
{
    public Guid Id { get; }
    public float[] Embedding { get; }
    public DistanceMetric DistanceMetric { get; }

    public DataPoint(Guid id, float[] embedding, DistanceMetric distanceMetric = DistanceMetric.Cosine)
    {
        Id = id;
        Embedding = embedding ?? throw new ArgumentNullException(nameof(embedding));
        DistanceMetric = distanceMetric;
    }

    public DataPoint()
    {
        Id = Guid.Empty;
        Embedding = Array.Empty<float>();
        DistanceMetric = DistanceMetric.Cosine;
    }

    /// <summary>
    /// Calculates distance between two data points using the configured metric
    /// </summary>
    public double Calculate(DataPoint x, DataPoint y)
    {
        return x.DistanceMetric switch
        {
            DistanceMetric.Euclidean => CalculateEuclideanDistance(x.Embedding, y.Embedding),
            DistanceMetric.Cosine => CalculateCosineDistance(x.Embedding, y.Embedding),
            _ => CalculateCosineDistance(x.Embedding, y.Embedding)
        };
    }

    /// <summary>
    /// Calculates Euclidean distance (L2 norm) between two embeddings
    /// Traditional distance metric for Ward linkage
    /// </summary>
    private double CalculateEuclideanDistance(float[] x, float[] y)
    {
        if (x.Length != y.Length)
            throw new ArgumentException("Embeddings must have the same length");

        double sum = 0.0;
        for (int i = 0; i < x.Length; i++)
        {
            var delta = x[i] - y[i];
            sum += delta * delta;
        }

        return Math.Sqrt(sum);
    }

    /// <summary>
    /// Calculates cosine distance (1 - cosine similarity) between two embeddings
    /// Recommended for semantic embeddings as it measures angular similarity
    /// </summary>
    private double CalculateCosineDistance(float[] x, float[] y)
    {
        if (x.Length != y.Length)
            throw new ArgumentException("Embeddings must have the same length");

        double dotProduct = 0.0;
        double magnitudeX = 0.0;
        double magnitudeY = 0.0;

        for (int i = 0; i < x.Length; i++)
        {
            dotProduct += x[i] * y[i];
            magnitudeX += x[i] * x[i];
            magnitudeY += y[i] * y[i];
        }

        magnitudeX = Math.Sqrt(magnitudeX);
        magnitudeY = Math.Sqrt(magnitudeY);

        if (magnitudeX == 0.0 || magnitudeY == 0.0)
            return 1.0; // Maximum distance if either vector is zero

        var cosineSimilarity = dotProduct / (magnitudeX * magnitudeY);
        return 1.0 - cosineSimilarity; // Convert similarity to distance
    }

    /// <summary>
    /// Calculates the centroid (average) of a cluster of data points
    /// </summary>
    public static DataPoint GetCentroid(Cluster<DataPoint> cluster)
    {
        if (cluster.Count == 1) 
            return cluster.First();

        var points = cluster.ToList();
        var dimensions = points[0].Embedding.Length;
        var centroid = new float[dimensions];
        var distanceMetric = points[0].DistanceMetric;

        foreach (var point in points)
        {
            for (int i = 0; i < dimensions; i++)
            {
                centroid[i] += point.Embedding[i];
            }
        }

        for (int i = 0; i < dimensions; i++)
        {
            centroid[i] /= points.Count;
        }

        return new DataPoint(Guid.NewGuid(), centroid, distanceMetric);
    }

    public int CompareTo(DataPoint other)
    {
        if (other == null) return 1;
        return Id.CompareTo(other.Id);
    }

    public bool Equals(DataPoint other)
    {
        if (other == null) return false;
        return Id.Equals(other.Id);
    }

    public override bool Equals(object obj)
    {
        return obj is DataPoint other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public override string ToString()
    {
        return Id.ToString();
    }

    public static bool operator ==(DataPoint left, DataPoint right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(DataPoint left, DataPoint right)
    {
        return !(left == right);
    }
}
