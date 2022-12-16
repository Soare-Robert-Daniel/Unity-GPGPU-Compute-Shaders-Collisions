/*
http://entitycrisis.blogspot.com/2010/02/spatial-hash-class-in-c.html

This is a rather useful class, the Spatial Hash. It is used for creating an index of spatial data (3D things in space) and allowing fast queries to be run against the index.

Effectively, you can use this class to ask, "I'm at this position, what other objects are near me?".
*/

using UnityEngine;  //This needs to be 'removed' to make 'universal', i.e. not tied to Unity3D
using System.Collections;

public class SpatialHash 
{
    private Hashtable idx;
    private int cellSize;
    
    public SpatialHash(int cellSize) {
        this.cellSize = cellSize;
        this.idx = new Hashtable();
    }

    public int Count {
        get { return idx.Count; }
    }

    public ICollection Cells {
        get { return idx.Keys; }
    }

    public void Insert(Vector3 v, object obj) {
        ArrayList cell;
        foreach(string key in Keys(v)) {
            if(idx.Contains(key))
                cell = (ArrayList)idx[key];
            else  {
                cell = new ArrayList();
                idx.Add(key, cell);
            }
            if(!cell.Contains(obj))
                cell.Add(obj);
        }
    }

    public ArrayList Query(Vector3 v) {
        string key = Key(v);
        if(idx.Contains(key))
            return (ArrayList)idx[key];
        return new ArrayList();
    }

    private ArrayList Keys(Vector3 v)  {
        int o = cellSize / 2;
        ArrayList keys = new ArrayList();
        keys.Add(Key(new Vector3(v.x-o, v.y-0, v.z-o)));
        keys.Add(Key(new Vector3(v.x-o, v.y-0, v.z-0)));
        keys.Add(Key(new Vector3(v.x-o, v.y-0, v.z+o)));
        keys.Add(Key(new Vector3(v.x-0, v.y-0, v.z-o)));
        keys.Add(Key(new Vector3(v.x-0, v.y-0, v.z-0)));
        keys.Add(Key(new Vector3(v.x-0, v.y-0, v.z+o)));
        keys.Add(Key(new Vector3(v.x+o, v.y-0, v.z-o)));
        keys.Add(Key(new Vector3(v.x+o, v.y-0, v.z-o)));
        keys.Add(Key(new Vector3(v.x+o, v.y-0, v.z-0)));
        keys.Add(Key(new Vector3(v.x-o, v.y-o, v.z-o)));
        keys.Add(Key(new Vector3(v.x-o, v.y-o, v.z-0)));
        keys.Add(Key(new Vector3(v.x-o, v.y-o, v.z+o)));
        keys.Add(Key(new Vector3(v.x-0, v.y-o, v.z-o)));
        keys.Add(Key(new Vector3(v.x-0, v.y-o, v.z-0)));
        keys.Add(Key(new Vector3(v.x-0, v.y-o, v.z+o)));
        keys.Add(Key(new Vector3(v.x+o, v.y-o, v.z-o)));
        keys.Add(Key(new Vector3(v.x+o, v.y-o, v.z-o)));
        keys.Add(Key(new Vector3(v.x+o, v.y-o, v.z-0)));
        keys.Add(Key(new Vector3(v.x-o, v.y+o, v.z-o)));
        keys.Add(Key(new Vector3(v.x-o, v.y+o, v.z-0)));
        keys.Add(Key(new Vector3(v.x-o, v.y+o, v.z+o)));
        keys.Add(Key(new Vector3(v.x-0, v.y+o, v.z-o)));
        keys.Add(Key(new Vector3(v.x-0, v.y+o, v.z-0)));
        keys.Add(Key(new Vector3(v.x-0, v.y+o, v.z+o)));
        keys.Add(Key(new Vector3(v.x+o, v.y+o, v.z-o)));
        keys.Add(Key(new Vector3(v.x+o, v.y+o, v.z-o)));
        keys.Add(Key(new Vector3(v.x+o, v.y+o, v.z-0)));
        return keys;
    }

    private string Key(Vector3 v) {
        int x = (int)Mathf.Floor(v.x/cellSize)*cellSize;
        int y = (int)Mathf.Floor(v.y/cellSize)*cellSize;
        int z = (int)Mathf.Floor(v.z/cellSize)*cellSize;
        return x.ToString() + ":" + y.ToString() + ":" + z.ToString();
    }
 
}