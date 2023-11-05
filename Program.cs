using System;
using System.IO;
using ImageMagick;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;



string currentDirectory = Directory.GetCurrentDirectory();

string origDirStr = Path.Combine(currentDirectory, @"original");
string reduDirStr = Path.Combine(currentDirectory, @"reduced");
string tmpDir = Path.Combine(currentDirectory, @"tmp");


int maxSizeMB = (int)Math.Round(0.95 * 25);
int maxSizeByte = maxSizeMB * 1024 * 1024;

string[] admitedExtensions = {"jpeg","jpg","bmp", "heic"};

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
const string tsFormat = "yyyyMMdd.HHmmss";
const string targetExtensions = "jpeg";

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

if (Directory.Exists(tmpDir))
{
    Directory.Delete(tmpDir, true);
}
Directory.CreateDirectory(tmpDir);



string[] files = Directory.GetFiles(origDirStr);

foreach (string filePath in files)
{
    var filesToDelete = new List<string>();

    //--------------------------------------------------------------------
    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
    string fileExtension = Path.GetExtension(filePath).Substring(1).Replace("jpg", "jpeg");

    if (!admitedExtensions.Contains(fileExtension.ToLower()))
    {
        Console.WriteLine(" ".PadLeft(spaces) + $"extensions {fileExtension} not supported");
        continue;
    }

    string destinationFilePath = String.Empty;
    //--------------------------------------------------------------------
    var imgPath = Path.Combine(tmpDir, Path.GetFileName(filePath));

    File.Copy(filePath, imgPath, true); // Set the third parameter to 'true' to overwrite if the file already exists

    FileInfo fileInfo = new FileInfo(imgPath);
    var sizeMB = fileInfo.Length / 1024 / 1024;
    Console.WriteLine($"{imgPath} - {sizeMB} MB");

    //--------------------------------------------------------------------
    //converting to jpeg

    if (fileExtension.ToLower() != targetExtensions)
    {

        try
        {
            Console.WriteLine(" ".PadLeft(spaces) + $"{DateTime.Now.ToString(tsFormat)} converting {fileExtension} to {targetExtensions} ...");

            using (MagickImage image = new MagickImage(imgPath))
            {
                image.Format = MagickFormat.Jpeg;
                imgPath = Path.ChangeExtension(imgPath, $".{targetExtensions}");
                image.Write(imgPath);
            }

            Console.WriteLine(" ".PadLeft(spaces) + $"{DateTime.Now.ToString(tsFormat)} conversion done");

        }
        catch (Exception ex)
        {
            Console.WriteLine(" ".PadLeft(spaces) + $"-----------------------------------------------");
            Console.WriteLine(" ".PadLeft(spaces) + $"{ex.Message}");
            Console.WriteLine(" ".PadLeft(spaces) + $"-----------------------------------------------");
            continue;
        }

        filesToDelete.Add(imgPath);
    }

    //--------------------------------------------------------------------
    // reduction
    try
    {
        int tryCount = 0;

        using (var image = Image.Load(imgPath))
        {

            Console.WriteLine(" ".PadLeft(spaces) + $"{DateTime.Now.ToString(tsFormat)} start reducing...");

            var redPercent = 0.2d;

            // reduce image quality until reach target size
            while (GetFileSize(image) > maxSizeByte)
            {
                int newWidth = (int)Math.Round(image.Width / (1 + redPercent));
                int newHeight = (int)Math.Round(image.Height / (1 + redPercent));
                tryCount++;

                Console.WriteLine(" ".PadLeft(spaces) + $"{DateTime.Now.ToString(tsFormat)} try {tryCount.ToString().PadLeft(3, '0')} : width: ${newHeight}, height: {newHeight}");

                image.Mutate(x => x
                    .Resize(new ResizeOptions
                    {
                        Size = new Size(newWidth, newHeight),
                        Mode = ResizeMode.Max
                    })
                    );

            }
            Console.WriteLine(" ".PadLeft(spaces) + $"{DateTime.Now.ToString(tsFormat)} reduction finished");

            imgPath = Path.Combine(tmpDir, $"{fileNameWithoutExtension}_reduced.{targetExtensions}");
            image.Save(imgPath);
            Console.WriteLine(" ".PadLeft(spaces) + $"{DateTime.Now.ToString(tsFormat)} image saved in {imgPath}");

            filesToDelete.Add(imgPath);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(" ".PadLeft(spaces) + $"-----------------------------------------------");
        Console.WriteLine(" ".PadLeft(spaces) + $"{ex.Message}");
        Console.WriteLine(" ".PadLeft(spaces) + $"-----------------------------------------------");
        continue;
    }

    //--------------------------------------------------------------------
    // copy to target folder
    try
    {
        destinationFilePath = Path.Combine(reduDirStr, Path.GetFileName(imgPath));
        File.Copy(imgPath, destinationFilePath, true); // Set the third parameter to 'true' to overwrite if the file already exists
    }
    catch (Exception ex)
    {
        Console.WriteLine(" ".PadLeft(spaces) + $"-----------------------------------------------");
        Console.WriteLine(" ".PadLeft(spaces) + $"{ex.Message}");
        Console.WriteLine(" ".PadLeft(spaces) + $"-----------------------------------------------");
        continue;
    }

    // delete tmp files
    foreach (string file in filesToDelete)
    {
        File.Delete(file);
        Console.WriteLine(" ".PadLeft(spaces) + $"{DateTime.Now.ToString(tsFormat)} {file} deleted");
    }

    //end
    Console.WriteLine("");
    Console.WriteLine(" ".PadLeft(spaces) + $"{DateTime.Now.ToString(tsFormat)} image copied to {destinationFilePath}");
    Console.WriteLine("");

}