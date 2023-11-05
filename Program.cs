using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;



string currentDirectory = Directory.GetCurrentDirectory();

string origDirStr = Path.Combine(currentDirectory, @"original");
string reduDirStr = Path.Combine(currentDirectory, @"reduced");

int maxSiteMB = (int)Math.Round(0.95 * 25);
int maxSizeByte = maxSiteMB * 1024 * 1024;

string[] admitedExtensions = {"jpeg","jpg","bmp" };

//-----------------------------------------------------------------------------------------------------------
static long GetFileSize(Image image)
{
    using (var stream = new MemoryStream())
    {
        image.Save(stream, new JpegEncoder());
        return stream.Length;
    }
}

const int spaces = 5;
const string tsFormat = "yyyyMMdd-HHmmss";

for (int i=0; i<admitedExtensions.Count(); i++) {
    admitedExtensions[i]= admitedExtensions[i].ToLower();
}


if (!Directory.Exists(origDirStr))
{
    Directory.CreateDirectory(origDirStr);
}
if (Directory.Exists(reduDirStr))
{
    Directory.Delete(reduDirStr, true);
}
Directory.CreateDirectory(reduDirStr);

string[] files = Directory.GetFiles(origDirStr);

foreach (string filePath in files)
{
    FileInfo fileInfo = new FileInfo(filePath);
    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
    string fileExtension = Path.GetExtension(filePath);
    var sizeMB = fileInfo.Length / 1024 / 1024;

    Console.WriteLine($"{filePath.PadRight(150)} {sizeMB} MB");

    if (!admitedExtensions.Contains(fileExtension.ToLower())) {
        Console.WriteLine(" ".PadLeft(spaces) + $"extensions {fileExtension} not supported");
        continue;
    }

    if (fileInfo.Length <= maxSizeByte) {
        Console.WriteLine(" ".PadLeft(spaces) + $"{sizeMB} < {maxSiteMB} nothing do do");

        string destinationFilePath = Path.Combine(reduDirStr, $"{fileNameWithoutExtension}_original.{fileExtension}");
        File.Copy(filePath, destinationFilePath, true); // Set the third parameter to 'true' to overwrite if the file already exists
        continue;
    }


    //reduction
    try
    {
        Console.WriteLine(" ".PadLeft(spaces) + $"{DateTime.Now.ToString(tsFormat)} start reducing...");

        string destinationFilePath = Path.Combine(reduDirStr, $"{fileNameWithoutExtension}_reduced.{fileExtension}");

        int tryNum = 1;

        using (var image = Image.Load(filePath))
        {
            var redPercent = 0.2d;

            // reduce image quality until reach target size
            while (GetFileSize(image) > maxSizeByte)
            {
                int newWidth = (int)Math.Round(image.Width / (1 + redPercent));
                int newHeight = (int)Math.Round(image.Height / (1 + redPercent));

                Console.WriteLine(" ".PadLeft(spaces) + $"{DateTime.Now.ToString(tsFormat)} try {tryNum.ToString().PadLeft(3,'0')} : width: ${newHeight}, height: {newHeight}");

                image.Mutate(x => x
                    .Resize(new ResizeOptions{
                                                    Size = new Size(newWidth , newHeight),
                                                    Mode = ResizeMode.Max
                                             })
                    );
                
                tryNum++;
            }

            Console.WriteLine(" ".PadLeft(spaces) + $"reduction finished");


            image.Save(destinationFilePath);
            Console.WriteLine(" ".PadLeft(spaces) + $"image saved in {destinationFilePath}");

        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(" ".PadLeft(spaces) + $"-----------------------------------------------");
        Console.WriteLine(" ".PadLeft(spaces) + $"{ex.Message}");
        Console.WriteLine(" ".PadLeft(spaces) + $"-----------------------------------------------");
    }

}
