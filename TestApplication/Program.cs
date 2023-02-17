using Accord.Math;
using Accord.Neuro;
using Accord.Neuro.Learning;
using Common;

SymbolDate[] data = new SymbolDate[1];
var lines = File.ReadAllText("./test.json");
lines = lines.Replace("Open", "open").Replace("Close", "close");
data = Newtonsoft.Json.JsonConvert.DeserializeObject<SymbolDate[]>(lines);

int input_size = 5;
int output_size = 2;
int total_size = input_size + output_size;

double[][] inputs = new double[data.Length - total_size][];
double[][] outputs = new double[data.Length - total_size][];

for (int i = 0; i < data.Length - total_size; i++)
{
    var inp = data.Skip(i).Take(input_size).ToList();
    var o = data.Skip(i + input_size).Take(output_size).ToList();

    inputs[i] = inp.Select(x => (double)x.close / (double)inp[0].close).ToArray();
    outputs[i] = new double[] { o[0].close > inp.Last().close ? 1 : 0, o[0].close > inp.Last().close ? 0 : 1 };/*o.Select(x => (double)x.close / (double)inp[0].close - .5).ToArray();*/
}

ActivationNetwork network = new ActivationNetwork(
                new SigmoidFunction(),
                input_size,  // number of inputs
                10, 16, 4, // number of neurons in the hidden layer
                output_size); // number of outputs
// var t = ActivationNetwork.Load("./Checkpoint/87000.ml");

double GetRealError(int size)
{
    double r = 0;
    for (int i = 0; i < size; i++)
    {
        var result = network.Compute(inputs[i]);
        var hinx = result.IndexOf(result.Max());
        var rinx = outputs[i].IndexOf(outputs[i].Max());
        r += (hinx == rinx) ? 1 : 0;
    }
    return r / outputs.Length;
}

BackPropagationLearning teacher = new BackPropagationLearning(network);

teacher.LearningRate = 0.10;
// Train the network
double error = double.MaxValue;
// for (int i = 0; i < 100000; i++)
int e = 0;
for (int i = 2500; i < Math.Min(Math.Floor(inputs.Length * 0.8), 50000); i += Math.Min(i, 25))
{
    var inp = inputs.TakeLast(i * 1).ToArray();
    var o = outputs.TakeLast(i * 1).ToArray();
    error = double.MaxValue;
    double start = error;
    double realError = 1;
    while (realError > 0.01)
    {
        {
            error = teacher.RunEpoch(inp, o);
        }
        realError = GetRealError(i);
        {
            // Console.WriteLine(e + "," + error);
            Console.Title = (i + "," + realError);
        }
    }
    network.Save($"./Checkpoint/cp{i}.ml");
    Console.WriteLine(i + "," + realError);
    network.Save($"./Checkpoint/Final.ml");
    int Start = 8;
    List<double> perdications = data.Skip(Start).Take(input_size).Select(x => (double)x.close).ToList();
    break;
}
