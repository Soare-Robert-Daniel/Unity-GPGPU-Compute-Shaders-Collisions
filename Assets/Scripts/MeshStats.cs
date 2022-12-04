using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MeshStats : MonoBehaviour
{
    // Start is called before the first frame update
    private void Start()
    {
        var mesh = GetComponent<MeshFilter>().mesh;
        var vertices = mesh.vertices;
        var triangles = mesh.triangles;
        var normals = mesh.normals;
        
        Debug.Log(mesh.vertexCount);
        Debug.Log(triangles.Length / 3);
        Debug.Log(string.Join(" | ",vertices.Select(x => x.ToString())));
        Debug.Log(string.Join(" | ",triangles.Select(x => x.ToString())));
        Debug.Log(string.Join(" | ",normals.Select(x => x.ToString())));
    }
    
}
