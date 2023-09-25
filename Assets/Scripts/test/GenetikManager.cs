using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MathNet.Numerics.LinearAlgebra;
using System.Linq;

public class GenetikManager : MonoBehaviour
{
    [Header("References")]
    public KarKontroller controller;

    [Header("Controls")]
    public int initialPopulation = 85;
    [Range(0.0f, 1.0f)]
    public float mutationRate = 0.055f;

    [Header("Crossover Controls")]
    public int bestAgentSelection = 8;
    public int worstAgentSelection = 3;
    public int numberToCrossover;

    private List<int> genePool = new List<int>();

    private int naturallySelected;

    private NNet[] population;

    [Header("Public View")]
    public int currentGeneration;
    public int currentGenome = 0;

    [Header("Novo")]
    public GameObject car;
    public GameObject[] car2;
    public KarKontroller[] controllers;
    public float[] geralFitness;
    public int vivos;
    public float bestofAll = 0 ;
    public float reference = 999;
    public int worstofAll;
    public int chegada;
    //public GameObject[] news;


    private void Start()
    {
        naturallySelected = 0;
        chegada = 0;
        reference = 999;
        car2 = new GameObject[initialPopulation];
        //controllers = new KarKontroller[initialPopulation];
        geralFitness = new float[initialPopulation];
        /*
        for (int i = 0; i < initialPopulation; i++)
            car2[i] = Object.Instantiate(car);
        */
        for (int i = 0; i < initialPopulation; i++) { 
        
            //controllers[i] = car2[i].GetComponent<KarKontroller>();
            controllers[i].eu = i;
        }
        

        CreatePopulation();
        vivos = initialPopulation;
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < initialPopulation; i++)
        {
            geralFitness[i] = controllers[i].overallFitness;
        }
    }

    private void CreatePopulation()
    {
        population = new NNet[initialPopulation];
        FillPopulationWithRandomValues(population, 0);
        ResetToCurrentGenome();
    }

    private void ResetToCurrentGenome()
    {
        for (int i = 0; i < initialPopulation; i++)
            controllers[i].ResetWithNetwork(population[i]);
    }

    private void FillPopulationWithRandomValues(NNet[] newPopulation, int startingIndex)
    {
        while (startingIndex < initialPopulation)
        {
            newPopulation[startingIndex] = new NNet();
            newPopulation[startingIndex].Initialise(controller.LAYERS, controller.NEURONS);
            startingIndex++;
        }
    }

    public void Death(float fitness, NNet network, int eu)
    {           

        population[eu].fitness = fitness;

        if (bestofAll < population[eu].fitness)
            bestofAll = population[eu].fitness;

        if (reference > population[eu].fitness)
        {
            reference = population[eu].fitness;
            worstofAll = eu;
        }
        if (currentGenome < population.Length - 1)
        { 
            currentGenome++;
            //ResetToCurrentGenome();

        }
        else
        {
            //RePopulate();
            StartCoroutine(Espera());
        }

    }

    IEnumerator Espera()
    {
        yield return new WaitForSeconds(1);
        
        genePool.Clear();
        currentGeneration++;
        naturallySelected = 0;
        
        SortPopulation();

        //currentGenome = 7;
        for (int i = 0; i < bestAgentSelection; i++)
        {
            controllers[naturallySelected].ResetWithoutNetwork();
            controllers[naturallySelected].overallFitness = 0;
            naturallySelected++;
        }
            

        
        //PickBestPopulation();
        
        //Crossover(population);
        //Mutate(population);

        FillPopulationWithRandomValues(population, naturallySelected);
        
        currentGenome = 0;

        for (int i = 0; naturallySelected < initialPopulation; i++)
        {
            controllers[naturallySelected].ResetWithNetwork(population[naturallySelected]);
            controllers[naturallySelected].overallFitness = 0;
            naturallySelected++;
        }
        //ResetToCurrentGenome();


    }

    private void Mutate(NNet[] newPopulation)
    {

        for (int i = 0; i < naturallySelected; i++)
        {

            for (int c = 0; c < newPopulation[i].weights.Count; c++)
            {

                if (Random.Range(0.0f, 1.0f) < mutationRate)
                {
                    newPopulation[i].weights[c] = MutateMatrix(newPopulation[i].weights[c]);
                }

            }

        }

    }

    Matrix<float> MutateMatrix(Matrix<float> A)
    {

        int randomPoints = Random.Range(1, (A.RowCount * A.ColumnCount) / 7);

        Matrix<float> C = A;

        for (int i = 0; i < randomPoints; i++)
        {
            int randomColumn = Random.Range(0, C.ColumnCount);
            int randomRow = Random.Range(0, C.RowCount);

            C[randomRow, randomColumn] = Mathf.Clamp(C[randomRow, randomColumn] + Random.Range(-1f, 1f), -1f, 1f);
        }

        return C;

    }

    private void Crossover(NNet[] newPopulation)
    {
        for (int i = 0; i < numberToCrossover; i += 2)
        {
            int AIndex = i;
            int BIndex = i + 1;

            if (genePool.Count >= 1)
            {
                for (int l = 0; l < 100; l++)
                {
                    AIndex = genePool[Random.Range(0, genePool.Count)];
                    BIndex = genePool[Random.Range(0, genePool.Count)];

                    if (AIndex != BIndex)
                        break;
                }
            }

            KarKontroller Child1 = controllers[AIndex];
            KarKontroller Child2 = controllers[BIndex];

            Child1.overallFitness = 0;
            Child2.overallFitness = 0;


            for (int w = 0; w < Child1.network.weights.Count; w++)
            {

                if (Random.Range(0.0f, 1.0f) < 0.5f)
                {
                    Child1.network.weights[w] = population[AIndex].weights[w];
                    Child2.network.weights[w] = population[BIndex].weights[w];
                }
                else
                {
                    Child2.network.weights[w] = population[AIndex].weights[w];
                    Child1.network.weights[w] = population[BIndex].weights[w];
                }

            }


            for (int w = 0; w < Child1.network.biases.Count; w++)
            {

                if (Random.Range(0.0f, 1.0f) < 0.5f)
                {
                    Child1.network.biases[w] = population[AIndex].biases[w];
                    Child2.network.biases[w] = population[BIndex].biases[w];
                }
                else
                {
                    Child2.network.biases[w] = population[AIndex].biases[w];
                    Child1.network.biases[w] = population[BIndex].biases[w];
                }

            }
            controllers[naturallySelected] = Child1;
            controllers[naturallySelected].ResetWithoutNetwork();
            //newPopulation[naturallySelected] = Child1;
            naturallySelected++;
            controllers[naturallySelected] = Child2;
            controllers[naturallySelected].ResetWithoutNetwork();
            //newPopulation[naturallySelected] = Child2;
            naturallySelected++;

        }
    }

    private void PickBestPopulation()
    {
        for (int i = 0; i < bestAgentSelection; i++)
        {
            population[naturallySelected].fitness = 0;
            naturallySelected++;

            //int f = Mathf.RoundToInt(population[i].fitness * 10);

            for (int c = 0; c < 12; c++)
            {
                genePool.Add(i);
            }

            
        }
        /*
        for (int i = 0; i < worstAgentSelection; i++)
        {
            int last = population.Length - 1;
            last -= i;

            int f = Mathf.RoundToInt(population[last].fitness * 10);

            for (int c = 0; c < f; c++)
            {
                genePool.Add(last);
            }

        }
        */

    }

    private void SortPopulation()
    {
        /*
        for (int i = 0; i < population.Length; i++)
        {
            for (int j = i; j < population.Length; j++)
            {
                if (population[i].fitness < population[j].fitness)
                {
                    NNet temp = population[i];
                    population[i] = population[j];
                    population[j] = temp;
                }
            }
        }
        */
        population = population.OrderByDescending(x => x.fitness).ToArray();
        controllers = controllers.OrderByDescending(x => x.overallFitness).ToArray();


    }

    public void Chegou()
    {
        chegada++;
    }
}