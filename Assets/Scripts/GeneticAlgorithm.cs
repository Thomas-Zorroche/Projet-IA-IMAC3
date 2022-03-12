using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneticAlgorithm
{
    Texture2D target;
    private Color[] targetColors;

    int nPixels;

    int populationSize;

    List<Image> population;

    private Image[] pool;
    private int poolCount;
    private int poolSize;

    float mutationChance = 100.0f;
    
    public List<Color> palette;

    public GeneticAlgorithm(RenderTexture targetRT, int _populationSize, List<Color> colorPalette)
    {
        palette = colorPalette;
        populationSize = _populationSize;

        poolSize = populationSize * 100;
        pool = new Image[poolSize];

        target = new Texture2D(targetRT.width, targetRT.height, TextureFormat.ARGB32, false);
        RenderTexture.active = targetRT;
        target.ReadPixels(new Rect(0, 0, targetRT.width, targetRT.height), 0, 0);
        target.Apply();

        nPixels = target.width * target.width;
        targetColors = target.GetPixels();

        // Correct colors
        for (int i = 0; i < targetColors.Length; i++)
        {
            if (!palette.Contains(targetColors[i]))
            {
                int index = FindNearestColor(i);
                if (index != -1)
                {
                    palette[index] = targetColors[i];
                }
            }
        }


        CreateRandomPopulation();
    }

    private int FindNearestColor(int index)
    {
        for (int i = 0; i < palette.Count; i++)
        {
            if (Mathf.Abs(palette[i].r - targetColors[index].r) < 0.0001f)
            {
                return i;
            }
        }
        return -1;
    }

    private void CreateRandomPopulation()
    {
        population = new List<Image>();
        for (int i = 0; i < populationSize; i++)
        {
            Image image = new Image(nPixels, target, palette, targetColors);
            population.Add(image);
        }
    }

    private void BuildPool()
    {
        System.Array.Clear(pool, 0, poolCount);
        poolCount = 0;

        foreach (var image in population)
        {
            int n = (int)(image.fitness * 100);
            for (int i = 0; i < n; i++)
            {
                pool[poolCount] = image;
                poolCount++;
            }
        }
    }

    public void Update()
    {
        //var weightedPopulation = GetWeightedPopulation();
        BuildPool();

        var newPopulation = new List<Image>();
        for (int i = 0; i < populationSize / 3; i++)
        {
            // Select two random individuals, based on their fitness probabilites
            var ind1 = WeightedChoice();
            var ind2 = WeightedChoice();

            // Mutate
            bool mutate = Random.Range(0.0f, 1.0f) < (1 / mutationChance);
            if (mutate)
            {
				var child = new Image(nPixels, target, palette, targetColors);
                newPopulation.Add(child);
            }
            else
            {
                // Crossover
                var child = new Image(ind1);
                child.Crossover(ind2);
                child.ComputeFitness(targetColors);
                newPopulation.Add(child);
            }

            newPopulation.Add(ind1);
            newPopulation.Add(ind2);
        }

        // Update Population
        population = newPopulation;
    }

    private Image WeightedChoice()
    {
        return pool[Random.Range(0, poolCount - 1)];
    }

    public Image GetBestParent(Image p1, Image p2)
    {
        return p1.fitness > p2.fitness ? p1 : p2;
    }

    public Image GetBestImage()
    {
        var best = population[0];
		foreach (var image in population)
		{
			if (image.fitness > best.fitness)
				best = image;
		}

		return best;
    }
}
