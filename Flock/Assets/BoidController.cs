using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unitilities.Tuples;
public class BoidController : MonoBehaviour {


    public int numBoids;
    public float regionSize;
    public float maxSpeed;
    public float maxForce;
    public GameObject target;
    public float neighborDistance;
    private float sqrNeighborDistance;

    private Boid[] boids;
    public float radius;

    private Vector3 tempSeparate;
    private Vector3 tempAlign;
    private Vector3 tempCohesion;

    private SpatialHash spatialHash;

	// Use this for initialization
	void Start () {

        spatialHash = new SpatialHash();
        //radius = 3f;
        sqrNeighborDistance = neighborDistance * neighborDistance;
        boids = new Boid[numBoids];
	    for (int i=0; i<numBoids; i++)
        {
            boids[i] = new Boid(new Vector3(Random.value * regionSize, Random.value * regionSize, 0),
                new Vector3(Random.value * regionSize, Random.value * regionSize, 0),
                new Vector3(Random.Range(1f, -1f), Random.Range(1f, -1f), 0));
        }
	}
	
	// Update is called once per frame
	void Update () {
        //UpdateBoids();
        UpdateBoidsWithSpatialHash();
	}

    void UpdateBoids()
    {
        for (int i = 0; i < numBoids; i++)
        {
            boids[i].acceleration = boids[i].acceleration * 0;

            CheckForBounds(ref boids[i]);
            //Separate(ref boids[i]);
            Flock(ref boids[i]);
            //ApplyForce(ref boids[i], Seek(ref boids[i], target.transform.position));

            boids[i].velocity = Vector3.ClampMagnitude(boids[i].velocity + boids[i].acceleration, maxSpeed);
            //boids[i].velocity += boids[i].acceleration;
            boids[i].position += boids[i].velocity;

        }
    }

    void UpdateBoidsWithSpatialHash()
    {
        BuildSpatialHash();

        for (int i = 0; i < numBoids; i++)
        {
            boids[i].acceleration = boids[i].acceleration * 0;

            CheckForBounds(ref boids[i]);
            FlockWithSpatialHash(ref boids[i]);
            ApplyForce(ref boids[i], 0.5f*Escape(ref boids[i], target.transform.position));

            boids[i].velocity = Vector3.ClampMagnitude(boids[i].velocity + boids[i].acceleration, maxSpeed);
            boids[i].position += boids[i].velocity;

        }

    }

    Tuple3I V3ToTuple(Vector3 v)
    {
        return new Tuple3I((int)(v.x / neighborDistance), (int)(v.y / neighborDistance), (int)(v.z / neighborDistance));   
    }

    void ApplyForce(ref Boid b, Vector3 force)
    {
        b.acceleration = b.acceleration + force;
    }

    Vector3 Seek(ref Boid b, Vector3 target)
    {
        Vector3 desired = target - b.position;
        desired = maxSpeed * desired.normalized;
        Vector3 steer = desired - b.velocity;
        steer = Vector3.ClampMagnitude(steer, maxForce);
        steer = Vector3.Lerp(Vector3.zero, steer, Vector3.Distance(b.position, target) / 100f);

        return steer;
    }

    Vector3 Escape(ref Boid b, Vector3 target)
    {
        Vector3 desired = target - b.position;
        desired = maxSpeed * desired.normalized;
        Vector3 steer = b.velocity - desired;
        steer = Vector3.ClampMagnitude(steer, maxForce);
        steer = Vector3.Lerp(steer, Vector3.zero, Vector3.Distance(b.position, target) / (regionSize/5));
        return steer;
    }

    void BuildSpatialHash()
    {
        spatialHash.Clear();
        foreach (Boid b in boids)
        {
            spatialHash.Add(V3ToTuple(b.position), b);
        }
    }

    void FlockWithSpatialHash(ref Boid self)
    {
        //Vector3 separate = SeparateWithSpatialHash(ref self);
        //Vector3 align = AlignWithSpatialHash(ref self);
        //Vector3 cohesion = CohesionWithSpatialHash(ref self);

        SeparateCohesionAlignWithSpatialHash(ref self);
        ApplyForce(ref self, tempSeparate);
        ApplyForce(ref self, tempAlign);
        ApplyForce(ref self, tempCohesion);

    }

    void Flock(ref Boid self)
    {
        //Vector3 sep = separate(boids);
        Vector3 separate = Separate(ref self);
        //Vector3 align = Align(ref self);
        //Vector3 cohesion = Cohesion(ref self);

        CohesionAndAlign(ref self);
        ApplyForce(ref self, separate);
        ApplyForce(ref self, tempAlign);
        ApplyForce(ref self, tempCohesion);

    }

    Vector3 SeparateWithSpatialHash(ref Boid self)
    {
        float separation = (radius * 2) * (radius * 2);
        Vector3 sum = Vector3.zero;
        int count = 0;
        Vector3 steer = Vector3.zero;

        List<Boid> others = spatialHash.GetNeighbors(V3ToTuple(self.position));


        foreach (Boid other in others)
        {
            float dist = Vector3.SqrMagnitude(self.position - other.position);
            if ((dist > 0) && (dist < separation))
            {
                Vector3 difference = (self.position - other.position).normalized;
                difference /= Mathf.Sqrt(dist);
                sum += difference;
                count++;
            }

        }
        if (count > 0)
        {
            
            sum /= count;
            sum = sum.normalized * maxSpeed;
            steer = sum - self.velocity;
            Vector3.ClampMagnitude(steer, maxForce);
            //ApplyForce(ref self, steer);
        }

        return steer;
    }

    Vector3 Separate(ref Boid self)
    {
        float separation = (radius * 2)* (radius * 2);
        Vector3 sum = Vector3.zero;
        int count = 0;
        Vector3 steer = Vector3.zero;

        foreach (Boid other in boids)
        {
            float dist = Vector3.SqrMagnitude(self.position- other.position);
            if ((dist > 0) && (dist < separation))
            {
                Vector3 difference = (self.position - other.position).normalized;
                difference /= Mathf.Sqrt(dist);
                sum += difference;
                count++;
            }

        }
        if (count > 0)
        {
            sum /= count;
            sum = sum.normalized * maxSpeed;
            steer = sum - self.velocity;
            Vector3.ClampMagnitude(steer, maxForce);
            //ApplyForce(ref self, steer);
        }

        return steer;
    }

    Vector3 AlignWithSpatialHash(ref Boid self)
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        List<Boid> others = spatialHash.GetNeighbors(V3ToTuple(self.position));

        foreach (Boid other in others)
        {
            if (Vector3.SqrMagnitude(other.position - self.position) < sqrNeighborDistance)
            {
                sum += other.velocity;
                count++;
            }
        }


        if (count > 0)
        {
            sum /= count;
            sum = sum.normalized * maxSpeed;

            return (Vector3.ClampMagnitude(sum - self.velocity, maxForce));
        }
        else
        {
            return Vector3.zero;
        }
    }

    Vector3 Align(ref Boid self)
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (Boid other in boids)
        {
            if (Vector3.SqrMagnitude(other.position - self.position) < sqrNeighborDistance)
            {
                sum += other.velocity;
                count++;
            }
        }


        if (count > 0)
        {
            sum /= count;
            sum = sum.normalized * maxSpeed;

            return (Vector3.ClampMagnitude(sum - self.velocity, maxForce));
        } else
        {
            return Vector3.zero;
        }
    }  

    Vector3 CohesionWithSpatialHash(ref Boid self)
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        List<Boid> others = spatialHash.GetNeighbors(V3ToTuple(self.position));

        foreach (Boid other in others)
        {
            if (Vector3.SqrMagnitude(other.position - self.position) < sqrNeighborDistance)
            {
                sum += other.position;
                count++;
            }
        }
        if (count > 0)
        {
            sum /= count;
            return Seek(ref self, sum);
        }
        else
        {
            return Vector3.zero;
        }
    }

    Vector3 Cohesion(ref Boid self)
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (Boid other in boids)
        {
            if (Vector3.SqrMagnitude(other.position - self.position) < sqrNeighborDistance)
            {
                sum += other.position;
                count++;
            }
        }
        if (count > 0)
        {
            sum /= count;
            return Seek(ref self, sum);
        } else
        {
            return Vector3.zero;
        }
    }

    void CohesionAndAlign(ref Boid self)
    {
        Vector3 sumPos = Vector3.zero;
        Vector3 sumVel = Vector3.zero;

        int count = 0;
        foreach (Boid other in boids)
        {
            if (Vector3.SqrMagnitude(other.position - self.position) < sqrNeighborDistance)
            {
                sumPos += other.position;
                sumVel += other.velocity;
                count++;
            }
        }
        if (count > 0)
        {
            sumPos /= count;
            sumVel /= count;

            sumVel = sumVel.normalized * maxSpeed;
            tempAlign = (Vector3.ClampMagnitude(sumVel - self.velocity, maxForce));
            tempCohesion = Seek(ref self, sumPos);
        }
        else
        {
            tempAlign = Vector3.zero;
            tempCohesion = Vector3.zero;
        }
    }

    void SeparateCohesionAlignWithSpatialHash(ref Boid self)
    {
        float separation = (radius * 2) * (radius * 2);
        Vector3 sum = Vector3.zero;
        int count = 0;
        Vector3 steer = Vector3.zero;

        List<Boid> others = spatialHash.GetNeighbors(V3ToTuple(self.position));


        foreach (Boid other in others)
        {
            float dist = Vector3.SqrMagnitude(self.position - other.position);
            if ((dist > 0) && (dist < separation))
            {
                Vector3 difference = (self.position - other.position).normalized;
                difference /= Mathf.Sqrt(dist);
                sum += difference;
                count++;
            }

        }
        if (count > 0)
        {
            sum /= count;
            sum = sum.normalized * maxSpeed;
            steer = sum - self.velocity;
            Vector3.ClampMagnitude(steer, maxForce);
            //ApplyForce(ref self, steer);
        }

        tempSeparate = steer;


        sum = Vector3.zero;
        count = 0;

        foreach (Boid other in others)
        {
            if (Vector3.SqrMagnitude(other.position - self.position) < sqrNeighborDistance)
            {
                sum += other.velocity;
                count++;
            }
        }


        if (count > 0)
        {
            sum /= count;
            sum = sum.normalized * maxSpeed;

            tempAlign = (Vector3.ClampMagnitude(sum - self.velocity, maxForce));
        }
        else
        {
            tempAlign = Vector3.zero;
        }



        //// cohesion

        sum = Vector3.zero;
        count = 0;

        foreach (Boid other in others)
        {
            if (Vector3.SqrMagnitude(other.position - self.position) < sqrNeighborDistance)
            {
                sum += other.position;
                count++;
            }
        }
        if (count > 0)
        {
            self.numNeighbors = count;

            sum /= count;
            tempCohesion = Seek(ref self, sum);
        }
        else
        {
            tempCohesion = Vector3.zero;
        }

    }

    void CheckForBounds(ref Boid b)
    {

        bool MIRRORMIRROR = false;
        if (MIRRORMIRROR)
        {
            if (b.position.x > regionSize)
            {
                b.position.x = 0;
            }
            else if (b.position.x < 0)
            {
                b.position.x = regionSize;
            }
            else if (b.position.y > regionSize)
            {
                b.position.y = 0;
            }
            else if (b.position.y < 0)
            {
                b.position.y = regionSize;
            }
            else if (b.position.z > regionSize)
            {
                b.position.z = 0;
            }
            else if (b.position.z < 0)
            {
                b.position.z = regionSize;
            }
        }
        else
        {
            Vector3 swerve = Vector3.zero;
            if (b.position.x > regionSize)
            {
                swerve.x = -maxSpeed;
            }
            else if (b.position.x < 0)
            {
                swerve.x = maxSpeed;
            }
            else if (b.position.y > regionSize)
            {
                swerve.y = -maxSpeed;
            }
            else if (b.position.y < 0)
            {
                swerve.y = maxSpeed;
            }
            else if (b.position.z > regionSize)
            {
                swerve.z = -maxSpeed;
            }
            else if (b.position.z < 0)
            {
                swerve.z = maxSpeed;
            }

            swerve = Vector3.ClampMagnitude(swerve, maxForce);
            //Vector3 swerve = desired - b.velocity;

            ApplyForce(ref b, swerve);
        }
    }

    //private void OnDrawGizmos()
    //{
    //    for (int i=0; i<numBoids; i++)
    //    {
    //        Gizmos.color = Color.Lerp(Color.cyan, Color.magenta, boids[i].numNeighbors/30f);
    //        //Gizmos.DrawSphere(boids[i].position,0.5f);
    //        Gizmos.DrawLine(boids[i].position, boids[i].position + 2*boids[i].velocity);
    //        //Gizmos.DrawLine(boids[i].position, boids[i].position + 2 * boids[i].acceleration);
    //    }
    //}

    //public struct boid
    //{
    //    public Vector3 position;
    //    public Vector3 velocity;
    //    public Vector3 acceleration;
    //}
}
