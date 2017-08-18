using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unitilities.Tuples;


public class SpatialHash {


    private Dictionary<Tuple3I, List<Boid>> dict;
    private List<Boid> returnList;
    private List<Tuple3I> NeighborKeys;

    public SpatialHash()
    {
        returnList = new List<Boid>();
        dict = new Dictionary<Tuple3I, List<Boid>>();

        
    }

    public void Add(Tuple3I key, Boid boid)
    {
        if (!dict.ContainsKey(key))
        {
            dict[key] = new List<Boid>();
        }
        dict[key].Add(boid);
    }

    public List<Boid> GetNeighbors(Tuple3I key)
    {
        returnList.Clear();
        returnList.AddRange(dict[key]);

        NeighborKeys = new List<Tuple3I>
        {
            new Tuple3I(key.first + 1, key.second + 1, key.third),
            new Tuple3I(key.first + 1, key.second, key.third),
            new Tuple3I(key.first + 1, key.second - 1, key.third),
            new Tuple3I(key.first, key.second + 1, key.third),
            new Tuple3I(key.first, key.second - 1, key.third),
            new Tuple3I(key.first - 1, key.second + 1, key.third),
            new Tuple3I(key.first - 1, key.second, key.third),
            new Tuple3I(key.first - 1, key.second - 1, key.third)
        };

        foreach (Tuple3I nkey in NeighborKeys)
        {
            if (dict.ContainsKey(nkey))
            {
                returnList.AddRange(dict[nkey]);
            }
        }

        return returnList;
    }

    public void Clear()
    {
        dict.Clear();
    }


}
