using System.Text.RegularExpressions;

namespace CommandLine
{
  class Types
  {
    public struct ParsedArgs
    {
      public Dictionary<string, string> Args;
      public List<string> _unassigned_;
      public string command;

      public ParsedArgs()
      {
        Args = new Dictionary<string, string>();
        _unassigned_ = new List<string>();
        command = "";
      }

      public readonly string? GetArg(string arg)
      {
        if (Args.TryGetValue(arg, out string? value))
          return value;

        return null;
      }
    }

    public struct Argument
    {
      public string LongHandArg { get; set; }
      public string? ShortHandArg { get; set;}
      public string Description { get; set; }
    }

    public struct Command
    {
      public string CommandName { get; set;}
      public string Description { get; set; }
      public Action<ParsedArgs> Callback { get; set; }
      public List<Argument> Args { get; set; }
      public List<Argument> RequiredArgs { get; set; }
      public Dictionary<string, object> Metadata { get; set; }

      public Command(
        string name,
        string description,
        Action<ParsedArgs> callback
      )
      {
        CommandName = name;
        Description = description;
        Callback = callback;
        Args = new List<Argument>();
        RequiredArgs = new List<Argument>();
        Metadata = new Dictionary<string, object>();
      }

      public Command AddArgument(
        string longHandArg, 
        string description,
        string? shortHandArg,
        bool required = false
      )
      {
        var argument = new Argument
        {
          LongHandArg = longHandArg,
          ShortHandArg = shortHandArg,
          Description = description
        };

        Args.Add(argument);

        if (required)
          RequiredArgs.Add(argument);

        return this;
      }

      public readonly Dictionary<string, string> GetArgs()
      {
        var args = new Dictionary<string, string>();

        foreach (var arg in Args)
          args.Add(arg.ShortHandArg ?? "", arg.LongHandArg);

        return args;
      }

      public readonly void MergeArgs(List<Argument> args)
      {
        foreach (var arg in args)
          Args.Add(arg);
      }

      public readonly void AddMetadata(
        string key,
        object value
      )
      {
        Metadata.Add(key, value);
      }
    }
  }

  partial class Parser
  {
    public Types.ParsedArgs ParsedArgs = new();

    public Types.ParsedArgs Parse(
      string[] args, 
      Dictionary<string, string>? longHandArgs = null
    )
    {
      // Args is in the format of
      // -arg1 value1 -arg2 value2 --arg3 value3 value4
      // Convert any shorthand args into longhand args. (shorthand args are -arg)
      // We parse through and create an object that looks like this
      // { "arg1": "value1", "arg2": "value2", "arg3": "value3", "_unassigned_": ["value4"] }

      longHandArgs ??= new Dictionary<string, string>();

      var parsedArgs = new Types.ParsedArgs();
      var index = -1;
      string? currentArg = null;

      foreach (string arg in args)
      {
        index++;

        // Check if the arg is a shorthand arg
        // If so then check if it's in the longhand args

        if (currentArg == null && IsArg().IsMatch(arg))
        {
          _ = longHandArgs.TryGetValue(arg, out string? longHandArg);
          
          // if (longHandArg == null)
          // {
          //   currentArg = arg;
          //   continue;
          // }

          // currentArg = longHandArg;
          currentArg = longHandArg ?? arg;

          // Check ahead if the next arg is a shorthand/longhand arg
          // If so then we can assume that the current arg is a boolean arg
          string? nextValue = null;

          try {
            nextValue = args[index + 1];
          } catch {}

          if (
            (nextValue != null && IsArg().IsMatch(args[index + 1])) ||
            (nextValue == null)
          )
          {
            parsedArgs.Args.Add(ArgStrip().Replace(currentArg, ""), "_true_");
            currentArg = null;
          }

          continue;
        }

        if (currentArg != null)
        {
          parsedArgs.Args.Add(ArgStrip().Replace(currentArg, ""), arg);
          currentArg = null;
          continue;
        }

        parsedArgs._unassigned_.Add(arg);
      }

      // Remove the first item from the unassigned list
      // And add it to the command property
      if (parsedArgs._unassigned_.Count > 0)
      {
        parsedArgs.command = parsedArgs._unassigned_[0];
        parsedArgs._unassigned_.RemoveAt(0);
      }

      ParsedArgs = parsedArgs;

      return parsedArgs;
    }

    [GeneratedRegex("^(-{1,2})\\w+")]
    public static partial Regex IsArg();
    [GeneratedRegex("^(-{1,2})")]
    public static partial Regex ArgStrip();
  }
  class Interface
  {
    private readonly List<Types.Command> Commands;
    private readonly List<Types.Argument> GlobalArgs;
    private Types.ParsedArgs ParsedArgs;

    public Interface()
    {
      Commands = new List<Types.Command>();
      GlobalArgs = new List<Types.Argument>();

      AddCommand("", "", (Types.ParsedArgs args) => {
        if (args.GetArg("help") == null) return;

        Console.WriteLine($"Usage: <command> [options] {Environment.NewLine}");
        Console.WriteLine("Global Options:");
        foreach (var arg in GlobalArgs)
        {
          Console.WriteLine($"{$"{arg.LongHandArg}{(arg.ShortHandArg != null ? $"|{arg.ShortHandArg}" : "")}",-20} {arg.Description}");
        }

        Console.WriteLine($"{Environment.NewLine}Commands:");
        foreach (var command in Commands) {
          if (command.Metadata.TryGetValue("hidden", out object? hidden) && (bool)hidden)
            continue;

          Console.WriteLine($"{command.CommandName,-20} {command.Description}");
          
          foreach (var arg in command.Args)
          {
            Console.WriteLine($"{" ",-21}{$"{arg.LongHandArg}{(arg.ShortHandArg != null ? $"|{arg.ShortHandArg}" : "")}"} {arg.Description}");
          }
        }
      }).AddMetadata("hidden", true);

      AddGlobalArg("--help", "Displays this help message", "-h");
    }

    public void Initalize(string[] args)
    {
      var commandName = new Parser().Parse(args).command;

      foreach (var command in Commands)
      {
        if (command.CommandName == commandName)
        {
          var parser = new Parser();
          command.MergeArgs(GlobalArgs);
          ParsedArgs = parser.Parse(args, command.GetArgs());
          
          // Check if all required args are present
          foreach (var requiredArg in command.RequiredArgs)
          {
            if (ParsedArgs.GetArg(Parser.ArgStrip().Replace(requiredArg.LongHandArg, "")) == null)
            {
              Console.WriteLine($"Missing required argument {requiredArg.LongHandArg}");
              Console.ReadKey(true);
              return;
            }
          }

          command.Callback(ParsedArgs);

          return;
        }
      }
    }

    public Types.Command AddCommand(
      string name,
      string description,
      Action<Types.ParsedArgs> callback
    )
    {
      var command = new Types.Command(name, description, callback);

      Commands.Add(command);

      return command;
    }

    public Interface AddGlobalArg(
      string longHandArg,
      string description,
      string? shortHandArg
    )
    {
      GlobalArgs.Add(new Types.Argument
      {
        LongHandArg = longHandArg,
        Description = description,
        ShortHandArg = shortHandArg
      });

      return this;
    }
  }
}