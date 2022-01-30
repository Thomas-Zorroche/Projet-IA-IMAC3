using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneticAlgorithm
{
    Texture2D target;

    int nPixels;

    int populationSize;

    List<Image> population;

    private List<Image> pool;

    float crossoverChance = 1.0f;
    float mutationChance = 100.0f;
    
    public List<Color> palette;

    public GeneticAlgorithm(RenderTexture targetRT, int _populationSize, List<Color> colorPalette)
    {
        palette = colorPalette;
        populationSize = _populationSize;
        pool = new List<Image>();

        target = new Texture2D(targetRT.width, targetRT.height, TextureFormat.ARGB32, false);
        RenderTexture.active = targetRT;
        target.ReadPixels(new Rect(0, 0, targetRT.width, targetRT.height), 0, 0);
        target.Apply();

        nPixels = target.width * target.width;

        CreateRandomPopulation();
    }

    private void CreateRandomPopulation()
    {
        population = new List<Image>();
        for (int i = 0; i < populationSize; i++)
        {
            Image image = new Image(nPixels, target, palette);
            population.Add(image);
        }
    }

    private void BuildPool()
    {
        pool.Clear();
        foreach (var image in population)
        {
            int n = (int)(image.fitness * 1000);
            for (int i = 0; i < n; i++)
                pool.Add(image);
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

            // Crossover
            bool crossover = Random.Range(0.0f, 1.0f) < (1 / crossoverChance);

            var child = new Image(nPixels, target, palette);
            if (crossover) child.Crossover(ind1, ind2);
            else child.SetRandomPixels(palette);

            // Mutate
            bool mutate = Random.Range(0.0f, 1.0f) < (1 / mutationChance);
            if (mutate)
            {
				// Create a random image
				//Debug.Log("Mutate !");

				child = new Image(nPixels, target, palette);
            }

            newPopulation.Add(child);
            newPopulation.Add(ind1);
            newPopulation.Add(ind2);
        }

        // Update Population
        population = newPopulation;
    }

    private Image WeightedChoice()
    {
        return pool[Random.Range(0, pool.Count - 1)];
    }

    public Image Crossover(Image ind1, Image ind2)
    {
        int middleIndex = Random.Range(0, nPixels);
        Image childImage = new Image(nPixels, target, palette);
        for (int i = 0; i < nPixels; i++)
            childImage.SetPixelColor(i, i < middleIndex ? ind1.GetPixel(i) : ind1.GetPixel(i));

        childImage.ComputeFitness();
        return childImage;
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
