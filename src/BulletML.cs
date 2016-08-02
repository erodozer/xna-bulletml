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
        public class TaskNode { }
    }

    namespace Implementation
    {
        public abstract class Step
        {
            abstract protected bool IsDone();
            public bool Done
            {
                get { return IsDone(); }
            }

            abstract public void UpdateParameters(float[] Parameters);
        }
    }
}
