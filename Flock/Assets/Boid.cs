using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boid {


    public Vector3 position;
    public Vector3 velocity;
    public Vector3 acceleration;
    public float numNeighbors;


    public Boid(Vector3 p, Vector3 v, Vector3 a)
    {
        position = p;
        velocity = v;
        acceleration = a;
        numNeighbors = 0;
    }
}
