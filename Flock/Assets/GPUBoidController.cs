using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class GPUBoidController : MonoBehaviour {

    public int numBoids;
    public float regionSize;
    public float maxSpeed;
    public float maxForce;
    public float neighborDistance;
    public float boidRadius;
    public ComputeShader CS;
    public Shader BoidRenderer;
    public GameObject target1;
    public GameObject target2;
    public GameObject target3;


    private BoidData[] boidsIn;
    private BoidData[] boidsOut;

    private ComputeBuffer boidBufferRead;
    private ComputeBuffer boidBufferWrite;

    private Material BoidMat;


    private int kernelFlock;

    private bool _DrawGizmos;


	// Use this for initialization
	void Start () {
        SetUpCS();
        _DrawGizmos = true;
	}

    // Update is called once per frame
    void Update()
    {
        CS.SetVector("TARGET_1", target1.transform.position);
        CS.SetVector("TARGET_2", target2.transform.position);
        CS.SetVector("TARGET_3", target3.transform.position);

        
        CS.Dispatch(kernelFlock, boidsIn.Length/16, 1, 1);


        //boidBufferWrite.GetData(boidsIn);

        //SwapBuffer(ref boidBufferRead, ref boidBufferWrite);
       

    }

    struct BoidData
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 acceleration;
        public int numNeighbors;
    }



    void SetUpCS()
    {
        boidsIn = new BoidData[numBoids];
        boidsOut = new BoidData[numBoids];

        Random.InitState(0);
        for (int i=0; i<numBoids; i++)
        {
            boidsIn[i].position = new Vector3(Random.value * regionSize, Random.value * regionSize, 0);
            boidsIn[i].velocity = new Vector3(Random.Range(1f, -1f), Random.Range(1f, -1f), 0);
            boidsIn[i].acceleration = Vector3.zero;
            boidsIn[i].numNeighbors = 0;
        }


        kernelFlock = CS.FindKernel("Flock");
        boidBufferRead = new ComputeBuffer(numBoids, Marshal.SizeOf(typeof(BoidData)));
        boidBufferWrite = new ComputeBuffer(numBoids, Marshal.SizeOf(typeof(BoidData)));
        

        CS.SetBuffer(kernelFlock, "boidBufferRead", boidBufferRead);
        CS.SetBuffer(kernelFlock, "boidBufferWrite", boidBufferWrite);
        CS.SetInt("bufferSize",numBoids);
        CS.SetFloat("REGION_SIZE", regionSize);

        CS.SetFloat("MAX_SPEED", maxSpeed);
        CS.SetFloat("MAX_FORCE", maxForce);
        CS.SetFloat("NEIGHBOR_DISTANCE", neighborDistance);
        CS.SetFloat("BOID_RADIUS", boidRadius);


        boidBufferRead.SetData(boidsIn);
    }

    private void OnRenderObject()
    {
        if (BoidMat == null)
            BoidMat = new Material(BoidRenderer);

        BoidMat.SetBuffer("BoidBuffer", boidBufferWrite);
        BoidMat.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Points, boidsIn.Length);
    }

    //private void OnDrawGizmos()
    //{
    //    if (_DrawGizmos)
    //    {
    //        Gizmos.color = Color.white;
    //        foreach (BoidData b in boidsIn)
    //        {
    //            Gizmos.DrawLine(b.position, b.position + b.velocity);
    //        }
    //    }
    //}


    private void OnDisable()
    {
        CSReleaseBuffer();
        _DrawGizmos = false;
    }

    private void CSReleaseBuffer()
    {
        boidBufferRead.Release();
        boidBufferWrite.Release();
    }

    void SwapBuffer(ref ComputeBuffer src, ref ComputeBuffer dst)
    {
        ComputeBuffer tmp = src;
        src = dst;
        dst = tmp;
    }
}
