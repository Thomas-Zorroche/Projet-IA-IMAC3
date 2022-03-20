using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneticAlgorithm
{
    Texture2D target;
    private Color[] targetColors;

    int nPixels;

    int populationSize;

    // -----------------------------------------------

    private float[] FitnessA;
    private float[] FitnessB;

    private Color[][] ColorsA;
    private Color[][] ColorsB;


    // -----------------------------------------------

    private int[] pool;
    private int poolCount;
    private int poolSize;
    private int poolFactor;

    float mutationChance;

    public Color[] palette;
    private int paletteCount;

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

        palette = colorPalette.ToArray();
        paletteCount = colorPalette.Count;
        populationSize = _populationSize;

        poolFactor = 50;
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
            if (!Contains(targetColors[i]))
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

    private bool Contains(Color color)
    {
        for (int i = 0; i < paletteCount; i++)
        {
            if (color.r == palette[i].r && color.g == palette[i].g && color.b == palette[i].b)
            {
                return true;
            }
        }
        return false;
    }

    private int FindNearestColor(int index)
    {
        for (int i = 0; i < paletteCount; i++)
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
        FitnessA = new float[populationSize];
        FitnessB = new float[populationSize];

        ColorsA = new Color[populationSize][];
        ColorsB = new Color[populationSize][];

        for (int i = 0; i < populationSize; i++)
        {
            ColorsA[i] = new Color[nPixels];
            SetRandomPixels(ColorsA[i]);
            FitnessA[i] = ComputeFitness(ColorsA[i]);
        }

        for (int i = 0; i < populationSize; i++)
        {
            ColorsB[i] = new Color[nPixels];
            SetRandomPixels(ColorsB[i]);
            FitnessB[i] = ComputeFitness(ColorsB[i]);
        }
    }

    private void BuildPool()
    {
        System.Array.Clear(pool, 0, poolCount);
        poolCount = 0;

        if (popA)
        {
            for (int imgIdx = 0; imgIdx < populationSize; imgIdx++)
            {
                int n = (int)(FitnessA[imgIdx] * poolFactor);
                for (int i = 0; i < n; i++)
                {
                    pool[poolCount] = imgIdx;
                    poolCount++;
                }
            }
        }
        else
        {
            for (int imgIdx = 0; imgIdx < populationSize; imgIdx++)
            {
                int n = (int)(FitnessB[imgIdx] * poolFactor);
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

        for (int i = 0; i + 2 < populationSize; i += 3)
        {
            // Select two random individuals, based on their fitness probabilites
            if (popA)
            {
                ColorsA[i] = ColorsB[pool[Random.Range(0, poolCount - 1)]];
                ColorsA[i + 1] = ColorsB[pool[Random.Range(0, poolCount - 1)]];
            }
            else
            {
                ColorsB[i] = ColorsA[pool[Random.Range(0, poolCount - 1)]];
                ColorsB[i + 1] = ColorsA[pool[Random.Range(0, poolCount - 1)]];
            }

            // Mutate
            bool mutate = Random.Range(0.0f, 1.0f) < (1 / mutationChance); // TODO inside condition
            if (mutate)
            {
                if (popA)
                {
                    SetRandomPixels(ColorsA[i + 2]);
                    FitnessA[i + 2] = ComputeFitness(ColorsA[i + 2]);
                }
                else
                {
                    SetRandomPixels(ColorsB[i + 2]);
                    FitnessB[i + 2] = ComputeFitness(ColorsB[i + 2]);
                }
            }
            else
            {
                // Crossover
                if (popA)
                {
                    ColorsA[i].CopyTo(ColorsA[i + 2], 0);
                    Crossover(ColorsA[i + 2], ColorsA[i + 1]);
                    FitnessA[i + 2] = ComputeFitness(ColorsA[i + 2]);
                }
                else
                {
                    ColorsB[i].CopyTo(ColorsB[i + 2], 0);
                    Crossover(ColorsB[i + 2], ColorsB[i + 1]);
                    FitnessB[i + 2] = ComputeFitness(ColorsB[i + 2]);
                }

            }
        }

    }

    public System.Tuple<Color[], float> GetBestImage()
    {
        var best = FitnessA[0];
        int bestIndex = 0;
        for (int i = 0; i < populationSize; i++)
        {
            if (FitnessA[i] > best)
            {
                bestIndex = i;
                best = FitnessA[i];
            }
        }
        return System.Tuple.Create(ColorsA[bestIndex], best);
    }

    public void ComputeFitnessGPU(Image image)
    {
        GAComputreShader.SetTexture(KERNEL_FITNESS_ID, CS_ID_TARGET_TEXTURE, target);
        GAComputreShader.SetTexture(KERNEL_FITNESS_ID, CS_ID_SRC_TEXTURE, sourceTextureCS);
        GAComputreShader.Dispatch(KERNEL_FITNESS_ID, nPixels / LOCAL_WORK_GROUPS, nPixels / LOCAL_WORK_GROUPS, 1);
    }


    // ----------------------------------------------------
    // ----------------------------------------------------
    // ----------------------------------------------------

    private float ComputeFitness(Color[] srcColors)
    {
        float fitness = 0.0f;
        for (int i = 0; i < nPixels; i++)
        {
            if (srcColors[i].r == targetColors[i].r && srcColors[i].g == targetColors[i].g && srcColors[i].b == targetColors[i].b)
            {
                fitness++;
            }
        }
        return fitness / nPixels;
    }
    private void Crossover(Color[] srcColors, Color[] parentColors)
    {
        int middleIndex = Random.Range(0, nPixels);
        for (int i = middleIndex; i < nPixels; i++)
        {
            srcColors[i] = parentColors[i];
        }
    }
    private void SetRandomPixels(Color[] srcColors)
    {
        for (int i = 0; i < nPixels; i++)
        {
            srcColors[i] = palette[Random.Range(0, paletteCount)];
        }
    }
}

