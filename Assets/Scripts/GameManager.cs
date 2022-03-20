using SimpleFileBrowser;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;

public class GameManager : MonoBehaviour
{
    [Header("Genetic Algorithm")]

    public Texture2D baseImage;

    public ComputeShader GAComputreShader;

    public int populationSize;

    public float deltaEMax = 40f;

    public int maxNumberOfColor = 6;

    public int size = 16;

    public float mutation;

    public int ClusteringSeed = 0;

    int epoch = 0;

    [Tooltip("Duration betweeen two epoch")]
    public float IterationTime = 0.5f;

    private bool GeneticAlgorithmIsRunning = false;

    private GeneticAlgorithm GA;
    private List<Color> palette = new List<Color>();

    private RenderTexture bestIndividual;
    private RenderTexture targetIndividual;
    private Texture2D bestTexture;


    [Header("Main Loop UI references")]
    public Canvas MainLoopCanvas;

    public RawImage targetImage;
    public RawImage bestImage;

    public InputField SearchField;

    public Text GameTimer;
    public Text fitnessText;
    public Text iterationText;
    public Text ErrorText;

    public GameObject ButtonsLayout;

    public GameObject CharacterNameButtonPrefab;

    [Header("Endgame UI references")]
    public Canvas EndgameCanvas;

    public Text SucessesText;

    public Text EndgameText;

    [Header("Other Canvas")]
    //public Canvas GACanvas;
    public Canvas MainMenuCanvas;

    float chrono;

    public int Sucesses = 0;
    public int Errors = 0;

    [Header("Game Variable")]
    public float RoundDuration = 30f;
    public int MaxErrorBeforeGameOver = 3;

    private List<Character> CharactersList = new List<Character>();
    private Character CharacterToFind;

    private float RoundStartTime;
    private bool GameOn = false;

    // -----------------------------------------------
    // -------- GENETIC ALGORITHM
    // -----------------------------------------------
    private bool popA = true;

    // -----------------------------------------------


    // -----------------------------------------------
    // -------- MULTI THREADING
    // -----------------------------------------------
    JobHandle GAJobHandle;
    GeneticAlgorithmJob GAJob;
    // -----------------------------------------------

    private void Start()
    {
        SearchField.onValueChanged.AddListener(delegate { OnSearchTextChanged(); });
        SearchField.onEndEdit.AddListener(delegate { SubmitSearchInput(); });

        //Initialize Characters List
        Object[] textures = Resources.LoadAll("BaseImages", typeof(Texture2D));
        foreach (Object texture in textures)
        {
            Character character = new Character(texture.name, (Texture2D)texture);
            CharactersList.Add(character);
        }
        CharacterToFind = GetRandomCharacter();

    }

    private Character GetRandomCharacter()
    {
        return CharactersList[Random.Range(0, CharactersList.Count)];
    }


    void InitGeneticAlgorithm()
    {
        //size = baseImage.width / 32;

        //Debug.Log(size * size + " Pixels");

        targetIndividual = CreateRenderTexture(size);
        bestIndividual = CreateRenderTexture(size);

        Graphics.Blit(baseImage, targetIndividual);

        bestTexture = new Texture2D(size, size);

        Texture2D textureFiltered = FilterRenderTexture(targetIndividual);

        Graphics.Blit(textureFiltered, targetIndividual);
        targetImage.texture = targetIndividual;

        GA = new GeneticAlgorithm(targetIndividual, populationSize, palette, GAComputreShader, mutation);

        chrono = 0.0f;
    }

    private struct GeneticAlgorithmJob : IJobParallelFor
    {
        public float _fitness;
        private Color _targetColor;
        private Color _currentColor;

        public NativeArray<Color> _ColorsA;
        public NativeArray<Color> _ColorsB;

        public NativeArray<float> _FitnessA;
        public NativeArray<float> _FitnessB;

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

        public void Execute(int i)
        {
            // Select two random individuals, based on their fitness probabilites
            if (popA)
            {
                _ColorsA[i] = _ColorsB[pool[Random.Range(0, poolCount - 1)]];
                _ColorsA[i + 1] = _ColorsB[pool[Random.Range(0, poolCount - 1)]];
            }
            else
            {
                _ColorsB[i] = _ColorsA[pool[Random.Range(0, poolCount - 1)]];
                _ColorsB[i + 1] = _ColorsA[pool[Random.Range(0, poolCount - 1)]];
            }

            // Mutate
            bool mutate = Random.Range(0.0f, 1.0f) < (1 / mutationChance); // TODO inside condition
            if (mutate)
            {
                if (popA)
                {
                    SetRandomPixels(_ColorsA[i + 2]);
                    _FitnessA[i + 2] = ComputeFitness(_ColorsA[i + 2]);
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

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();

        if (GeneticAlgorithmIsRunning)
        {
            // Build Pool
            BuildPool();
            popA = (epoch + 1) % 2 == 0;

            // Genetic algorithm step
            // 1
            GAJob = new GeneticAlgorithmJob()
            {
/*                vertices = waterVertices,
                normals = waterNormals,
                offsetSpeed = waveOffsetSpeed,
                time = Time.time,
                scale = waveScale,
                height = waveHeight*/
            };

            // 2
            GAJobHandle = GAJob.Schedule(populationSize, 64);


            epoch++;
        }

        if (GameOn)
        {
            int timeLeft = Mathf.FloorToInt(RoundDuration - (Time.realtimeSinceStartup - RoundStartTime));
            GameTimer.text = timeLeft.ToString();

            if (timeLeft == 0)
                InitEndgameCanvas();

        }
    }

    private void LateUpdate()
    {
        // 1
        GAJobHandle.Complete();
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

    private void InitEndgameCanvas(bool IsGameOver = false)
    {
        GeneticAlgorithmIsRunning = false;

        GameOn = false;

        EndgameCanvas.gameObject.SetActive(true);
        MainLoopCanvas.gameObject.SetActive(false);

        EndgameText.text = IsGameOver || Sucesses == 0 ? "DOMMAGE..." : "FÉLICITATION !";

        string playAgainText = "Retente ta chance !";
        string text = "";

        if (Sucesses > 0)
            text += "Tu as trouvé " + Sucesses.ToString() + " image" + (Sucesses > 1 ? "s" : "") + (!IsGameOver ? " en " + RoundDuration + "s." : ".");
        else
            text += "Tu n'as trouvé aucune image... " + (!IsGameOver? playAgainText : "");

        if (IsGameOver)
            text += "/n Tu as fait 3 erreurs. " + playAgainText;

        SucessesText.text = text;

        SearchField.text = "";

    }

    IEnumerator UpdateTimer()
    {
        while (true)
        {
            yield return new WaitForSeconds(IterationTime);

            // Debug.Log("UPDATE TIMER");

            // Get Best Image
            var bestInd = GA.GetBestImage();
            bestTexture.SetPixels(bestInd.Item1);
            bestTexture.Apply();
            Graphics.Blit(bestTexture, bestIndividual);
            bestImage.texture = bestIndividual;

            iterationText.text = epoch.ToString();
            fitnessText.text = (bestInd.Item2).ToString("p");
        }
    }

    public void OnClickOnDraw()
    {
        Debug.Log("click on draw");
    }

    public void OnClickOnUpload()
    {
        Debug.Log("click on load");

        FileBrowser.SetFilters(true, new FileBrowser.Filter("Images", ".png", ".jpg"));

        FileBrowser.SetDefaultFilter(".jpg");

        FileBrowser.AddQuickLink("Users", "C:\\Users", null);

        // Show a select folder dialog 
        // onSuccess event: print the selected folder's path
        // onCancel event: print "Canceled"
        // Load file/folder: folder, Allow multiple selection: false
        // Initial path: default (Documents), Initial filename: empty
        // Title: "Select Folder", Submit button text: "Select"
        FileBrowser.ShowLoadDialog((paths) => OnUploadSucess(paths),
                                  () => { Debug.Log("Canceled"); },
                                  FileBrowser.PickMode.Files);

    }

    public void OnClickOnBeginGame()
    {
        GameOn = true;
        ResetGeneticAlgorithm();

        Sucesses = 0;
        Errors = 0;

        MainLoopCanvas.gameObject.SetActive(true);
        MainMenuCanvas.gameObject.SetActive(false);
        EndgameCanvas.gameObject.SetActive(false);

        GeneticAlgorithmIsRunning = true;

        RoundStartTime = Time.realtimeSinceStartup;
        chrono = Time.realtimeSinceStartup;
        StartCoroutine(UpdateTimer());
    }

    private void ResetGeneticAlgorithm()
    {
        epoch = 0;

        CharacterToFind = GetRandomCharacter();
        baseImage = CharacterToFind.Image;
        Debug.Log(CharacterToFind.CharacterName);


        InitGeneticAlgorithm();
    }

    public void TogglePlay()
    {
        GeneticAlgorithmIsRunning = !GeneticAlgorithmIsRunning;

        //Debug.Log(Time.realtimeSinceStartup - chrono);
    }

    public static Texture2D LoadPNG(string filePath, int size)
    {

        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(filePath))
        {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(size, size);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }
        return tex;
    }

    public void OnUploadSucess(string[] paths)
    {
        Debug.Log("Selected: " + paths[0]);

        baseImage = LoadPNG(paths[0], size);

        InitGeneticAlgorithm();

        //GACanvas.gameObject.SetActive(true);
        MainMenuCanvas.gameObject.SetActive(false);
        GeneticAlgorithmIsRunning = true;

        Debug.Log("UPLOAD");
        chrono = Time.realtimeSinceStartup;
        StartCoroutine(UpdateTimer());
    }

    public void OnSearchTextChanged()
    {
        foreach (Transform item in ButtonsLayout.transform)
        {
            Destroy(item.gameObject);
        }
        if (SearchField.text != "")
        {
            List<Character> filtered = CharactersList.FindAll(e => e.CharacterName.ToLower().Contains(SearchField.text.ToLower()));

            // Sort by search position in character name
            filtered.Sort((a, b) =>
                a.CharacterName.ToLower().IndexOf(SearchField.text.ToLower())
                    .CompareTo(
                        b.CharacterName.ToLower().IndexOf(SearchField.text.ToLower())
                    ));

            // create buttons
            filtered.ForEach(e => {
                GameObject newButton = Instantiate(CharacterNameButtonPrefab, ButtonsLayout.transform);
                newButton.GetComponentInChildren<Text>().text = e.CharacterName;
                newButton.GetComponent<Button>().onClick.AddListener(delegate { OnClickOnSolution(e); });
            });
        }


    }

    private void SubmitSearchInput()
    {
        if (!Input.GetKeyDown(KeyCode.Return)) return;

        if (ButtonsLayout.transform.childCount > 0)
        {
            Button FirstButton = ButtonsLayout.transform.GetChild(0).GetComponent<Button>();
            FirstButton.onClick.Invoke();
        }
    }

    public void OnClickOnSolution(Character character)
    {
        //Debug.Log("Click on " + character.CharacterName);
        if (CharacterToFind == character)
        {
            Sucesses++;
            StartCoroutine(ShowTarget());

            SearchField.text = "";
            if (!SearchField.isFocused)
            {
                SearchField.Select();
                SearchField.ActivateInputField();
            }
        }
        else
		{
            Errors++;
            StartCoroutine(ShowErrorMessage());
		}

        if (Errors >= 3)
            InitEndgameCanvas(true);

    }

    private IEnumerator ShowTarget()
    {
        targetImage.gameObject.SetActive(true);

        yield return new WaitForSeconds(1);

        targetImage.gameObject.SetActive(false);
        ResetGeneticAlgorithm();

    }
    private IEnumerator ShowErrorMessage()
    {
        ErrorText.gameObject.SetActive(true);

        yield return new WaitForSeconds(1);

        ErrorText.gameObject.SetActive(false);
    }



    public Texture2D FilterRenderTexture(RenderTexture rt)
    {
        Texture2D myTexture = toTexture2D(rt);
        Color[] pixels = myTexture.GetPixels();
        palette.Clear();
        List<Vector3> colorsLab = new List<Vector3>();

        foreach (Color pix in pixels)
        {
            colorsLab.Add(RGBToLab(pix));
        }

        int[] clusters = Cluster(colorsLab);
        List<Color>[] colorClustered = new List<Color>[maxNumberOfColor];
        for (int i = 0; i < colorClustered.Length; i++)
            colorClustered[i] = new List<Color>();

        for (int i = 0; i < pixels.Length; i++)
        {
            int cluster = clusters[i];
            colorClustered[cluster].Add(pixels[i]);

        }

        palette.Capacity = colorClustered.Length;
        for (int i = 0; i < colorClustered.Length; i++)
        {
            Color meanColor = Average(colorClustered[i]);
            if (!float.IsNaN(meanColor.r))
            {
                palette.Add(meanColor);
            }
            else
            {
                Debug.LogWarning("ONE COLOR FROM THE PALETTE WAS PROBLEMATIC");
            }
        }


        for (int i = 0; i < pixels.Length; i++)
        {
            int cluster = clusters[i];
            Color color = palette[cluster];
            color.a = 1f;
            int column = i % myTexture.width;
            int row = i / myTexture.width;
            //myTexture.SetPixel(column, row, GetClosestColor(palette, myTexture.GetPixel(column, row)));

            myTexture.SetPixel(column, row, color);

            if (palette[cluster].r != myTexture.GetPixel(column, row).r)
            {
                palette[cluster] = myTexture.GetPixel(column, row);
            }
        }
        myTexture.Apply();

        return myTexture;
    }

    Color Average(List<Color> colors)
    {

        Vector4 sum = new Vector4();

        foreach (var color in colors)
        {
            sum += new Vector4(color.r * color.r, color.g * color.g, color.b * color.b, color.a * color.a);
        }

        sum /= colors.Count;

        return new Color(Mathf.Sqrt(sum.x), Mathf.Sqrt(sum.y), Mathf.Sqrt(sum.z), Mathf.Sqrt(sum.w));
    }

    int[] Cluster(List<Vector3> colors)
    {
        //List<Vector3> data = Normalized(colors);
        List<Vector3> data = colors;
        bool changed = true; bool success = true;
        int numClusters = maxNumberOfColor;

        int[] clustering = InitClustering(data, numClusters);
        Vector3[] means = new Vector3[numClusters];
        int maxCount = data.Count * 10;
        int ct = 0;
        while (changed == true && success == true && ct < maxCount)
        {
            ++ct;
            success = UpdateMeans(data, clustering, ref means);
            changed = UpdateClustering(data, ref clustering, means);

            // Check if a cluster point has no points
            int[] clusterCounts = new int[numClusters];
            for (int i = 0; i < data.Count; ++i)
            {
                int cluster = clustering[i];
                ++clusterCounts[cluster];
            }

            for (int k = 0; k < numClusters; ++k)
            {
                if (clusterCounts[k] == 0)
                {
                    int ct2 = 0;
                    do
                    {
                        ++ct2;
                        means[k] = new Vector3(Random.Range(0f, 100f), Random.Range(-128, 128), Random.Range(-128, 128));
                        changed = UpdateClustering(data, ref clustering, means);

                    } while (changed == false && ct2 < maxCount);
                }
            }

        }

        return clustering;
    }

    //   List<Vector3> Normalized(List<Vector3> data)
    //{
    //       List<Vector3> returnValue = new List<Vector3>();
    //	foreach (Vector3 vector in data)
    //	{
    //           returnValue.Add(vector.normalized);
    //	}
    //       return returnValue;
    //}

    private bool UpdateMeans(List<Vector3> data, int[] clustering, ref Vector3[] means)
    {
        // Check if a cluster point has no points
        int numClusters = means.Length;
        int[] clusterCounts = new int[numClusters];
        for (int i = 0; i < data.Count; ++i)
        {
            int cluster = clustering[i];
            ++clusterCounts[cluster];
        }

        //for (int k = 0; k < numClusters; ++k)
        //    if (clusterCounts[k] == 0)
        //        return false;

        // Calculate means
        for (int i = 0; i < means.Length; ++i)
            means[i] = new Vector3();

        for (int i = 0; i < data.Count; ++i)
        {
            int cluster = clustering[i];
            means[cluster] += data[i];
        }

        for (int i = 0; i < means.Length; ++i)
            means[i] /= clusterCounts[i];

        return true;
    }


    private bool UpdateClustering(List<Vector3> data, ref int[] clustering, Vector3[] means)
    {
        int numClusters = means.Length;
        bool changed = false;

        int[] newClustering = clustering;

        float[] distances = new float[numClusters];

        for (int i = 0; i < data.Count; ++i)
        {
            int newClusterID = 0;
            for (int k = 0; k < numClusters; ++k)
            {
                distances[k] = Vector3.Distance(data[i], means[k]);
                if (distances[k] <= distances[newClusterID])
                    newClusterID = k;
            }

            if (newClusterID != newClustering[i])
            {
                changed = true;
                newClustering[i] = newClusterID;
            }
        }

        if (changed == false)
            return false;

        //      int[] clusterCounts = new int[numClusters];
        //      for (int i = 0; i < data.Count; ++i)
        //      {
        //          int cluster = newClustering[i];
        //          ++clusterCounts[cluster];
        //      }

        //      for (int k = 0; k < numClusters; ++k)
        //          if (clusterCounts[k] == 0)
        //              return false;

        clustering = newClustering;
        return true; // no zero-counts and at least one change
    }

    private int[] InitClustering(List<Vector3> data, int numClusters)
    {
        Random.InitState(ClusteringSeed);
        int[] clustering = new int[data.Count];

        for (int i = 0; i < numClusters; ++i)
            clustering[i] = i;

        for (int i = numClusters; i < clustering.Length; ++i)
            clustering[i] = Random.Range(0, numClusters);
        return clustering;
    }

    //   public Color GetClosestColor(List<Color> palette, Color color)
    //{
    //       Color closest = palette[0];
    //       Vector4 colorLab = RGBToLab(color);
    //       float minDelta = DeltaE(colorLab, RGBToLab(palette[0]));

    //       for (int i = 1; i < palette.Count; i++)
    //	{
    //           var paletteLab = RGBToLab(palette[i]);
    //           var delta = DeltaE(colorLab, paletteLab);

    //           if (minDelta > delta)
    //           {
    //               minDelta = delta;
    //               closest = palette[i];
    //           }
    //       }
    //       return closest;
    //}

    //public RenderTexture GetTargetRender()
    //   {
    //       return targetIndividual;
    //   }

    //   public Texture2D GetBestTexture()
    //   {
    //       return bestTexture;
    //   }

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

    static public Texture2D toTexture2D(RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGB24, false);
        // ReadPixels looks at the active RenderTexture.
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        return tex;
    }

    public static Vector3 RGBToLab(Color color)
    {
        float[] xyz = new float[3];
        float[] lab = new float[3];
        float[] rgb = new float[] { color.r, color.g, color.b };

        // Convert to C linear
        for (int i = 0; i < rgb.Length; i++)
        {
            if (rgb[i] > .04045f)
                rgb[i] = (float)System.Math.Pow((rgb[i] + .055) / 1.055, 2.4);
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
                xyz[i] = (float)System.Math.Pow(xyz[i], (1.0 / 3.0));
            else
                xyz[i] = (xyz[i] * 7.787f) + (16.0f / 116.0f);
        }


        lab[0] = (116.0f * xyz[1]) - 16.0f;
        lab[1] = 500.0f * (xyz[0] - xyz[1]);
        lab[2] = 200.0f * (xyz[1] - xyz[2]);


        return new Vector3(lab[0], lab[1], lab[2]);
    }

    //   public static float DeltaE(Vector4 LabColor1, Vector4 LabColor2)
    //{
    //       return Vector4.Distance(LabColor1, LabColor2);
    //}

    private void OnDestroy()
    {
        SearchField.onValueChanged.RemoveAllListeners();
    }
}
