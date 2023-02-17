using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApplication
{
    public static class Saver
    {
        /// <summary>
        /// Writes the given object instance to a binary file.
        /// <para>Object type (and all child types) must be decorated with the [Serializable] attribute.</para>
        /// <para>To prevent a variable from being serialized, decorate it with the [NonSerialized] attribute; cannot be applied to properties.</para>
        /// </summary>
        /// <typeparam name="T">The type of object being written to the binary file.</typeparam>
        /// <param name="filePath">The file path to write the object instance to.</param>
        /// <param name="objectToWrite">The object instance to write to the binary file.</param>
        /// <param name="append">If false the file will be overwritten if it already exists. If true the contents will be appended to the file.</param>
        public static void WriteToBinaryFile<T>(string filePath, T objectToWrite, bool append = false)
        {
            File.WriteAllText(filePath, Newtonsoft.Json.JsonConvert.SerializeObject(objectToWrite));
        }

        /// <summary>
        /// Reads an object instance from a binary file.
        /// </summary>
        /// <typeparam name="T">The type of object to read from the binary file.</typeparam>
        /// <param name="filePath">The file path to read the object instance from.</param>
        /// <returns>Returns a new instance of the object read from the binary file.</returns>
        public static T ReadFromBinaryFile<T>(string filePath)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath));
        }

        public static TKey GetClosestKey<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey targetKey)
    where TKey : IComparable<TKey>
        {
            TKey closestKey = default(TKey);
            double closestDifference = double.MaxValue;

            foreach (var key in dict.Keys)
            {
                double difference = Math.Abs((double)(key.CompareTo(targetKey)));
                if (difference < closestDifference)
                {
                    closestDifference = difference;
                    closestKey = key;
                }
            }

            return closestKey;
        }
    }
}
