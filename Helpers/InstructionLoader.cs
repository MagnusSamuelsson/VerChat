namespace VericateChat.Helpers
{
    public static class InstructionLoader
    {
        private static readonly string _instructions;

        static InstructionLoader()
        {
            _instructions = File.ReadAllText("Instructions/modelinstructions.txt");
        }

        public static string GetInstructions()
        {
            return _instructions;
        }
    }
}
