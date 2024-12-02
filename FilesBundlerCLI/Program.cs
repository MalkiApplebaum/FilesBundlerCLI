using System.CommandLine;
using System.IO;
using System.Linq;

var rootCommand = new RootCommand("Root command for File Bundler CLI");

var outputOption = new Option<FileInfo>(new[] { "--output", "-o" }, "File path and name");
var languageOption = new Option<List<string>>(new[] { "--language", "-l" })
{
    IsRequired = true,
    Description = "A list of programming languages (separate with commas). Use 'all' for all files."
};
var noteOption = new Option<bool>(new[] { "--note", "-n" }, "Add notes with file paths and names.");
var sortOption = new Option<bool>(new[] { "--sort", "-s" }, "Sort files before merging.");
var removeEmptyLinesOption = new Option<bool>(new[] { "--remove", "-r" }, "remove empty lines.");
var authorOption = new Option<string>(new[] { "--author", "-a" }, "include author name.");

// פקודת bundle
var bundleCommand = new Command("bundle", "Bundle code files into a single file");

bundleCommand.AddOption(outputOption);
bundleCommand.AddOption(languageOption);
bundleCommand.AddOption(noteOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(removeEmptyLinesOption);
bundleCommand.AddOption(authorOption);


bundleCommand.SetHandler(( output,languages,note, sort, remove, author) =>
{
    try
    {
        if (output != null)
        {
            using var newFile = File.Create(output.FullName);
            using var writer = new StreamWriter(newFile);

            string[] allFiles = Array.Empty<string>();
            if (languages.Contains("all"))
            {
                allFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*", SearchOption.AllDirectories)
                .Where(file=>!file.EndsWith(".rsp",StringComparison.OrdinalIgnoreCase))
                .ToArray();
            }
            else
            {
                foreach (var l in languages)
                {
                    var fileExtensions = l switch
                    {
                        "c#" => new[] { "*.cs" },
                        "java" => new[] { "*.java" },
                        "python" => new[] { "*.py" },
                        "javascript" => new[] { "*.js" },
                        "c++" => new[] { "*.cpp" },
                        "c" => new[] { "*.c" },
                        "html" => new[] { "*.html" },
                        _ => Array.Empty<string>()
                    };

                    foreach (var ext in fileExtensions)
                    {
                        allFiles = allFiles.Concat(Directory.GetFiles(Directory.GetCurrentDirectory(), ext, SearchOption.AllDirectories)).ToArray();
                    }
                }
            }
            if (sort)
            {
                allFiles = allFiles.OrderBy(file => Path.GetExtension(file).ToLower()).ThenBy(file => Path.GetFileName(file).ToLower()).ToArray();
            }
            else
            {
                allFiles = allFiles.OrderBy(file => Path.GetFileName(file).ToLower()).ToArray();
            }
            if(author!=null)
            {
                writer.WriteLine($"//{ author}");
            }
            foreach (var file in allFiles)
            {
                if (file != output.FullName)
                {
                    if (note)
                    {
                        writer.WriteLine($"// Source file: {Path.GetFileName(file)}");
                        writer.WriteLine($"// Relative path: {Path.GetRelativePath(Directory.GetCurrentDirectory(), file)}");
                        writer.WriteLine();
                    }

                    using var reader = new StreamReader(file);
                    string fileContent = reader.ReadToEnd();
                    if(remove)
                    {
                        fileContent=string.Join(
                            Environment.NewLine,
                            fileContent.Split(new[] { Environment.NewLine },StringSplitOptions.None)
                            .Where(line => !string.IsNullOrWhiteSpace(line))
                        );
                    }
                    writer.WriteLine(fileContent);
                    writer.WriteLine();
                }
            }

            Console.WriteLine("File was created successfully.");
        }
        else
        {
            Console.WriteLine("Error: Output file not specified.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}, outputOption, languageOption, noteOption, sortOption, removeEmptyLinesOption, authorOption);

// פקודת create-rsp
var createRspCommand = new Command("create-rsp", "Create a response file with pre-filled command options.");

var rspOutputOption = new Option<FileInfo>(new[] { "--rsp-output","-rsp" }, "Path to save the .rsp response file");
createRspCommand.AddOption(rspOutputOption);

createRspCommand.SetHandler((rspOutput) =>
{
    Console.WriteLine("Please enter the programming languages (comma separated):");
    string languagesInput = Console.ReadLine();
    var languages = languagesInput.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                  .Select(lang => lang.Trim())
                                  .ToList();

    Console.WriteLine("Please enter the output file path:");
    string outputPath = Console.ReadLine();

    Console.WriteLine("Do you want to add a note (yes/no)?");
    bool note = Console.ReadLine()?.ToLower() == "yes";

    Console.WriteLine("Do you want to sort the files (yes/no)?");
    bool sort = Console.ReadLine()?.ToLower() == "yes";

    Console.WriteLine("Do you want to remove empty lines (yes/no)?");
    bool remove = Console.ReadLine()?.ToLower() == "yes";

    Console.WriteLine("Please enter the author name (optional):");
    string author = Console.ReadLine();

    var rspCommand = $"bundle --output \"{outputPath}\" --language {string.Join(",", languages)}" +
                     (note ? " --note" : "") +
                     (sort ? " --sort" : "") +
                     (remove ? " --remove" : "") +
                     (author != null ? $" --author \"{author}\"" : "");

    if (rspOutput != null)
    {
        File.WriteAllText(rspOutput.FullName, rspCommand);
        Console.WriteLine($"Response file created: {rspOutput.FullName}");
    }
    else
    {
        Console.WriteLine("Response file path is invalid.");
    }
},rspOutputOption);

rootCommand.AddCommand(createRspCommand);
rootCommand.AddCommand(bundleCommand);
await rootCommand.InvokeAsync(args);
