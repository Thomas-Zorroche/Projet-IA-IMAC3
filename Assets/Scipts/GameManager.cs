using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Texture2D baseImage;

    public int populationSize;

    private GeneticAlgorithm GA;
    
    private RenderTexture gridImage;

    private Texture2D bestTexture;

    private int size;

    private List<Color> dominantColors;

    // Start is called before the first frame update
    void Start()
    {
        size = baseImage.width / 32;

        Debug.Log(size * size + " Pixels");

        gridImage = CreateRenderTexture(size);
        Graphics.Blit(baseImage, gridImage);

        // Retrieve dominant colors
        //dominantColors
        //gridImage

        bestTexture = new Texture2D(size, size);
        bestTexture.filterMode = FilterMode.Point;

        GA = new GeneticAlgorithm(gridImage, populationSize);
    }


    // Update is called once per frame
    void Update()
    {
        // Genetic algorithm step
        //GA.Update();
        //
        //// Get Best Image
        //Image bestImage = GA.GetBestImage();
        ////Debug.Log("Fitness: " + bestImage.fitness);
        //bestTexture.SetPixels(bestImage.GetColors());
        //bestTexture.Apply();
    }

    public RenderTexture GetGridImage()
    {
        return gridImage;
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
}
