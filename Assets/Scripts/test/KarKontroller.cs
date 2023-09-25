using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(NNet))]

public class KarKontroller : MonoBehaviour
{
    private Vector3 startPosition, startRotation;
    public NNet network;

    [Range(-1f, 1f)]
    public float a, t;

    public float timeSinceStart = 0f;

    [Header("Fitness")]
    public float overallFitness;
    public float distanceMultipler = 1.4f;
    public float avgSpeedMultiplier = 0.2f;
    public float sensorMultiplier = 0.1f;

    [Header("Network Options")]
    public int LAYERS = 1;
    public int NEURONS = 10;

    private Vector3 lastPosition;
    private float totalDistanceTravelled;
    private float avgSpeed;

    private float aSensor, bSensor, cSensor;

    public int eu;
    public BoxCollider bc;
    public bool encostou;

    private void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.eulerAngles;
        network = GetComponent<NNet>();
        bc = GetComponent<BoxCollider>();


    }

    public void ResetWithNetwork(NNet net)
    {
        network = net;
        Reset();
    }

    public void ResetWithoutNetwork()
    {
        Reset();
    }



    public void Reset()
    {

        timeSinceStart = 0f;
        totalDistanceTravelled = 0;
        avgSpeed = 0f;
        lastPosition = startPosition;
        overallFitness = 0f;
        transform.position = startPosition;
        transform.eulerAngles = startRotation;
        //network.Initialise(LAYERS, NEURONS);
        bc.enabled = true;
        encostou = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
       
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.tag == "Parede")
        {
            bc.enabled = false;
            encostou = true;
            Death();
        }
        if (collision.gameObject.tag == "chegada")
        {
            GameObject.FindObjectOfType<GenetikManager>().Chegou();
            bc.enabled = false;
            encostou = true;
            Death();

        }
    }

    private void FixedUpdate()
    {

        if (!encostou)
        {
            InputSensors();
            lastPosition = transform.position;

            (a, t) = network.RunNetwork(aSensor, bSensor, cSensor);
            timeSinceStart += Time.deltaTime;
            MoveCar(a, t);
            CalculateFitness();
        }

        else
            MoveCar(0, 0);

        //a = 0;
        //t = 0;


    }

    private void Death()
    {
        GameObject.FindObjectOfType<GenetikManager>().Death(overallFitness, network, eu);
    }

    private void CalculateFitness()
    {

        totalDistanceTravelled += Vector3.Distance(transform.position, lastPosition);
        avgSpeed = totalDistanceTravelled / timeSinceStart;

        overallFitness = (totalDistanceTravelled * distanceMultipler) + (avgSpeed * avgSpeedMultiplier) + (((aSensor + bSensor + cSensor) / 3) * sensorMultiplier);

        if (timeSinceStart > 20 && overallFitness < 40)
        {
            Death();
        }

        if (overallFitness >= 1000)
        {
            Death();
        }

    }

    private void InputSensors()
    {

        Vector3 a = (transform.forward + transform.right);
        Vector3 b = (transform.forward);
        Vector3 c = (transform.forward - transform.right);

        Ray r = new Ray(transform.position, a);
        RaycastHit hit;

        if (Physics.Raycast(r, out hit))
        {
            aSensor = hit.distance / 20;
            Debug.DrawLine(r.origin, hit.point, Color.red);
        }

        r.direction = b;

        if (Physics.Raycast(r, out hit))
        {
            bSensor = hit.distance / 20;
            Debug.DrawLine(r.origin, hit.point, Color.red);
        }

        r.direction = c;

        if (Physics.Raycast(r, out hit))
        {
            cSensor = hit.distance / 20;
            Debug.DrawLine(r.origin, hit.point, Color.red);
        }

    }

    private Vector3 inp;
    public void MoveCar(float v, float h)
    {
        inp = Vector3.Lerp(Vector3.zero, new Vector3(0, 0, v * 11.4f), 0.02f);
        inp = transform.TransformDirection(inp);
        transform.position += inp;

        transform.eulerAngles += new Vector3(0, (h * 90) * 0.02f, 0);
    }

}