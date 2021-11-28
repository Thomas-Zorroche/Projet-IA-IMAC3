using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Ground ground;
    [Range(1, 10)]
    [SerializeField] private float generationPerSecond = 1f;

    private float elapsed = 0f;

    [SerializeField] private bool isPaused = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
		if (!isPaused)
		{
            float waitingTime = 1f / generationPerSecond;
            elapsed += Time.deltaTime;
            while (elapsed >= waitingTime)
            {
                elapsed = 0f;
                ground.GenerateFlowers();
            }
		}
    }
}
