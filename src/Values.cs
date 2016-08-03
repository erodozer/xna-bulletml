using System;
using System.Data;
using System.Text.RegularExpressions;

namespace github.io.nhydock.BulletML
{
    namespace Specification
    {
        using System.Xml.Serialization;
        public class ValueNode
        {
        }

        [Serializable]
        public class Direction : ValueNode
        {
            public const string ABSOLUTE = "absolute";
            public const string RELATIVE = "relative";
            public const string SEQUENCE = "sequence";
            public const string AIM = "aim";

            /// <summary>
            /// "aim" type means that NUMBER is relative to the direction to my ship (The direction to my ship is 0, clockwise).
            /// "absolute" type means that NUMBER is the absolute value
            ///   (12 o'clock is 0, clockwise).
            /// "relative" type means that NUMBER is relative to the direction of this bullet 
            ///   (0 means that the direction of this fire and the direction of the bullet are the same).
            /// "sequence" type means that NUMBER is relative to the direction of the previous fire
            ///   (0 means that the direction of this fire and the direction of the previous fire are the same).
            /// </summary>
            [XmlAttribute(AttributeName = "type")]
            public string Type;
            [XmlText]
            public string _expression;

            public float Angle(float[] parameters)
            {
                return Evaluator.Calculate(_expression, parameters);
            }

            Direction()
            {
                Type = AIM;
                _expression = "0";
            }
        }

        [Serializable]
        public class Speed : ValueNode
        {
            public const string ABSOLUTE = "absolute";
            public const string RELATIVE = "relative";
            public const string SEQUENCE = "sequence";
            /// <summary>
            /// In case of the type is "relative", if this element is included in changeSpeed element, the speed is relative to the current speed of this bullet. 
            ///   If not, the speed is relative to the speed of this bullet.
            /// In case of the type is "sequence", if this element is included in changeSpeed element, the speed is changing successively.
            ///   If not, the speed is relative to the speed of the previous fire.
            /// </summary>
            [XmlAttribute(AttributeName = "type")]
            public string Type;
            [XmlText]
            public string _expression;

            public float Rate(float[] parameters)
            {
                return Evaluator.Calculate(_expression, parameters);
            }

            Speed()
            {
                Type = ABSOLUTE;
                _expression = "0";
            }
        }

        [Serializable]
        public class Term : ValueNode
        {
            [XmlText]
            public string _expression;

            public float Frames(float[] parameters)
            {
                return Evaluator.Calculate(_expression, parameters);
            }

            Term()
            {
                _expression = "0";
            }
        }

        public class Param : ValueNode
        {
            [XmlText]
            public string Expression;

            public float GetValue(float[] parameters)
            {
                return Evaluator.Calculate(Expression, parameters);
            }
        }

        /// <summary>
        /// Simple string evaluator that properties and params use.
        /// </summary>
        class Evaluator
        {
            private static Random rand = new Random();

            public static float Calculate(string exp, float[] parameters)
            {
                
                // replace randoms with values
                exp = Regex.Replace(exp, @"\$rand", m => rand.NextDouble().ToString());
                // replace rank with difficulty
                exp = Regex.Replace(exp, @"\$rank", GameManager.GameDifficulty().ToString());

                for (int i = 0; i < parameters.Length; i++)
                {
                    string pattern = "\\$" + (i + 1).ToString();
                    exp = Regex.Replace(exp, pattern, parameters[i].ToString());
                }

                DataTable dt = new DataTable();
                return float.Parse(dt.Compute(exp, "").ToString());
            }
        }
    }
}
