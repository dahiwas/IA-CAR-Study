using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using MathNet.Numerics.LinearAlgebra;
using System.Linq;

public class GM : MonoBehaviour
{
    [Header("References")]
    public CC controller;
    public Text individuo;
    public Text geracao;
    public Text best;
    public Text crossover;
    public Text popinitial;
    public Text mutation;

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

    private NEURAL[] population;

    [Header("Public View")]
    public int currentGeneration;
    public int currentGenome = 0;
    public int chegada;

    public InputField popInicial;
    public InputField inputMutation;
    public InputField inputCrossover;
    public InputField inputBest;

    private void Start()
    {
        bestAgentSelection = int.Parse(best.text);
        mutationRate = float.Parse(mutation.text);
        initialPopulation = int.Parse(popinitial.text);
        numberToCrossover = int.Parse(crossover.text);
        CreatePopulation();
    }

    public void Restart()
    {
        popinitial.text = popInicial.text;
        popInicial.text = "";
        mutation.text = inputMutation.text;
        inputMutation.text = "";
        best.text = inputBest.text;
        inputBest.text = "";
        crossover.text = inputCrossover.text;
        inputCrossover.text = "";
        bestAgentSelection = int.Parse(best.text);
        mutationRate = float.Parse(mutation.text);
        initialPopulation = int.Parse(popinitial.text);
        numberToCrossover = int.Parse(crossover.text);
        CreatePopulation();
    }

    private void CreatePopulation()
    {

        currentGenome = 0;
        currentGeneration = 0;
        geracao.text = currentGeneration.ToString();
        individuo.text = currentGenome.ToString();
        population = new NEURAL[initialPopulation];
        FillPopulationWithRandomValues(population, 0);
        ResetToCurrentGenome();
    }

    private void ResetToCurrentGenome()
    {
        controller.ResetWithNetwork(population[currentGenome]);
    }

    private void FillPopulationWithRandomValues(NEURAL[] newPopulation, int startingIndex)
    {
        while (startingIndex < initialPopulation)
        {
            newPopulation[startingIndex] = new NEURAL();
            newPopulation[startingIndex].Initialise(controller.LAYERS, controller.NEURONS);
            startingIndex++;
        }
    }

    public void Death(float fitness, NEURAL network)
    {
        population[currentGenome].fitness = fitness;
        currentGenome = currentGenome + 1;
        individuo.text = currentGenome.ToString();
        if (currentGenome < population.Length - 1)
        {

            
            ResetToCurrentGenome();

        }
        else
        {
            RePopulate();
            individuo.text = currentGenome.ToString();
        }

    }


    private void RePopulate()
    {
        genePool.Clear();
        currentGeneration++;
        geracao.text = currentGeneration.ToString();
        naturallySelected = 0;
        SortPopulation();

        NEURAL[] newPopulation = PickBestPopulation();

        Crossover(newPopulation);
        Mutate(newPopulation);

        FillPopulationWithRandomValues(newPopulation, naturallySelected);

        population = newPopulation;

        currentGenome = 0;

        ResetToCurrentGenome();

    }


    private void Mutate(NEURAL[] newPopulation)
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

    private void Crossover(NEURAL[] newPopulation)
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

            NEURAL Child1 = new NEURAL();
            NEURAL Child2 = new NEURAL();

            Child1.Initialise(controller.LAYERS, controller.NEURONS);
            Child2.Initialise(controller.LAYERS, controller.NEURONS);

            Child1.fitness = 0;
            Child2.fitness = 0;


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

            newPopulation[naturallySelected] = Child1;
            naturallySelected++;

            newPopulation[naturallySelected] = Child2;
            naturallySelected++;

        }
    }

    private NEURAL[] PickBestPopulation()
    {

        NEURAL[] newPopulation = new NEURAL[initialPopulation];

        for (int i = 0; i < bestAgentSelection; i++)
        {
            newPopulation[naturallySelected] = population[i].InitialiseCopy(controller.LAYERS, controller.NEURONS);
            newPopulation[naturallySelected].fitness = 0;
            naturallySelected++;

            int f = Mathf.RoundToInt(population[i].fitness * 10);

            for (int c = 0; c < f; c++)
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
        return newPopulation;

    }

    private void SortPopulation()
    {/*
        for (int i = 0; i < population.Length; i++)
        {
            for (int j = i; j < population.Length; j++)
            {
                if (population[i].fitness < population[j].fitness)
                {
                    NEURAL temp = population[i];
                    population[i] = population[j];
                    population[j] = temp;
                }
            }
        }
        */
        population = population.OrderByDescending(x => x.fitness).ToArray();
    }

    public void Chegou()
    {

        chegada++;
    }
}