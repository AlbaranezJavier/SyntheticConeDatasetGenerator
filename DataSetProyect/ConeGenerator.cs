using System.Collections.Generic;
using UnityEngine;

public class ConeGenerator : MonoBehaviour
{
    /*
     Generates the object specified as an argument at random positions within limits. Press "g" to generate.
         */
    public Camera cam;
    public GameObject[] objects;
    public int number;
    public GameObject limitReference;
    public Vector2 maxRangeXZ;
    public Vector2 minRangeXZ;
    public float heightY;

    [HideInInspector]
    public List<GameObject> objectList = new List<GameObject>();

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyUp(KeyCode.G))
        {
            CleanScene();
            GenerateObjects();
        }
    }

    /* Create objects randomly within limits */
    private void GenerateObjects()
    {
        for (int i=0; i < number; i++)
        {
            int randObject = Random.Range(0, objects.Length);
            Vector3 randLocalPosition = new Vector3(Random.Range(minRangeXZ[0], maxRangeXZ[0]), heightY, Random.Range(minRangeXZ[1], maxRangeXZ[1]));
            objectList.Add(Instantiate(objects[randObject], limitReference.transform.TransformPoint(randLocalPosition), Quaternion.Euler(0f, 0f, 0f)));
        }
    }

    /* Destroy the objects and renew the list  */
    private void CleanScene()
    {
        foreach(GameObject go in objectList)
        {
            Destroy(go);
        }
        objectList = new List<GameObject>();
    }
}
