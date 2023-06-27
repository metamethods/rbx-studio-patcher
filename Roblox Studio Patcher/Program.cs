
using Main;

namespace RobloxStudioPatcher
{
  class Program
  {
    static void Main(string[] args)
    {
      MainProgram mainProgram = new(args);

      Task.Run(mainProgram.Start).Wait();
    }
  }
}