using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace GradientTest;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        RadialGradientGenerator.GenerateRadialGradientsCss("images/astralswarm.webp");
        RadialGradientGenerator.GenerateRadialGradientsCss("images/asylum.jpg");
        RadialGradientGenerator.GenerateRadialGradientsCss("images/over.png");
        RadialGradientGenerator.GenerateRadialGradientsCss("images/lilypad.png");
        RadialGradientGenerator.GenerateRadialGradientsCss("images/test.jpg");

        Console.ReadLine();
    }
}

public class GeneLinearGradient
{
    public float Angle { get; set; }

    public GeneLinearGradientKeyframe Start { get; set; }
    public GeneLinearGradientKeyframe End { get; set; }

    public GeneLinearGradient()
    {

    }

    public GeneLinearGradient(float angle, Rgb color)
    {
        Angle = angle;
        Start = new GeneLinearGradientKeyframe
        {
            Color = color
        };
        End = new GeneLinearGradientKeyframe
        {
            Color = color
        };
    }

    public GeneLinearGradient Clone()
    {
        return new GeneLinearGradient()
        {
            Angle = Angle,
            Start = new GeneLinearGradientKeyframe()
            {
                Color = Start.Color
            },
            End = new GeneLinearGradientKeyframe()
            {
                Color = End.Color
            }
        };
    }
}

public class GeneLinearGradientKeyframe
{
    public Rgb Color { get; set; }
}



public class RadialGradientGenerator
{
    private const int PopulationSize = 32;
    private const int MaxGenerations = 64;
    private const double MutationRate = 0.1;
    private const int ElitismCount = 4;
    private static readonly Random Rng = new();

    private class GradientGene
    {
        public PointF Position { get; set; }
        public Rgb Color { get; set; }
        public float Radius { get; set; }

        public GradientGene Clone()
        {
            return new GradientGene
            {
                Position = Position,
                Color = Color,
                Radius = Radius
            };
        }
    }

    private class GradientChromosome
    {
        public GeneLinearGradient Background { get; set; }
        public List<GradientGene> Genes { get; set; }
        public float Fitness { get; set; } = float.MaxValue;

        public GradientChromosome(
            GeneLinearGradient background,
            int numberOfGenes)
        {
            Background = background;
            Genes = new List<GradientGene>(numberOfGenes);
        }

        public GradientChromosome Clone()
        {
            return new GradientChromosome(Background, Genes.Count)
            {
                Genes = Genes.Select(g => g.Clone()).ToList(),
                Fitness = Fitness
            };
        }
    }

    public static string GenerateRadialGradientsCss(
        string imagePath,
        int numberOfGradients = 5)
    {
        Console.WriteLine();
        Console.WriteLine(imagePath);

        using var originalImage = Image.Load<Rgba32>(imagePath);
        originalImage.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(64, 64),
            Mode = ResizeMode.Stretch,
            // Sampler = new BicubicResampler()
        }));

        originalImage.SaveAsPng(imagePath + ".small.png");
        originalImage.SaveAsPng(imagePath + ".smallblur.png");

        var dominantColors = GetDominantColors(originalImage, numberOfGradients);

        var population = InitializePopulation(numberOfGradients, dominantColors);

        float imageDiagonal = MathF.Sqrt((originalImage.Width * originalImage.Width) + (originalImage.Height * originalImage.Height));

        for (int i = 0; i < MaxGenerations; i++)
        {
            CalculatePopulationFitness(population, originalImage, imageDiagonal);

            population = CreateNextGeneration(population);
        }

        CalculatePopulationFitness(population, originalImage, imageDiagonal);

        var bestSolution = population.First();

        var render = RenderChromosome(bestSolution, imageDiagonal, originalImage.Size);
        originalImage.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var pixelRow = accessor.GetRowSpan(y);
                for (int x = 0; x < pixelRow.Length; x++)
                {
                    ref var pixel = ref pixelRow[x];
                    pixel = render[x, y];
                }
            }
        });
        originalImage.SaveAsPng(imagePath + ".final.png");
        string output = ConvertToCss(bestSolution);
        Console.WriteLine(output);

        return output;
    }

    private static List<GradientChromosome> InitializePopulation(
        int numberOfGradients,
        List<Rgb> availableColors)
    {

        var population = new List<GradientChromosome>(PopulationSize);
        for (int i = 0; i < PopulationSize; i++)
        {
            var backgroundColor = availableColors[Rng.Next(availableColors.Count)];

            var background = new GeneLinearGradient((float)Rng.NextDouble() * 180.0f, backgroundColor);

            var chromosome = new GradientChromosome(background, numberOfGradients);
            for (int j = 0; j < numberOfGradients; j++)
            {
                chromosome.Genes.Add(new GradientGene
                {
                    Position = new PointF((float)Rng.NextDouble(), (float)Rng.NextDouble()),
                    Color = availableColors[Rng.Next(availableColors.Count)],
                    Radius = Range(0.25f, 0.75f)
                });
            }
            population.Add(chromosome);
        }
        return population;
    }

    private static void CalculatePopulationFitness(
        List<GradientChromosome> population,
        Image<Rgba32> targetImage,
        float imageDiagonal)
    {
        foreach (var chromosome in population)
        {
            chromosome.Fitness = CalculateFitness(chromosome, targetImage, imageDiagonal);
        }
        population.Sort((a, b) => a.Fitness.CompareTo(b.Fitness));
    }

    private static List<GradientChromosome> CreateNextGeneration(
        List<GradientChromosome> currentPopulation)
    {
        var newPopulation = new List<GradientChromosome>(PopulationSize);

        // Elitism
        for (int i = 0; i < ElitismCount; i++)
        {
            newPopulation.Add(currentPopulation[i].Clone());
        }

        // Crossover and mutation
        while (newPopulation.Count < PopulationSize)
        {
            var parent1 = SelectParent(currentPopulation);
            var parent2 = SelectParent(currentPopulation);
            var child = Crossover(parent1, parent2);
            Mutate(child);
            newPopulation.Add(child);
        }
        return newPopulation;
    }

    private static GradientChromosome SelectParent(
        List<GradientChromosome> population)
    {
        const int tournamentSize = 3;
        var tournamentContestants = new List<GradientChromosome>();
        for (int i = 0; i < tournamentSize; i++)
        {
            tournamentContestants.Add(population[Rng.Next(population.Count)]);
        }
        return tournamentContestants.OrderBy(c => c.Fitness).First();
    }

    private static GradientChromosome Crossover(
        GradientChromosome parent1,
        GradientChromosome parent2)
    {
        var child = new GradientChromosome(parent1.Background, parent1.Genes.Count)
        {
            Background = Rng.NextDouble() < 0.5 ? parent1.Background.Clone() : parent2.Background.Clone()
        };
        for (int i = 0; i < parent1.Genes.Count; i++)
        {
            child.Genes.Add(Rng.NextDouble() < 0.5 ? parent1.Genes[i].Clone() : parent2.Genes[i].Clone());
        }
        return child;
    }

    private static void Mutate(
        GradientChromosome chromosome)
    {
        // Mutate background
        if (Rng.NextDouble() < MutationRate)
        {
            chromosome.Background.Angle = chromosome.Background.Angle + (Range(-15.0f, 15.0f) % 360.0f);
        }

        if (Rng.NextDouble() < MutationRate)
        {
            chromosome.Background.Start.Color = new Rgb(
                Math.Clamp(chromosome.Background.Start.Color.R + Range(-0.08f, 0.08f), 0.0f, 1.0f),
                Math.Clamp(chromosome.Background.Start.Color.G + Range(-0.08f, 0.08f), 0.0f, 1.0f),
                Math.Clamp(chromosome.Background.Start.Color.B + Range(-0.08f, 0.08f), 0.0f, 1.0f));
        }

        if (Rng.NextDouble() < MutationRate)
        {
            chromosome.Background.End.Color = new Rgb(
                Math.Clamp(chromosome.Background.End.Color.R + Range(-0.08f, 0.08f), 0.0f, 1.0f),
                Math.Clamp(chromosome.Background.End.Color.G + Range(-0.08f, 0.08f), 0.0f, 1.0f),
                Math.Clamp(chromosome.Background.End.Color.B + Range(-0.08f, 0.08f), 0.0f, 1.0f));
        }

        foreach (var gene in chromosome.Genes)
        {
            // Mutate position
            if (Rng.NextDouble() < MutationRate)
            {
                gene.Position = new PointF(
                    Math.Clamp(gene.Position.X + Range(-0.1f, 0.1f), 0.0f, 1.0f),
                    Math.Clamp(gene.Position.Y + Range(-0.1f, 0.1f), 0.0f, 1.0f));
            }

            // Mutate color
            if (Rng.NextDouble() < MutationRate)
            {
                gene.Color = new Rgb(
                    Math.Clamp(gene.Color.R + Range(-0.08f, 0.08f), 0.0f, 1.0f),
                    Math.Clamp(gene.Color.G + Range(-0.08f, 0.08f), 0.0f, 1.0f),
                    Math.Clamp(gene.Color.B + Range(-0.08f, 0.08f), 0.0f, 1.0f));
            }

            // Mutate radius
            if (Rng.NextDouble() < MutationRate)
            {
                gene.Radius = Math.Clamp(gene.Radius + Range(-0.1f, 0.1f), 0.125f, 1.0f);
            }
        }
    }

    private static float CalculateFitness(
        GradientChromosome chromosome,
        Image<Rgba32> targetImage,
        float imageDiagonal)
    {
        var dimensions = targetImage.Size;

        float totalDifference = 0.0f;
        var render = RenderChromosome(chromosome, imageDiagonal, dimensions);

        for (int y = 0; y < dimensions.Height; y++)
        {
            for (int x = 0; x < dimensions.Width; x++)
            {
                var generatedColor = render[x, y];
                Rgb targetColor = targetImage[x, y];

                float diffR = generatedColor.R - targetColor.R;
                float diffG = generatedColor.G - targetColor.G;
                float diffB = generatedColor.B - targetColor.B;
                totalDifference += (diffR * diffR) + (diffG * diffG) + (diffB * diffB);
            }
        }
        return totalDifference;
    }

    private static Rgb[,] RenderChromosome(
        GradientChromosome chromosome,
        float imageDiagonal,
        Size dimensions)
    {
        var render = new Rgb[dimensions.Width, dimensions.Height];

        float angleRad = chromosome.Background.Angle * MathF.PI / 180.0f;
        float vx = MathF.Sin(angleRad);
        float vy = -MathF.Cos(angleRad);
        float d00 = (0 * vx) + (0 * vy);
        float d10 = (dimensions.Width * vx) + (0 * vy);
        float d01 = (0 * vx) + (dimensions.Height * vy);
        float d11 = (dimensions.Width * vx) + (dimensions.Height * vy);
        float min_d = Math.Min(d00, Math.Min(d10, Math.Min(d01, d11)));
        float max_d = Math.Max(d00, Math.Max(d10, Math.Max(d01, d11)));
        float range = max_d - min_d;

        for (int y = 0; y < dimensions.Height; y++)
        {
            for (int x = 0; x < dimensions.Width; x++)
            {
                float d = (x * vx) + (y * vy);
                float time = (d - min_d) / range;
                time = MathF.Max(0.0f, MathF.Min(1.0f, time));

                render[x, y] = Lerp(chromosome.Background.End.Color, chromosome.Background.Start.Color, time);
            }
        }

        foreach (var gene in chromosome.Genes)
        {
            var genePixelPosition = new Point(
                (int)(gene.Position.X * dimensions.Width),
                (int)(gene.Position.Y * dimensions.Height));

            float radiusPixels = gene.Radius * imageDiagonal * 0.5f;

            int minX = Math.Max(0, (int)(genePixelPosition.X - radiusPixels));
            int maxX = Math.Min(dimensions.Width - 1, (int)(genePixelPosition.X + radiusPixels));
            int minY = Math.Max(0, (int)(genePixelPosition.Y - radiusPixels));
            int maxY = Math.Min(dimensions.Height - 1, (int)(genePixelPosition.Y + radiusPixels));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    var difference = new Point(
                        x - genePixelPosition.X,
                        y - genePixelPosition.Y);
                    float distance = MathF.Sqrt((difference.X * difference.X) + (difference.Y * difference.Y));

                    if (distance < radiusPixels)
                    {
                        float alpha = 1.0f - (distance / radiusPixels);
                        ref var pixel = ref render[x, y];
                        pixel = Lerp(gene.Color, pixel, alpha);
                    }
                }
            }
        }
        return render;
    }

    public static List<Rgb> GetDominantColors(
        Image<Rgba32> image,
        int numberOfColors,
        int maxIterations = 100,
        double tolerance = 0.1)
    {
        int totalPixels = image.Width * image.Height;
        if (totalPixels == 0)
        {
            return [];
        }

        var allPixels = new Rgb[totalPixels];
        int pixelIndex = 0;

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    allPixels[pixelIndex++] = row[x];
                }
            }
        });

        var centroids = new Rgb[numberOfColors];
        var rand = new Random();
        for (int i = 0; i < numberOfColors; i++)
        {
            centroids[i] = allPixels[rand.Next(allPixels.Length)];
        }

        int[] pixelClusterAssignments = new int[totalPixels];
        float[] clusterRSum = new float[numberOfColors];
        float[] clusterGSum = new float[numberOfColors];
        float[] clusterBSum = new float[numberOfColors];
        int[] clusterPixelCount = new int[numberOfColors];

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            Array.Clear(clusterRSum, 0, numberOfColors);
            Array.Clear(clusterGSum, 0, numberOfColors);
            Array.Clear(clusterBSum, 0, numberOfColors);
            Array.Clear(clusterPixelCount, 0, numberOfColors);

            for (int i = 0; i < totalPixels; i++)
            {
                var currentPixel = allPixels[i];
                ref int closestCentroidIndex = ref pixelClusterAssignments[i];
                float minDistance = float.MaxValue;

                for (int j = 0; j < numberOfColors; j++)
                {
                    float distance = GetColorDistance(currentPixel, centroids[j]);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestCentroidIndex = j;
                    }
                }

                clusterRSum[closestCentroidIndex] += currentPixel.R;
                clusterGSum[closestCentroidIndex] += currentPixel.G;
                clusterBSum[closestCentroidIndex] += currentPixel.B;
                clusterPixelCount[closestCentroidIndex]++;
            }

            var newCentroids = new Rgb[numberOfColors];
            double totalCentroidMovement = 0;

            for (int i = 0; i < numberOfColors; i++)
            {
                int c = clusterPixelCount[i];

                if (c > 0)
                {
                    float r = clusterRSum[i] / c;
                    float g = clusterGSum[i] / c;
                    float b = clusterBSum[i] / c;
                    var newCentroid = new Rgb(r, g, b);

                    totalCentroidMovement += GetColorDistance(centroids[i], newCentroid);
                    newCentroids[i] = newCentroid;
                }
                else
                {
                    newCentroids[i] = allPixels[rand.Next(allPixels.Length)];
                }
            }

            Array.Copy(newCentroids, centroids, numberOfColors);

            if (totalCentroidMovement < tolerance)
            {
                break;
            }
        }

        var colorCounts = new Dictionary<Rgb, int>();
        for (int i = 0; i < totalPixels; i++)
        {
            var dominantColor = centroids[pixelClusterAssignments[i]];

            if (colorCounts.TryGetValue(dominantColor, out int currentCount))
            {
                colorCounts[dominantColor] = currentCount + 1;
            }
            else
            {
                colorCounts[dominantColor] = 1;
            }
        }

        var dominantColors = colorCounts
            .OrderByDescending(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();

        return dominantColors;
    }

    public static Rgb Lerp(
        Rgb start,
        Rgb end,
        float alpha)
    {
        return new Rgb(
            (start.R * alpha) + (end.R * (1.0f - alpha)),
            (start.G * alpha) + (end.G * (1.0f - alpha)),
            (start.B * alpha) + (end.B * (1.0f - alpha)));
    }

    public static float Range(
        float minimum,
        float maximum)
    {
        return ((float)Rng.NextDouble() * (maximum - minimum)) + minimum;
    }

    private static float GetColorDistance(
        Rgb color1,
        Rgb color2)
    {
        float dr = color1.R - color2.R;
        float dg = color1.G - color2.G;
        float db = color1.B - color2.B;
        return MathF.Sqrt((dr * dr) + (dg * dg) + (db * db));
    }

    private static string ConvertToCss(
        GradientChromosome chromosome)
    {
        var gradients = new List<string>();
        for (int i = chromosome.Genes.Count - 1; i >= 0; i--)
        {
            var gene = chromosome.Genes[i];
            float positionX = gene.Position.X * 100;
            float positionY = gene.Position.Y * 100;

            gradients.Add($"radial-gradient(at {positionX:0.#}% {positionY:0.#}%, {Rgba32ToHexString(gene.Color)} 0, {Rgba32ToTransparentHexString(gene.Color)} {gene.Radius * 100:0.#}%)");
        }

        string cssGradients = $"background-image: {string.Join(", ", gradients)}, linear-gradient({chromosome.Background.Angle:0}deg, {Rgba32ToHexString(chromosome.Background.Start.Color)}, {Rgba32ToHexString(chromosome.Background.End.Color)});";
        return $"{cssGradients}";
    }

    public static string Rgba32ToHexString(Rgba32 color)
    {
        if (color.A == 255)
        {
            return $"#{color.R:x2}{color.G:x2}{color.B:x2}";
        }

        return $"#{color.R:x2}{color.G:x2}{color.B:x2}{color.A:x2}";
    }

    public static string Rgba32ToTransparentHexString(Rgba32 color)
    {
        return $"#{color.R:x2}{color.G:x2}{color.B:x2}00";
    }
}
