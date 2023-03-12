using System;
using System.IO;
using System.Text;

string input = "";
string output = "";

int count = 0;

foreach (string arg in args)
{
    try
    {
        switch (arg)
        {
            case "--input":
                input = args[count + 1];
                break;
            case "--output":
                output = args[count + 1];
                break;
        }
        count++;
    }
    catch
    {
    }
}

if (args.Length > 4 || string.IsNullOrEmpty(input) || string.IsNullOrEmpty(output))
{
    Console.WriteLine("USAGE: \n\n logo-xiaomi-csharp --input <path/to/logo.img> --output <output-folder>");
    Environment.Exit(0);
}

Extract(input, output);

void Extract(string inputFile, string outputFolder)
{
    Directory.CreateDirectory(outputFolder);

    byte[] header = Encoding.ASCII.GetBytes("LOGO!!!!");
    byte[] bmpHeader = Encoding.ASCII.GetBytes("BM");

    using (FileStream inputLogo = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
    using (BinaryReader binaryReader = new BinaryReader(inputLogo))
    {
        inputLogo.Seek(0x4000, SeekOrigin.Begin);
        byte[] headerRead = BitConverter.GetBytes(binaryReader.ReadUInt64());

        if (headerRead.SequenceEqual(header))
        {
            Console.WriteLine("This is a valid logo image!\n");
            uint imageOffset = 0x0;
            List<uint> imageOffsets = new();

            inputLogo.Seek(0x4008, SeekOrigin.Begin);


            byte[] buffer = new byte[2];
            int offset = 0;
            while (inputLogo.Read(buffer, 0, 2) > 0)
            {
                uint lastOffset = Convert.ToUInt32(offset);

                if (buffer.SequenceEqual(bmpHeader))
                {
                    imageOffset = lastOffset + 0x4008;
                    Console.WriteLine("BMP found at offset: " + imageOffset);
                    imageOffsets.Add(imageOffset);
                }
                offset += 2;
                inputLogo.Seek(offset + 0x4008, SeekOrigin.Begin);
            }

            Console.WriteLine("\nExtraction has started\n");

            int count = 0;

            foreach (uint imageOffsetInFile in imageOffsets)
            {
                inputLogo.Seek(imageOffsetInFile, SeekOrigin.Begin);
                byte[] fileSizeHex = new byte[6];
                inputLogo.Read(fileSizeHex, 0, 6);

                List<byte> bytes = new List<byte>(fileSizeHex); // This isn't ideal but I really don't know my way around
                bytes.RemoveAt(0);
                bytes.RemoveAt(1);

                fileSizeHex = bytes.ToArray();

                int fileSizeInt = BitConverter.ToInt32(fileSizeHex);

                Console.WriteLine("Target file size: " + fileSizeInt / 1024 / 1024 + "MB");

                inputLogo.Seek(imageOffsetInFile, SeekOrigin.Begin);

                byte[] imageFile = new byte[fileSizeInt];

                inputLogo.Read(imageFile, 0, fileSizeInt);

                File.WriteAllBytes($"{outputFolder}/logo-{count}.bmp", imageFile);

                count++;
            }
        }
        else
        {
            Console.WriteLine("This isn't a valid logo image");
            Console.WriteLine("Your logo header: " + Encoding.ASCII.GetString(headerRead));
            Console.WriteLine("Expected logo header: " + Encoding.ASCII.GetString(header));
            Environment.Exit(0);
        }
    }

}