namespace TomlConfig.Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var myTest = new MyTest();
            Console.WriteLine($"Guid = {myTest.Guid}");

            myTest.Guid = Guid.NewGuid().ToString().Split('-').Last().ToUpper();
            myTest.Save();

            myTest.Load();
            Console.WriteLine($"Guid = {myTest.Guid}");
            Console.ReadLine();
        }
    }
}
