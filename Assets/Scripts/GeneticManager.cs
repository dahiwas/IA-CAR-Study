using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MathNet.Numerics.LinearAlgebra;
using System.Linq;

public class GeneticManager : MonoBehaviour
{

    [Header("Public view")]
    public int currentGeneration;
    public int currentGenome = 0;
    public int vivos;
    public int chegada = 0;
    public float[] geralFitness;
    public int genepoolCount;
    public float bestofAll = 0;
    public NeuralNetwork[] best;


    [Header("References")]
    public GameObject car;
    public CarController controller;

    public GameObject firstPlace;
    public GameObject secondPlace;

    public GameObject[] car2;
    public CarController[] controllers;

    [Header("Controls")]
    public int initialPopulation = 85;
    [Range(0.0f, 1.0f)]
    public float mutationRate = 0.055f;

    [Header("Crossover Controls")]
    //Pegar os 8 melhores agentes que performaram bem
    public int bestAgentSelection = 8;
    public int worstAgentSelectoin = 3;
    public int numberToCrossover;

    public List<int> genePool = new List<int>();

    private int naturallySelected;

    private NeuralNetwork[] population;



    private void Start()
    {
        car2 = new GameObject[initialPopulation];
        controllers = new CarController[initialPopulation];
        geralFitness = new float[initialPopulation];
        for (int i = 0; i < initialPopulation; i++)
            car2[i] = Object.Instantiate(car);
        for (int i = 0; i < initialPopulation; i++)
        {
            controllers[i] = car2[i].GetComponent<CarController>();
            controllers[i].eu = i;
        }

        CreatePopulation();
        vivos = initialPopulation;
        
    }

    public void FixedUpdate()
    {
        for (int i = 0; i < initialPopulation; i++)
        {
            geralFitness[i] = controllers[i].overallFitness;
        }
        //Debug.Log(population.Length);
        genepoolCount = genePool.Count();
        }


    //Completamente aleatorio
    private void CreatePopulation()
    {
        best = new NeuralNetwork[initialPopulation];
        population = new NeuralNetwork[initialPopulation];
        FillPopulationWithRandomValues(population, 0);
        ResetToCurrentGenome();
    }

    private void ResetToCurrentGenome()
    {
        for (int i = 0; i < initialPopulation; i++)
            controllers[i].ResetWithNetwork(population[i]);
    }

    private void FillPopulationWithRandomValues(NeuralNetwork[] newPopulation, int startingIndex)
    {
        while (startingIndex < initialPopulation)
        {
            newPopulation[startingIndex] = new NeuralNetwork();
            //Colocando dentro de cada
            newPopulation[startingIndex].Initialise(controllers[startingIndex].LAYERS, controllers[startingIndex].NEURONS);
            startingIndex++;
        }
    }

    //O CarController mostra para o Manager qual é o fitness atual, conseguindo mapear um históricode carros
    public void Death(float fitness, NeuralNetwork network, int eu)
    {
        if(vivos > 1)
        {
            vivos--;
            //Coletar o fitness atual
            //population[currentGenome].fitness = fitness;
            //Próximo Carro
            //currentGenome++;
            //Object.Destroy(obj);
            //Reseto
            //ResetToCurrentGenome();
            population[eu].fitness = fitness;
        }
        else
        {
            /*
            for (int i = 0; i < initialPopulation; i++)
                Debug.Log(population[i].fitness);
            */
            population[eu].fitness = fitness;
            StartCoroutine(Espera(fitness));
            //Repopular caso esteja vazio
            
            
        }
    }

    IEnumerator Espera(float fitness)
    {
        yield return new WaitForSeconds(2);
        RePopulate();
    }
    private void RePopulate()
    {
        //Esvaziar a lista
        genePool.Clear();
        currentGeneration++;
        naturallySelected = 0;
        //Necessário ordenar os melhores listados
        SortPopulation();

        NeuralNetwork[] newPopulation = PickBestPopulation();

        Crossover(newPopulation);
        Mutate(newPopulation);
        FillPopulationWithRandomValues(newPopulation, naturallySelected);

        //Aqui deve recolocar os genes novos nos indiivduos com for
        population = newPopulation;
        currentGenome = 0;
        ResetToCurrentGenome();
        controllers[0].ResetWithNetwork(best[0]);
        vivos = 10;


    }

    private void Mutate( NeuralNetwork[] newPopulation)
    {
        //Para cada elemento da população entrará nos pesos de cada um e mudará
        for (int i = 0; i < naturallySelected; i++)
        {
            for (int c = 0; c < newPopulation[i].weights.Count; c++)
            {
                if(Random.Range(0.0f, 1f) < mutationRate)
                {
                    //Randomizar a mtriz de uma população
                    newPopulation[i].weights[c] = MutateMatrix(newPopulation[i].weights[c]);
                }
            }
        }
    }

    Matrix<float> MutateMatrix(Matrix<float> A)
    {
        //Mutar apenas alguns pontos, e não tudo
        int randomPoints = Random.Range(1, (A.RowCount * A.ColumnCount) / 7);

        Matrix<float> C = A;

        for (int i = 0; i < randomPoints; i++)
        {
            int randomColumn = Random.Range(0, C.ColumnCount);
            int randomRow = Random.Range(0, C.RowCount);

            C[randomRow, randomColumn] = Mathf.Clamp(C[randomRow, randomColumn]+Random.Range(-1f,1f), -1f, 1f);
        }

        return C;
    }


    private void Crossover(NeuralNetwork[] newPopulation)
    {
        for (int i = 0; i < numberToCrossover; i += 2)
        {
            int AIndex = i;
            int BIndex = i + 1;


            if(genePool.Count >= 1)
            {
                for(int l = 0; l < 100; l++)
                {
                    AIndex = genePool[Random.Range(0, genePool.Count-1)];
                    BIndex = genePool[Random.Range(0, genePool.Count-1)];

                    if (AIndex != BIndex)
                        break;
                }
            }

            NeuralNetwork Child1 = new NeuralNetwork();
            NeuralNetwork Child2 = new NeuralNetwork();

            Child1.Initialise(controllers[AIndex].LAYERS, controllers[AIndex].NEURONS);
            Child2.Initialise(controllers[BIndex].LAYERS, controllers[BIndex].NEURONS);

            Child1.fitness = 0;
            Child2.fitness = 0;


            //50% pra dar e 50% pra nao
            for (int w = 0; w < Child1.weights.Count; w++)
            {
                if (Random.Range(0.0f, 1.0f) < 0.5f)
                {
                    Child1.weights[w] = population[AIndex].weights[w];
                    Child2.weights[w] = population[BIndex].weights[w];
                }
                else
                {
                    Child2.weights[w] = population[AIndex].weights[w];
                    Child1.weights[w] = population[BIndex].weights[w];
                }
            }

            for (int w = 0; w < Child1.biases.Count; w++)
            {
                if (Random.Range(0.0f, 1.0f) < 0.5f)
                {
                    Child1.biases[w] = population[AIndex].biases[w];
                    Child2.biases[w] = population[BIndex].biases[w];
                }
                else
                {
                    Child2.biases[w] = population[AIndex].biases[w];
                    Child1.biases[w] = population[BIndex].biases[w];
                }
            }
        }
    }



    /*
     * Pq não pegar o primeiro logo? Bom tudo o que acontece aqui é aleatorio, por isso é necessário preencher o gene pool o mais randomizado
     * possível, não tem como saber o que vai acontecer
     * 
     */
    private NeuralNetwork[] PickBestPopulation()
    {
        NeuralNetwork[] newPopulation = new NeuralNetwork[initialPopulation];

        for (int i = 0; i < bestAgentSelection; i++)
        {
            //Copia da rede neural
            newPopulation[naturallySelected] = population[i].InitialiseCopy(controllers[i].LAYERS, controllers[i].NEURONS);
            newPopulation[naturallySelected].fitness = 0;
            naturallySelected++;

            //Colocar 10x esse gene no genepool, faz sentido afinal queremos mais desse tipo de gene quedeu certo, mais facil de ser selecionado nocrossover

            //int f = Mathf.RoundToInt(population[i].fitness);

            for (int c = 0; c < 3; c++)
            {
                genePool.Add(i);
            }
        }
        /*
        for (int i = 0; i < worstAgentSelectoin; i++)
        {
            int last = population.Length - 1;
            last -= i;

            //Colocar 10x esse gene no genepool, faz sentido afinal queremos mais desse tipo de gene quedeu certo, mais facil de ser selecionado nocrossover

            int f = Mathf.RoundToInt(population[last].fitness * 10);

            for (int c = 0; c < f + 1; c++)
            {
                genePool.Add(last);
            }
        }
        */
        return newPopulation;

    }

    //BubbleSort para tal ordenação
    private void SortPopulation()
    {
        /*
        for (int i = 0; i < population.Length; i++)
        {
            for(int j = 0; j < population.Length; j++)
            {
                if (population[i].fitness < population[j].fitness)
                {
                    NeuralNetwork temp = population[i];
                    population[i] = population[j];
                    population[j] = temp;

                }
            }
        }*/

        population = population.OrderByDescending(x => x.fitness).ToArray();
        controllers = controllers.OrderByDescending(x => x.overallFitness).ToArray();
        for (int i = 0; i < initialPopulation; i++)
        {
            //Debug.Log(population[i].fitness);
            //Debug.Log(controllers[i].overallFitness);
        }
            

        if (bestofAll < population[0].fitness)
            bestofAll = population[0].fitness;

        best[0] = population[0];
    }

    public void Chegou()
    {
        chegada++;
    }

}
