namespace MonC
{
    public struct FileLocation
    {
        public uint Line;
        public uint Column;

        public override string ToString()
        {
            return $"FileLocation(Line = {Line}, Column = {Column})";
        }
    }
}