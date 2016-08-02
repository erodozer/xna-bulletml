using System;
using System.Collections.Generic;

namespace github.io.nhydock.BulletML
{
    public class GameManager
    {
        public static int GameDifficulty()
        {
            return 0;
        }
    }

    namespace Specification
    {
        using System.Xml.Serialization;
        
        public class TaskNode { }

        public interface BulletMLNode { }

        [Serializable()]
        public class Reference<T> : TaskNode where T : BulletMLNode
        {
            [XmlAttribute(AttributeName = "label")]
            [XmlText]
            public string Label;

            [XmlElement("param", typeof(Param))]
            public Param[] Parameters;

            public float[] GetParams(float[] param)
            {
                float[] eval = new float[Parameters.Length];
                for (int i = 0; i < eval.Length; i++)
                {
                    eval[i] = Parameters[i].GetValue(param);
                }
                return eval;
            }
        }
    }

    namespace Implementation
    {
        using Specification;

        public abstract class Step
        {
            public TaskNode Node;
            public float[] ParamList;

            public Step(TaskNode node, float[] Parameters)
            {
                Node = node;
                ParamList = Parameters;
            }

            abstract protected bool IsDone();
            public bool Done
            {
                get { return IsDone(); }
            }

            virtual public void Reset() { }
            virtual public void UpdateParameters(float[] Parameters) {
                ParamList = Parameters;
            }
        }
    }
}
