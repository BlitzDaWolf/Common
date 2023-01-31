using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApplication
{
    [Serializable]
    public class QTrader
    {
        //private readonly double[,] qTable;
        public Dictionary<double[], double[]> qTable { get; set; } = new Dictionary<double[], double[]>();
        private int position;
        private double balance;
        private double startBalance;
        private int episode;
        private double learningRate;
        private double discountFactor;
        [NonSerialized]
        private Random Random;
        private Random random
        {
            get
            {
                if(Random == null)
                    Random = new Random();
                return Random;
            }
        }
        public double lastPNL { get; set; } = 0;
        public double score;

        [NonSerialized]
        private double[] prevState;
        [NonSerialized]
        private int prevAction;

        public QTrader(double learningRate, double discountFactor, double startBalance)
        {
            // lastPNL = balance - startBalance;
            // qTable = new double[2, 2];
            position = 0;
            balance = startBalance;
            episode = 0;
            this.learningRate = learningRate;
            this.discountFactor = discountFactor;
            this.startBalance = startBalance;
            Random = new Random();
        }

        public int Position
        {
            get { return position; }
        }

        public double Balance
        {
            get => balance;
            set
            {
                balance = value;
            }
        }
        public double StartBalance => startBalance;
        public double PNL
        {
            get => balance - startBalance;
            set { lastPNL = value; }
        }

        public void Train(double[] price, int side)
        {
            double[] currentState = GetState(price);
            int action = GetAction(currentState);
            double reward = GetReward(action, side);
            double[] nextState = GetState(price);


            if (prevState != null)
            {
                qTable[prevState][prevAction] = (1 - learningRate) *
                    qTable[prevState][prevAction] + learningRate *
                    (reward + discountFactor *
                        Math.Max(qTable[currentState][0], qTable[currentState][1]));
            }

            prevState = currentState;
            prevAction = action;
            episode++;
        }

        public int GetAction(double[] currentState)
        {
            /*if (random.NextDouble() < 0.5 || episode < 100)
            {
                return random.Next(0, 2);
            }
            else*/
            {
                if (qTable[currentState][0] > qTable[currentState][1])
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
        }

        public double[] GetState(double[] price)
        {
            if (!qTable.ContainsKey(price))
            {
                qTable.Add(price, Enumerable.Range(1, 2).Select(_ => random.NextDouble() * 2 - 1).ToArray());
            }
            return price;
        }

        public double GetReward(int action, int side)
        {
            return action == side ? 1 : -2;
            return score;
            return balance / startBalance;
        }

        public void Buy()
        {
            if (balance <= 0)
            {
                return;
            }
            balance -= 1;
            position += 1;
        }

        public void Sell()
        {
            if (position <= 0)
            {
                return;
            }
            balance += 1;
            position -= 1;
        }

        public void Normalize()
        {
            foreach (var item in qTable)
            {
                var mn = item.Value.Min();
                var mx = item.Value.Max() - mn;
                if (mn != 0)
                {
                    item.Value[0] -= mn;
                    item.Value[1] -= mn;

                    item.Value[0] /= mx;
                    item.Value[1] /= mx;

                    item.Value[0] = item.Value[0] * 2 - 1;
                    item.Value[1] = item.Value[1] * 2 - 1;
                }
            }
        }

        public void Mutate()
        {
            foreach (var item in qTable)
            {
                item.Value[0] += random.NextDouble() - 0.5;
                if (item.Value[0] < double.NaN)
                {
                    item.Value[0] = random.NextDouble() * 2 - 1;
                }
                item.Value[1] += random.NextDouble() - 0.5;
                if (item.Value[1] < double.NaN)
                {
                    item.Value[1] = random.NextDouble() * 2 - 1;
                }
            }
            Normalize();
        }

        public void Reverse(QTrader controll)
        {
            foreach (var item in controll.qTable)
            {
                if (qTable.ContainsKey(item.Key))
                {
                double[] a = new double[item.Value.Length];
                for (int i = 0; i < item.Value.Length; i++)
                {
                    a[i] = item.Value[i] - qTable[item.Key][i];
                    qTable[item.Key][i] = a[i] * 2;
                }
                }
            }
            Normalize();
        }
    }
}
