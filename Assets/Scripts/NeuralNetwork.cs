using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MathNet.Numerics.LinearAlgebra;
using System;

//Criar numeros aleatorios
using Random = UnityEngine.Random;

public class NeuralNetwork : MonoBehaviour
{
    //Criar a matriz dos inputs
    public Matrix<float> inputLayer = Matrix<float>.Build.Dense(1, 3);
    //As hiddenLayers será uma lista de matrizes, pois pode se colocar muitas matrizes para aumentar a rapidez
    public List<Matrix<float>> hiddenLayers = new List<Matrix<float>>();
    //Criar a matriz dos outputs
    public Matrix<float> outputlayer = Matrix<float>.Build.Dense(1, 2);
    //Os pesos também será uma lista de matrizes
    public List<Matrix<float>> weights = new List<Matrix<float>>();
    //Os biases será uma lista de valores 
    public List<float> biases = new List<float>();
    //Variavel para poder controlar os bias
    public float fitness;

    //Inicializar a rede neural
    public void Initialise(int hiddenLayerCount, int hiddenNeuronCount)
    {
        //Inicialização de todas as matrizes/listas
        inputLayer.Clear();
        hiddenLayers.Clear();
        outputlayer.Clear();
        weights.Clear();
        biases.Clear();

        //Para cada HiddenLayer é necessário adicionar os pesos
        for (int i = 0; i < hiddenLayerCount + 1; i++)
        {
            Matrix<float> f = Matrix<float>.Build.Dense(1, hiddenNeuronCount);

            hiddenLayers.Add(f);

            //Colocar dentro da lista dos biases os numeros aleatorios
            biases.Add(Random.Range(-1f,1f));
            
            //Weights
            /*
                                        I1  I2  I3
            [I1]        [H1]        H1  a   b   c
            [I2]    x   [H2]    =   H2  d   e   f
            [I3]
             
             */
            //é necessario o ajuste por conta de q a qtd de linhas dos inputs pode ser diferente das hiddenlayers
            if (i == 0)
            {
                Matrix<float> inputToH1 = Matrix<float>.Build.Dense(3, hiddenNeuronCount);
                weights.Add(inputToH1);
            }

            Matrix<float> HiddenToHidden = Matrix<float>.Build.Dense(hiddenNeuronCount, hiddenNeuronCount);
            weights.Add(HiddenToHidden);
        }
        //Agora é necessário adicionar a matrix final de output
        Matrix<float> OutputWeight = Matrix<float>.Build.Dense(hiddenNeuronCount, 2);
        weights.Add(OutputWeight);
        biases.Add(Random.Range(-1f, 1f));

        RandomiseWeights();


    }

    public void RandomiseWeights()
    {
        for (int i = 0; i < weights.Count; i++)
        {
            for (int x = 0; x < weights[i].RowCount; x++)
            {
                for (int y = 0; y < weights[i].ColumnCount; y++)
                {
                    weights[i][x, y] = Random.Range(-1f, 1f);
                }
            }
        }
    }

    public (float, float) RunNetwork (float a, float b, float c)
    {
        inputLayer[0, 0] = a;
        inputLayer[0, 1] = b;
        inputLayer[0, 2] = c;

        //Não queremos perder nenhum dado pois, se utilizassemos a sigmoide já agora, perderiamos os numeros negativos, como o volante
        //varia de -1 a 1 então é melhor seja mantido os valores passados, mesmo negativos, e para a aceleração fariamos posteriormente
        //a função sigmoide propriamente dita
        inputLayer = inputLayer.PointwiseTanh();

        //Input*Peso + bias
        hiddenLayers[0] = ((inputLayer * weights[0]) + biases[0]).PointwiseTanh();

        for (int i = 1; i < hiddenLayers.Count; i++)
        {
            hiddenLayers[i] = ((hiddenLayers[i - 1] * weights[i]) + biases[i]).PointwiseTanh();
        }

        //Primeiro output é a aceleração e o segundo será o volante
        outputlayer = ((hiddenLayers[hiddenLayers.Count - 1] * weights[weights.Count - 1]) + biases[biases.Count - 1]).PointwiseTanh();


        return (Sigmoid(outputlayer[0,0]), (float)Math.Tanh(outputlayer[0,1]));
    }

    private float Sigmoid(float s)
    {
        return (1 / (1 + Mathf.Exp(-s)));
    }


    //Realizar uma cópia do estado atual para que possa ser adquirido no GeneticManager
    public NeuralNetwork InitialiseCopy(int hiddenLayerCount, int hiddenNeuronCount)
    {
        NeuralNetwork n = new NeuralNetwork();
        List<Matrix<float>> newWeights = new List<Matrix<float>>();
        for (int i = 0; i < this.weights.Count; i++)
        {
            Matrix<float> currentWeight = Matrix<float>.Build.Dense(weights[i].RowCount, weights[i].ColumnCount);

            for (int x = 0; x < currentWeight.RowCount; x++)
            {
                for (int y = 0; y < currentWeight.ColumnCount; y++)
                {
                    currentWeight[x, y] = weights[i][x, y];
                }
            }

            newWeights.Add(currentWeight);
        }
        List<float> newBiases = new List<float>();

        newBiases.AddRange(biases);

        n.weights = newWeights;
        n.biases = newBiases;

        n.InitialiseHidden(hiddenLayerCount, hiddenNeuronCount);
        return n;
    }

    public void InitialiseHidden(int hiddenLayerCount, int hiddenNeuronCount)
    {
        inputLayer.Clear();
        hiddenLayers.Clear();
        outputlayer.Clear();

        for (int i = 0; i < hiddenLayerCount+1; i++)
        {
            Matrix<float> newHiddenLayer = Matrix<float>.Build.Dense(1, hiddenNeuronCount);
            hiddenLayers.Add(newHiddenLayer);
        }
    }

}
