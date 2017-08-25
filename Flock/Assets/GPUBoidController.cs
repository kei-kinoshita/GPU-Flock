using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class GPUBoidController : MonoBehaviour {

    public bool is3D;
    public int numBoids;
    public float regionSize;
    public float maxSpeed;
    public float maxForce;
    public float neighborDistance;
    public float boidRadius;
    public ComputeShader CS;
    public Shader BoidRenderer;
    public Shader BoidTrailRenderer;
    public GameObject targetPrefab;
    public int blockSize;

    private BoidData[] boidsIn;
    private BoidData[] boidsOut;
    private Trail[] TrailData;

    private ComputeBuffer boidBufferRead;
    private ComputeBuffer boidBufferWrite;
    private ComputeBuffer magnetBufferRead;
    private ComputeBuffer boidTrailBuffer;

    private Material BoidMat;
    private Material BoidTrailMat;

    private int kernelFlock;
    private int kernelMagnet;

    private bool _DrawGizmos;

    private List<Magnet> magnets;
    private Camera mainCam;

    private int trailBufferSize;
    public int numTrails;
    private int currTrailNum;
    private int TrailKernel;

	// Use this for initialization
	void Start () {
        SetUpCS();
        _DrawGizmos = true;
        mainCam = Camera.main;
        if (BoidTrailMat == null)
            BoidTrailMat = new Material(BoidTrailRenderer);
        BoidTrailMat.SetInt("NUM_TRAILS", numTrails);
        if (BoidMat == null)
            BoidMat = new Material(BoidRenderer);
        currTrailNum = 0;
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit mouseHit = new RaycastHit();
        if (Physics.Raycast(mainCam.ScreenPointToRay(Input.mousePosition), out mouseHit))
        {
            CS.SetVector("MOUSE_POS", new Vector3(mouseHit.point.x, mouseHit.point.y, 0) );

        }


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

        currTrailNum = (currTrailNum + 1) % numTrails;
        //Debug.Log(currTrailNum);

        CS.SetInt("CURR_TRAIL_NUM", currTrailNum);
        BoidTrailMat.SetInt("CURR_TRAIL_NUM", currTrailNum);

        CS.Dispatch(kernelFlock, boidsIn.Length/blockSize, 1, 1);
        //CS.Dispatch(TrailKernel, boidsIn.Length / blockSize, 1, 1);


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

    struct Trail
    {
        public Vector3 position;
        public int trailNum;
        public int numNeighbors;
        
        public Trail(Vector3 pos, int num)
        {
            position = pos;
            trailNum = num;
            numNeighbors = 0;
        }
    }

    void SetUpCS()
    {
        boidsIn = new BoidData[numBoids];
        boidsOut = new BoidData[numBoids];

        Random.InitState(42);
        for (int i=0; i<numBoids; i++)
        {
            //bool zaxis = false;
            if (!is3D)
            {
                boidsIn[i].position = new Vector3(Random.value * regionSize, Random.value * regionSize, 0);
                boidsIn[i].velocity = new Vector3(Random.Range(1f, -1f) * maxSpeed, Random.Range(1f, -1f) * maxSpeed, 0);
            } else
            {
                boidsIn[i].position = new Vector3(Random.value * regionSize, Random.value * regionSize, Random.value * regionSize);
                boidsIn[i].velocity = new Vector3(Random.Range(1f, -1f) * maxSpeed, Random.Range(1f, -1f) * maxSpeed, Random.Range(1f, -1f) * maxSpeed);
            }
            boidsIn[i].acceleration = Vector3.zero;
            boidsIn[i].numNeighbors = 0;
        }


        kernelFlock = CS.FindKernel("Flock");
        //TrailKernel = CS.FindKernel("TrailUpdate");

        boidBufferRead = new ComputeBuffer(numBoids, Marshal.SizeOf(typeof(BoidData)));
        boidBufferWrite = new ComputeBuffer(numBoids, Marshal.SizeOf(typeof(BoidData)));
        CS.SetBuffer(kernelFlock, "boidBufferRead", boidBufferRead);
        CS.SetBuffer(kernelFlock, "boidBufferWrite", boidBufferWrite);


        CS.SetBuffer(TrailKernel, "boidBufferRead", boidBufferRead);
        //CS.SetBuffer(TrailKernel, "boidBufferWrite", boidBufferWrite);

        // set up trail;
        trailBufferSize = numTrails * numBoids;
        boidTrailBuffer = new ComputeBuffer(trailBufferSize, Marshal.SizeOf(typeof(Trail)));
        TrailData = new Trail[trailBufferSize];
        for (int i=0; i<trailBufferSize; i++)
        {
            TrailData[i] = new Trail(Vector3.zero, (i % numBoids) % numTrails);
           
        }
        boidTrailBuffer.SetData(TrailData);
        CS.SetBuffer(kernelFlock, "trailBuffer", boidTrailBuffer);
        //CS.SetBuffer(TrailKernel, "trailBuffer", boidTrailBuffer);


        CS.SetInt("BLOCK_SIZE", blockSize);
        CS.SetInt("BUFFER_SIZE",numBoids);
        CS.SetFloat("REGION_SIZE", regionSize);
        CS.SetFloat("MAX_SPEED", maxSpeed);
        CS.SetFloat("MAX_FORCE", maxForce);
        CS.SetFloat("NEIGHBOR_DISTANCE", neighborDistance);
        CS.SetFloat("BOID_RADIUS", boidRadius);
        CS.SetInt("NUM_TRAILS", numTrails);


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



        BoidTrailMat.SetBuffer("BoidTrailBuffer", boidTrailBuffer);
        BoidTrailMat.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Points, trailBufferSize);

        BoidMat.SetBuffer("BoidBuffer", boidBufferWrite);
        BoidMat.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Points, boidsIn.Length);
    }

    //private void OnDrawGizmos()
    //{
        
    //    Gizmos.color = Color.white;
    //    boidTrailBuffer.GetData(TrailData);


    //    foreach (Trail t in TrailData)
    //    {



    //        Gizmos.DrawLine(t.position, t.position + Vector3.right*10f);

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
        boidTrailBuffer.Release();
    }

    void SwapBuffer(ref ComputeBuffer src, ref ComputeBuffer dst)
    {
        ComputeBuffer tmp = src;
        src = dst;
        dst = tmp;
    }
}
