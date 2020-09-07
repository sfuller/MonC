namespace Driver
{
    public interface ITool : IInput
    {
    }

    public interface IExecutableTool : ITool
    {
        public void Execute();
    }
}