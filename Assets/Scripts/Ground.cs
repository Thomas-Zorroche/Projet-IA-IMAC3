using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ground : MonoBehaviour
{
	[SerializeField] private GameObject flowerPrefab;
	[Range(0, 20)]
	[SerializeField] private int flowerCount = 5;

	private List<GameObject> flowers = new List<GameObject>();

	[Range(0, 2)]
	[SerializeField] private float boundMargin = 1f;
	[SerializeField] private MeshCollider groundCollider;

	private void OnApplicationQuit()
	{
		RemoveAllFlowers();
	}

	private void Awake()
	{

	}

	// Start is called before the first frame update
	void Start()
	{
		GenerateFlowers();
	}

    public void GenerateFlowers()
    {
		RemoveAllFlowers();
		for (int i = 0; i < flowerCount; i++)
		{
			GameObject flower = Instantiate(flowerPrefab, transform);
			flower.transform.position = GetPositionInBounds();
			flowers.Add(flower);
		}
	}

	private void RemoveAllFlowers()
	{
		foreach (GameObject flower in flowers)
		{
			Destroy(flower);
		}
		flowers.Clear();
	}



	void Update()
    {
		if (Input.anyKeyDown)
		{
			GenerateFlowers();
		}
    }

	private Vector3 GetPositionInBounds()
	{
		return new Vector3(
			Random.Range(groundCollider.bounds.min.x + boundMargin, groundCollider.bounds.max.x - boundMargin),
			groundCollider.bounds.center.y,
			Random.Range(groundCollider.bounds.min.z + boundMargin, groundCollider.bounds.max.z - boundMargin)
		);
	}
}
