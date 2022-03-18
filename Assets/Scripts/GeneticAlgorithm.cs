using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneticAlgorithm
{
    Texture2D target;
    private Color[] targetColors;

    int nPixels;

    int populationSize;

    List<Image> populationA;
    List<Image> populationB;

    private int[] pool;
    private int poolCount;
    private int poolSize;
    private int poolFactor;

    float mutationChance;

    public List<Color> palette;

    private bool popA = true;

    private ComputeShader GAComputreShader;


    // Compute Shader Variables
    RenderTexture sourceTextureCS;
    readonly int KERNEL_FITNESS_ID;
    readonly int LOCAL_WORK_GROUPS = 8; // 8 x 8 x 1
    readonly int CS_ID_TARGET_TEXTURE = Shader.PropertyToID("TargetTexture");
    readonly int CS_ID_SRC_TEXTURE = Shader.PropertyToID("SrcTexture");


    public GeneticAlgorithm(RenderTexture targetRT, int _populationSize, List<Color> colorPalette, ComputeShader shader, float mutation)
    {
        mutationChance = mutation;

        GAComputreShader = shader;
        //KERNEL_FITNESS_ID = GAComputreShader.FindKernel("ComputeFitness");

        palette = colorPalette;
        populationSize = _populationSize;

        poolFactor = 100;
        poolSize = populationSize * poolFactor;
        pool = new int[poolSize];

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
        populationA = new List<Image>();
        for (int i = 0; i < populationSize; i++)
        {
            Image image = new Image(nPixels);
            image.SetRandomPixels(palette);
            image.ComputeFitness(targetColors);
            populationA.Add(image);
        }

        populationB = new List<Image>();
        for (int i = 0; i < populationSize; i++)
        {
            Image image = new Image(nPixels);
            image.SetRandomPixels(palette);
            image.ComputeFitness(targetColors);
            populationB.Add(image);
        }
    }

    private void BuildPool()
    {
        System.Array.Clear(pool, 0, poolCount);
        poolCount = 0;

        if (popA)
        {
            for (int imgIdx = 0; imgIdx < populationA.Count; imgIdx++)
            {
                int n = (int)(populationA[imgIdx].fitness * poolFactor);
                for (int i = 0; i < n; i++)
                {
                    pool[poolCount] = imgIdx;
                    poolCount++;
                }
            }
        }
        else
        {
            for (int imgIdx = 0; imgIdx < populationB.Count; imgIdx++)
            {
                int n = (int)(populationB[imgIdx].fitness * poolFactor);
                for (int i = 0; i < n; i++)
                {
                    pool[poolCount] = imgIdx;
                    poolCount++;
                }
            }
        }
    }

    public void Update(int epoch)
    {
        BuildPool();

        popA = (epoch + 1) % 2 == 0;

        //var newPopulation = new List<Image>();
        for (int i = 0; i + 2 < populationSize; i += 3)
        {
            // Select two random individuals, based on their fitness probabilites
            if (popA)
            {
                populationA[i] = WeightedChoice();
                populationA[i + 1] = WeightedChoice();
            }
            else
            {
                populationB[i] = WeightedChoice();
                populationB[i + 1] = WeightedChoice();
            }

            // Mutate
            bool mutate = Random.Range(0.0f, 1.0f) < (1 / mutationChance); // TODO inside condition
            if (mutate)
            {
                if (popA)
                {
                    populationA[i + 2].SetRandomPixels(palette);
                    populationA[i + 2].ComputeFitness(targetColors);
                }
                else
                {
                    populationB[i + 2].SetRandomPixels(palette);
                    populationB[i + 2].ComputeFitness(targetColors);
                }
            }
            else
            {
                // Crossover
                if (popA)
                {
                    populationA[i + 2].CopyColors(populationA[i]);
                    populationA[i + 2].Crossover(populationA[i + 1]);
                    populationA[i + 2].ComputeFitness(targetColors);
                }
                else
                {
                    populationB[i + 2].CopyColors(populationB[i]);
                    populationB[i + 2].Crossover(populationB[i + 1]);
                    populationB[i + 2].ComputeFitness(targetColors);
                }

            }
        }

    }

    private Image WeightedChoice()
    {
        if (popA)
        {
            return populationB[pool[Random.Range(0, poolCount - 1)]];
        }
        else
        {
            return populationA[pool[Random.Range(0, poolCount - 1)]];
        }
    }

    public Image GetBestParent(Image p1, Image p2)
    {
        return p1.fitness > p2.fitness ? p1 : p2;
    }

    public Image GetBestImage()
    {
        var best = populationA[0];
        foreach (var image in populationA)
        {
            if (image.fitness > best.fitness)
                best = image;
        }

        return best;
    }

    public void ComputeFitnessGPU(Image image)
    {
        GAComputreShader.SetTexture(KERNEL_FITNESS_ID, CS_ID_TARGET_TEXTURE, target);
        GAComputreShader.SetTexture(KERNEL_FITNESS_ID, CS_ID_SRC_TEXTURE, sourceTextureCS);
        GAComputreShader.Dispatch(KERNEL_FITNESS_ID, nPixels / LOCAL_WORK_GROUPS, nPixels / LOCAL_WORK_GROUPS, 1);
    }
}


