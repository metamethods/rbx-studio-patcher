using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Net.Http;

using CommandLine;

namespace Main
{
  class MainProgram
  {
    public HttpClient client = new();
    public string[] Args;
    public string? RbxVersion = "";

    public MainProgram(string[] args)
    {
      Args = args;
    }
    public static bool IsAdmin()
    {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        return new WindowsPrincipal(WindowsIdentity.GetCurrent())
          .IsInRole(WindowsBuiltInRole.Administrator);

      return false;
    }

    private static byte[] ParseHexString(string hexString)
    {
      hexString = hexString.Replace(" ", "");
      int length = hexString.Length;
      byte[] bytes = new byte[length / 2];

      for (int i = 0; i < length; i += 2)
      {
        bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
      }

      return bytes;
    }

    private static int FindSequenceIndex(byte[] source, byte[] sequence)
    {
      for (int i = 0; i < source.Length - sequence.Length + 1; i++)
      {
        if (source.Skip(i).Take(sequence.Length).SequenceEqual(sequence))
        {
          return i;
        }
      }
      return -1;
    }

    public static void ReplaceHexSequenceInFile(string filePath, string outputFile, string findHex, string replaceHex)
    {
      // Copy the file to the output file
      File.Copy(filePath, outputFile, true);

      byte[] findBytes = ParseHexString(findHex);
      byte[] replaceBytes = ParseHexString(replaceHex);

      using FileStream fileStream = new(outputFile, FileMode.Open, FileAccess.ReadWrite);
      using BinaryReader reader = new(fileStream);
      using BinaryWriter writer = new(fileStream);

      byte[] buffer = new byte[4096]; // Chunk size, adjust as needed
      int bytesRead;
      int offset = 0;

      while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
      {
        int index = FindSequenceIndex(buffer, findBytes);
        if (index >= 0)
        {
          fileStream.Position = offset + index;
          writer.Write(replaceBytes);
        }

        offset += bytesRead;
      }
    }

    private async Task<string?> GetVersion()
    {
      var response = await client.GetStringAsync("http://s3.amazonaws.com/setup.roblox.com/versionQTStudio");

      return response;
    }

    private void SetupCLI()
    {
      Interface commandLineInterface = new();

      commandLineInterface.AddCommand("patch", "Patches roblox studio", (args) =>
      {
        if (!IsAdmin())
        {
          Console.WriteLine("You must run this program as administrator to patch roblox studio.");
          Environment.Exit(1);
        }

        var version = args.GetArg("version") ?? RbxVersion ?? "";
        var path = args.GetArg("path") ?? Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox", "Versions");
        var output = args.GetArg("output") ?? "RobloxStudioBeta_internal.exe";

        var studioFolder = Path.Combine(path, version);

        if (!Directory.Exists(studioFolder))
        {
          Console.WriteLine($"Roblox studio version {version} does not exist.");
          Console.WriteLine("Maybe try running the program with/without the --version argument?, and or check if you have roblox studio installed and point the --path argument to the roblox studio versions folder.");
          Environment.Exit(1);
        }

        var studioBetaFile = Path.Combine(studioFolder, "RobloxStudioBeta.exe");
        var studioBetaInsiderOutputFile = Path.Combine(studioFolder, output);

        Console.WriteLine($"Patching roblox studio version {version}...");

        ReplaceHexSequenceInFile(
          studioBetaFile, studioBetaInsiderOutputFile, 
          "CC 83 CB 04 89 5E 64 4C", "CC 83 CB 05 89 5E 64 4C"
        );

        Console.WriteLine($"Patched roblox studio version {version}.");
        Console.WriteLine($"Find the file at {studioBetaInsiderOutputFile}.");
      })
      .AddArgument("--path", "Path to roblox studio versions folder", "-p")
      .AddArgument("--output", "File output name", "-o")
      .AddArgument("--version", "Version to patch", "-vr");

      commandLineInterface.Initalize(Args);
    }

    public async Task Start()
    {
      Console.WriteLine("Getting roblox studio version...");
      RbxVersion = await GetVersion();

      if (RbxVersion == null)
      {
        Console.WriteLine("Failed to get roblox studio version.");
        Environment.Exit(1);
      }

      SetupCLI();
    }
  }
}