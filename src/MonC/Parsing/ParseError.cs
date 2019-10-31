
namespace MonC
{
    public struct ParseError
    {
        public string Message;
        public FileLocation Start;
        public FileLocation End;
    }
}