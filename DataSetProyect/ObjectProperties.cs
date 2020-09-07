using System;
using UnityEngine;

/*
 This class contains the properties of each object and these are used to generate the dataset
     */
public class ObjectProperties : MonoBehaviour
{
    public int type;
    public CoordinatesSystem coordinatesSystem;

    private Func<Vector3, Vector3>[] coordinateSystemArray;

    private void Start()
    {
        coordinateSystemArray = new Func<Vector3, Vector3>[]
        {
            (Vector3 size) => new Vector3(size.x * transform.localScale.x, size.y * transform.localScale.y, size.z * transform.localScale.z),
            (Vector3 size) => new Vector3(size.x * transform.localScale.x, size.z * transform.localScale.z, size.y * transform.localScale.y),
            (Vector3 size) => new Vector3(size.y * transform.localScale.y, size.x * transform.localScale.x, size.z * transform.localScale.z),
            (Vector3 size) => new Vector3(size.y * transform.localScale.y, size.z * transform.localScale.z, size.x * transform.localScale.x)
        };
    }

    public enum CoordinatesSystem { xyz = 0, xzy = 1, yxz = 2, yzx = 3 }

    public Quaternion GetYew()
    {
        //return UnityEditor.TransformUtils.GetInspectorRotation(transform);
        return transform.rotation;
    }

    /*
     Collects the width(z), height(y) and length(x)
         */
    public Vector3 GetSize()
    {
        Vector3 sizeTemp = coordinateSystemArray[(int)coordinatesSystem](GetComponent<MeshFilter>().mesh.bounds.size);
        return new Vector3(sizeTemp.z, sizeTemp.y, sizeTemp.x);
    }
}



