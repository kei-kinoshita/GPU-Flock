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
    public ComputeShader CS;

    private BoidData[] boidsIn;
    BoidData[] boidsOut;

    private ComputeBuffer boidBufferRead;
    private ComputeBuffer boidBufferWrite;


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

        boidBufferRead.SetData(boidsIn);
        CS.Dispatch(kernelFlock, boidsIn.Length, 1, 1);
        boidBufferWrite.GetData(boidsIn);

        //SwapBuffer(ref boidBufferRead, ref boidBufferWrite);
       

    }
    struct BoidData
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 acceleration;
    }


    private void OnDisable()
    {
        CSReleaseBuffer();
        _DrawGizmos = false;
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
    }

    private void OnDrawGizmos()
    {
        if (_DrawGizmos)
        {
            Gizmos.color = Color.white;
            foreach (BoidData b in boidsIn)
            {
                Gizmos.DrawLine(b.position, b.position + b.velocity);
            }
        }
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
