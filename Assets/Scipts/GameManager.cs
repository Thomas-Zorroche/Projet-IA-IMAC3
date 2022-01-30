using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    public Texture2D baseImage;

    public int populationSize;

    public float deltaEMax = 40f;

    public int maxNumberOfColor = 6;

    public int sizeRatio = 32; 

    private GeneticAlgorithm GA;

    private RenderTexture bestIndividual;
    private RenderTexture targetIndividual;

    private Texture2D bestTexture;

    public RawImage targetImage;
    public RawImage bestImage;

    public Text fitnessText;
    public Text iterationText;

    private int size;

    int epoch = 0;

    private List<Color> palette = new List<Color>();



    // Start is called before the first frame update
    void Start()
    {
        size = baseImage.width / sizeRatio;

        Debug.Log(size * size + " Pixels");

        targetIndividual = CreateRenderTexture(size);
        bestIndividual = CreateRenderTexture(size);

        Graphics.Blit(baseImage, targetIndividual);

        bestTexture = new Texture2D(size, size);

        Texture2D textureFiltered = FilterRenderTexture(targetIndividual);
        textureFiltered.Apply();

        Graphics.Blit(textureFiltered, targetIndividual);
        targetImage.texture = targetIndividual;

        GA = new GeneticAlgorithm(targetIndividual, populationSize, palette);
    }


    void Update()
    {
		// Get Best Image
		Image bestInd = GA.GetBestImage();
		bestTexture.SetPixels(bestInd.GetColors());
		bestTexture.Apply();
        Graphics.Blit(bestTexture, bestIndividual);
        bestImage.texture = bestIndividual;


        iterationText.text = epoch.ToString();
        fitnessText.text = "fitness : " + (bestInd.fitness * 100).ToString() + "%";

        // Genetic algorithm step
        GA.Update();


        epoch++;

	}

    public Texture2D FilterRenderTexture(RenderTexture rt)
	{
        Texture2D myTexture = toTexture2D(rt);
        Color[] pixels = myTexture.GetPixels();
        palette.Clear();
        List<Vector4> colorsLab = new List<Vector4>();

        foreach (Color pix in pixels)
        {
            if (palette.Count == 0)
			{
                palette.Add(pix);
                continue;
			}
            if (palette.Count >= maxNumberOfColor)
                break;

            int paletteSize = palette.Count;
            bool addToPalette = true;
			for (int i = 0; i < paletteSize; i++)
			{
                var pixLab = RGBToLab(pix);
                var paletteLab = RGBToLab(palette[i]);
                var delta = DeltaE(pixLab, paletteLab);

				if (delta < deltaEMax)
				{
                    addToPalette = false;
                    break;
				}
			}
            if (addToPalette) palette.Add(pix);
		}

		Debug.Log("Palette size : " + palette.Count);


		// Show Palette
		//for (int i = 0; i < palette.Count; i++)
		//{
		//          myTexture.SetPixel(0, i, palette[i]);
		//}
		//      myTexture.Apply();

		for (int i = 0; i < myTexture.width; i++)
		{
		    for (int j = 0; j < myTexture.height; j++)
			{
                myTexture.SetPixel(i, j, GetClosestColor(palette, myTexture.GetPixel(i, j)));
			}
		}
        myTexture.Apply();

        return myTexture;
    }

    public Color GetClosestColor(List<Color> palette, Color color)
	{
        Color closest = palette[0];
        Vector4 colorLab = RGBToLab(color);
        float minDelta = DeltaE(colorLab, RGBToLab(palette[0]));

        for (int i = 1; i < palette.Count; i++)
		{
            var paletteLab = RGBToLab(palette[i]);
            var delta = DeltaE(colorLab, paletteLab);

            if (minDelta > delta)
            {
                minDelta = delta;
                closest = palette[i];
            }
        }
        return closest;
	}

    public RenderTexture GetTargetRender()
    {
        return targetIndividual;
    }

    public Texture2D GetBestTexture()
    {
        return bestTexture;
    }

    public static RenderTexture CreateRenderTexture(int size, RenderTextureFormat format = RenderTextureFormat.ARGB32, bool useMips = false)
    {
        RenderTexture rt = new RenderTexture(size, size, 0, format, RenderTextureReadWrite.Linear);

        rt.useMipMap = useMips;
        rt.autoGenerateMips = false;
        rt.anisoLevel = 6;
        rt.filterMode = FilterMode.Point;
        rt.wrapMode = TextureWrapMode.Repeat;
        rt.enableRandomWrite = true;
        rt.Create();

        return rt;
    }
    static public Texture2D toTexture2D( RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGB24, false);
        // ReadPixels looks at the active RenderTexture.
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        return tex;
    }

    public static Vector4 RGBToLab(Color color)
    {
        float[] xyz = new float[3];
        float[] lab = new float[3];
        float[] rgb = new float[] { color.r, color.g, color.b};

        // Convert to C linear
		for (int i = 0; i < rgb.Length; i++)
		{
            if (rgb[i] > .04045f)
                rgb[i] = (float)Math.Pow((rgb[i] + .055) / 1.055, 2.4);
            else
                rgb[i] = rgb[i] / 12.92f;
		}
        
        rgb[0] = rgb[0] * 100.0f;
        rgb[1] = rgb[1] * 100.0f;
        rgb[2] = rgb[2] * 100.0f;

        // Convert into xyz
        xyz[0] = ((rgb[0] * .412453f) + (rgb[1] * .357580f) + (rgb[2] * .180423f));
        xyz[1] = ((rgb[0] * .212671f) + (rgb[1] * .715160f) + (rgb[2] * .072169f));
        xyz[2] = ((rgb[0] * .019334f) + (rgb[1] * .119193f) + (rgb[2] * .950227f));


        xyz[0] = xyz[0] / 95.047f;
        xyz[1] = xyz[1] / 100.0f;
        xyz[2] = xyz[2] / 108.883f;

        // Convert in Lab
		for (int i = 0; i < xyz.Length; i++)
		{
            if (xyz[i] > .008856f)
                xyz[i] = (float)Math.Pow(xyz[i], (1.0 / 3.0));
            else
                xyz[i] = (xyz[i] * 7.787f) + (16.0f / 116.0f);
		}


        lab[0] = (116.0f * xyz[1]) - 16.0f;
        lab[1] = 500.0f * (xyz[0] - xyz[1]);
        lab[2] = 200.0f * (xyz[1] - xyz[2]);

        return new Vector4(lab[0], lab[1], lab[2], color.a);
    }

    public static float DeltaE(Vector4 LabColor1, Vector4 LabColor2)
	{
        return Vector4.Distance(LabColor1, LabColor2);
	}
}
