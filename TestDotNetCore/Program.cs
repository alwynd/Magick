// See https://aka.ms/new-console-template for more information

using TestDotNetCore;


Magick magick = new Magick();
magick.Debug = false;

int perc = 10;
long minSize = 1024 * 1024L;
string prev = "";
string folder = "";

if (args.Length > 0)
{
    foreach (var arg in args)
    {
        Console.WriteLine($"arg: {arg}");
        if (arg == "-debug")
        {
            magick.Debug = true;
        }
        if (prev == "-perc")
        {
            perc = int.Parse(arg);
        }
        if (prev == "-minsize")
        {
            minSize = long.Parse(arg);
        }

        if (prev == "-folder") folder = arg;
        prev = arg;
    }
}

Console.WriteLine($"folder: {folder}");
magick.ResizeAllImagesInFolder(folder, perc, minSize);


