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
    public GameObject targetPrefab;
    public int blockSize;

    private BoidData[] boidsIn;
    private BoidData[] boidsOut;

    private ComputeBuffer boidBufferRead;
    private ComputeBuffer boidBufferWrite;
    private ComputeBuffer magnetBufferRead;

    private Material BoidMat;


    private int kernelFlock;
    private int kernelMagnet;

    private bool _DrawGizmos;

    private List<Magnet> magnets;
    private Camera mainCam;


	// Use this for initialization
	void Start () {
        SetUpCS();
        _DrawGizmos = true;
        mainCam = Camera.main;
        
	}

    // Update is called once per frame
    void Update()
    {

        if (Input.GetMouseButtonDown(0))
        { // Left mouse button clicked

            Debug.Log("L mouse clicked");

            RaycastHit raycastHit = new RaycastHit();
            if (Physics.Raycast(mainCam.ScreenPointToRay(Input.mousePosition), out raycastHit))
            {
                Debug.Log("L mouse clicked Y");

                magnets.Add(new Magnet(new Vector3(raycastHit.point.x, raycastHit.point.y, 0), neighborDistance, true));
                GameObject currMagnet = Instantiate(targetPrefab);
                currMagnet.transform.position = magnets[magnets.Count - 1].position;
                UpdateMagnets();
                
            }
        } else if (Input.GetMouseButtonDown(1))
        { // right mouse button clicked

            Debug.Log("R mouse clicked");

            RaycastHit raycastHit = new RaycastHit();
            if (Physics.Raycast(mainCam.ScreenPointToRay(Input.mousePosition), out raycastHit))
            {
                magnets.Add(new Magnet(new Vector3(raycastHit.point.x, raycastHit.point.y, 0), neighborDistance, false));
                GameObject currMagnet = Instantiate(targetPrefab);
                currMagnet.transform.position = magnets[magnets.Count - 1].position;
                UpdateMagnets();
            }
        }

        CS.SetFloat("DELTA_TIME", Time.deltaTime);
        //CS.SetVector("TARGET_1", target1.transform.position);
        //CS.SetVector("TARGET_2", target2.transform.position);
        //CS.SetVector("TARGET_3", target3.transform.position);

        CS.SetInt("BLOCK_SIZE", blockSize);
        CS.Dispatch(kernelFlock, boidsIn.Length/blockSize, 1, 1);


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


    struct Magnet
    {
        public Vector3 position;
        public float range;
        public bool isAttractor;

        public Magnet(Vector3 pos, float ran, bool attractor)
        {
            position = pos;
            range = ran;
            isAttractor = attractor;
        }
    }

    void SetUpCS()
    {
        boidsIn = new BoidData[numBoids];
        boidsOut = new BoidData[numBoids];

        Random.InitState(0);
        for (int i=0; i<numBoids; i++)
        {
            boidsIn[i].position = new Vector3(Random.value * regionSize, Random.value * regionSize, 0);
            boidsIn[i].velocity = new Vector3(Random.Range(1f, -1f)*maxSpeed, Random.Range(1f, -1f)*maxSpeed, 0);
            boidsIn[i].acceleration = Vector3.zero;
            boidsIn[i].numNeighbors = 0;
        }


        kernelFlock = CS.FindKernel("Flock");
        boidBufferRead = new ComputeBuffer(numBoids, Marshal.SizeOf(typeof(BoidData)));
        boidBufferWrite = new ComputeBuffer(numBoids, Marshal.SizeOf(typeof(BoidData)));
        

        CS.SetBuffer(kernelFlock, "boidBufferRead", boidBufferRead);
        CS.SetBuffer(kernelFlock, "boidBufferWrite", boidBufferWrite);
        CS.SetInt("BUFFER_SIZE",numBoids);
        CS.SetFloat("REGION_SIZE", regionSize);

        CS.SetFloat("MAX_SPEED", maxSpeed);
        CS.SetFloat("MAX_FORCE", maxForce);
        CS.SetFloat("NEIGHBOR_DISTANCE", neighborDistance);
        CS.SetFloat("BOID_RADIUS", boidRadius);


        boidBufferRead.SetData(boidsIn);

        magnets = new List<Magnet>();
        CS.SetInt("NUM_MAGNETS", magnets.Count);
    }

    void UpdateMagnets()
    {
        if (magnetBufferRead != null)
        {
            magnetBufferRead.Release();
        }
        magnetBufferRead = new ComputeBuffer(magnets.Count, Marshal.SizeOf(typeof(Magnet)));
        magnetBufferRead.SetData(magnets.ToArray());
        CS.SetBuffer(kernelFlock, "magnetBufferRead", magnetBufferRead);
        CS.SetInt("NUM_MAGNETS", magnets.Count);
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
