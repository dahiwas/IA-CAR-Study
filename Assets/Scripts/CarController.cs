using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NeuralNetwork))]

public class CarController : MonoBehaviour
{

    public GameObject me;
    //Toda vez que ele morrer, resetar nessa posição
    private Vector3 startPosition, startRotation;

    public int eu;

    private BoxCollider bc;
    //Valores de aceleração e rotação irão variar de -1 a 1
    [Range(-1f, 1f)]

    private NeuralNetwork network;

    public float a, t;

    //Serve para saber o quão longe o carro foi, mas também se ele está parado por muito tempo, se estiver parado, é um carro inútil
    public float timeSinceStart = 0f;

    //O algoritmo de quão longe e quão rápido está indo, esses dois fatores contribuem
    //Para marcar a pontuação do carro, o quão bem foi.
    [Header("Fitness")]
    public float overallFitness;
    //Dar o peso de cada um
    //No caso o peso da distancia é muito mais significativo
    public float distanceMultipler = 1.4f;


    //Se colocassemos um peso maior para a velocidade, teríamos um carro mais rápido, mas não necessariamente que vá mais longe
    public float avgSpeedMultipler = 0.2f;
    public float sensorMultipler = 0.1f;

    [Header("Network")]
    public int LAYERS = 1;
    public int NEURONS = 10;

    //Servem para calcular o fitness
    private Vector3 lastPosition;
    private float totalDistanceTravelled;
    private float avgSpeed;


    public bool encostou = false;

    //LineRenderer para podermos ver as linhas dos sensores
    public LineRenderer lineA;
    Ray ray;
    RaycastHit hitt;


    //Teremos 3 sensores no nosso carrinho, cada um terá um distância da origem
    //Esses sensores serão os inputs da rede neural
    public float aSensor, bSensor, cSensor;

    private void Awake(){
        //Variaveis do carro
        startPosition = transform.position;
        startRotation = transform.eulerAngles;
        bc = GetComponent<BoxCollider>();

        //Start da rede
        network = GetComponent<NeuralNetwork>();

        me = this.gameObject;

        //teste da rede
        //network.Initialise(LAYERS, NEURONS);

    }

    //Toda vez que morrermos será resetado
    public void Reset(){
        timeSinceStart = 0f;
        totalDistanceTravelled = 0;
        avgSpeed = 0f;
        lastPosition = startPosition;
        overallFitness = 0f;
        transform.position = startPosition;
        transform.eulerAngles = startRotation;
        network.Initialise(LAYERS, NEURONS);
        bc.enabled = true;
        encostou = false;
    }

    public void ResetWithNetwork(NeuralNetwork net)
    {
        network = net;
        Reset();

    }

    private void OnCollisionEnter(Collision collision){
        

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
            GameObject.FindObjectOfType<GeneticManager>().Chegou();
            bc.enabled = false;
            encostou = true;
            Death();

        }
    }

    private void FixedUpdate(){

        

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
        GameObject.FindObjectOfType<GeneticManager>().Death(overallFitness, network, eu);
    }

    private void CalculateFitness(){
        //Cada frames estaremos alterando a distancia percorrida para a última posição
        //Nao precisa a distancia exata, mas , apenas uma métrica para comparar cada carro
        totalDistanceTravelled += Vector3.Distance(transform.position, lastPosition);
        avgSpeed = totalDistanceTravelled/timeSinceStart;

        overallFitness = (totalDistanceTravelled*distanceMultipler)+(avgSpeed*avgSpeedMultipler)+
            (((aSensor+bSensor+cSensor)/3)*sensorMultipler);

        //Não está fazendo nada de útil, então basta reiniciar
        if(timeSinceStart > 20 && overallFitness < 40){
            Death();
        }
        //Seria basicamente 3 voltas inteiras
        if(overallFitness >= 1000 || timeSinceStart > 20){
            //Mas também salvar a rede em um arquivo json para uma pesquisa dessa rede para outros percursos e afins
            Death();
        }

        //Os valores dados por 20 | 40 | 1000 são hardcode, ou seja tentativa e erro

    }


    private void InputSensors(){
        //Será um vetor que estará apontando para diagonal de frente e direita
        Vector3 a = (transform.forward+transform.right);
        //Vetor que aponta para frente
        Vector3 b = (transform.forward);
        //Será um vetor que estará apontando para diagonal de frente e esquerda
        Vector3 c = (transform.forward-transform.right);

        Ray r = new Ray(transform.position, a);
        RaycastHit hit;


        //É necessário para os inputs sempre darem valor de -1 a 1 pq senão a rede neural fica presa
        if (Physics.Raycast(r , out hit)){
            //Dividir por 20 assegurará que não será um numero mt grande
            //Caso o numero fosse muito grande, quando chegar na função de ativação sigmoide
            //Ele tenderá a 1, e se ficar tendendo a 1 nunca vai diversificar
            aSensor = hit.distance / 15;
            
        }

        //Configurar os outros sensores

        float distanCarWallB = 0;
        r.direction = b;
        if (Physics.Raycast(r , out hit)){
            bSensor = hit.distance/15;
            distanCarWallB = hit.distance;
        }
        
        r.direction = c;
        if (Physics.Raycast(r , out hit)){
            cSensor = hit.distance / 15;
        }

        Debug.DrawRay(transform.position, b, Color.red);
        Debug.DrawRay(transform.position, c, Color.red);
        Debug.DrawRay(transform.position, a, Color.red);


    }

    //Será a função que moverá o carro
    private Vector3 inp;
    public void MoveCar (float v, float h) {
        //Estamos indo do 0 para o valor da aceleração*11.4 com uma taxa de 0.02
        //Esses números não são os mais intuitivos, mas foi o que levou a crer um incremento mais natural
        inp = Vector3.Lerp(Vector3.zero, new Vector3(0,0,v*11.4f), 0.02f);
        //Essa função converte o que está no input para o carro de forma relativa a ele, e não ao mundo
        inp = transform.TransformDirection(inp);
        //Faz o carro se mexer
        transform.position += inp;

        //Dependendo do valor dado h, ele virará 90 graus ou -90 graus
        //Mas para ser mais realista e não virar de uma vez, é multiplicado por 0.02f
        transform.eulerAngles += new Vector3(0, (h*90)*0.02f, 0);
    }



}
