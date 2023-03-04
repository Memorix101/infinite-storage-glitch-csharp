using FFMediaToolkit;
using FFMediaToolkit.Decoding;
using FFMediaToolkit.Encoding;
using FFMediaToolkit.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Collections;
using System.IO.Compression;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using ZstdSharp;

namespace infinite_storage_glitch_csharp
{
    public static class Extensions
    {
        public static Image<Bgr24> ToBitmap(this ImageData imageData)
        {
            return Image.LoadPixelData<Bgr24>(imageData.Data, imageData.ImageSize.Width, imageData.ImageSize.Height);
        }
    }

    internal class Program
    {
        static Vector2 res = new Vector2(1280, 720);

        private static byte[] gzip_compress(byte[] input)
        {
            var to = new MemoryStream();
            var gZipStream = new GZipStream(to, CompressionMode.Compress);
            MemoryStream memoryStream = new MemoryStream(input); //file.CopyTo(gZipStream);
            memoryStream.CopyTo(gZipStream);
            gZipStream.Flush();
            return to.ToArray();
        }

        private static byte[] gzip_decompress(byte[] compressed)
        {
            var from = new MemoryStream(compressed);
            var to = new MemoryStream();
            var gZipStream = new GZipStream(from, CompressionMode.Decompress);
            gZipStream.CopyTo(to);
            return to.ToArray();
        }

        /*
        // https://houseofcat.io/guides/csharp/net/compression
        static byte[] gzip_compress(byte[] data)
        {
            using var compressedStream = new MemoryStream();
            using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, false))
            {
                gzipStream.Write(data);
            }

            return compressedStream.ToArray();
        }

        static byte[] gzip_decompress(byte[] compressedData)
        {
            using var uncompressedStream = new MemoryStream();

            using (var compressedStream = new MemoryStream(compressedData))
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, false))
            {
                gzipStream.CopyTo(uncompressedStream);
            }

            return uncompressedStream.ToArray();
        }
        */

        static byte[] zstd_compress(byte[] ms, int level)
        {
            Console.Write("\nCompressing data stream\n");
            MemoryStream dataStream = new MemoryStream(ms);
            var resultStream = new MemoryStream();
            using (var compressionStream = new CompressionStream(resultStream, level))
                dataStream.CopyTo(compressionStream);
            return resultStream.ToArray();
        }

        static byte[] zstd_decompress(byte[] ms, int level)
        {
            Console.Write("\nDecompressing data stream\n");
            MemoryStream input = new MemoryStream(ms);
            var resultStream = new MemoryStream();
            using (var decompressionStream = new DecompressionStream(input, level))
                decompressionStream.CopyTo(resultStream);
            return resultStream.ToArray();
        }

        static void WriteCustomHeader(string path, uint eof)
        {
            Console.WriteLine("Embedding custom header");
            FileInfo _fi = new FileInfo(path);
            string _ext = _fi.Extension;
            string _name = _fi.Name;
            Stream fileStream = File.Open("./out.gif", FileMode.OpenOrCreate);
            fileStream.Position = 64;
            //fileStream.Seek(16, SeekOrigin.Begin);
            fileStream.Write(Encoding.ASCII.GetBytes(_name), 0, Encoding.ASCII.GetBytes(_name).Length);
            fileStream.Position = 64 + 256;
            fileStream.Write(BitConverter.GetBytes(eof));
            //fileStream.Seek(32, SeekOrigin.Begin);
            //fileStream.Position = 32;
            //fileStream.Write(Encoding.ASCII.GetBytes(_ext), 0, Encoding.ASCII.GetBytes(_ext).Length);
            //var ms = new MemoryStream();
            //fileStream.CopyTo(ms);
            //ms.Close();
            //File.WriteAllBytes("./out.gif", ms.ToArray());
            fileStream.Close();
        }

        static void TamperHeader(string path, uint eof)
        {
            Console.WriteLine("Tampering with header");
            FileInfo _fi = new FileInfo(path);
            string _ext = _fi.Extension;
            string _name = _fi.Name;
            Stream fileStream = File.Open("./out.gif", FileMode.OpenOrCreate);
            fileStream.Position = 784;
            fileStream.Write(BitConverter.GetBytes(eof));
            fileStream.Close();
        }

        static byte[] ReadTamperHeader(byte[] _in)
        {
            Console.WriteLine("Reading tamper header");
            Stream fileStream = File.Open("./out.gif", FileMode.OpenOrCreate);
            MemoryStream ms = new MemoryStream(_in);
            fileStream.Position = 784;
            BinaryReader binaryReader = new BinaryReader(fileStream);
            uint eof = binaryReader.ReadUInt32();
            //fileStream.CopyTo(ms);
            //var a = zstd_decompress(ms).ToArray();
            ms.SetLength(eof);
            //ms.Close();
            //File.WriteAllBytes("./out.gif", ms.ToArray());
            fileStream.Close();
            return ms.ToArray();
        }

        static (string, byte[]) ReadCustomHeader(byte[] _in)
        {
            Console.WriteLine("Reading custom header");
            Stream fileStream = File.Open("./out.gif", FileMode.OpenOrCreate);
            MemoryStream ms = new MemoryStream(_in);
            fileStream.Position = 64;
            BinaryReader binaryReader = new BinaryReader(fileStream);
            string outFileName = string.Empty;
            while (true)
            {
                var _ = binaryReader.ReadChar();
                if (_ == '\0') { break; }
                outFileName += _;
            }
            fileStream.Position = 64 + 256;
            uint eof = binaryReader.ReadUInt32();
            //fileStream.CopyTo(ms);
            //var a = zstd_decompress(ms).ToArray();
            ms.SetLength(eof);
            //ms.Close();
            //File.WriteAllBytes("./out.gif", ms.ToArray());
            fileStream.Close();
            return (outFileName, ms.ToArray());
        }
        static byte[] BitArrayToByteArray(BitArray bits) // https://stackoverflow.com/a/4619295
        {
            byte[] ret = new byte[(bits.Length - 1) / 8 + 1];
            bits.CopyTo(ret, 0);
            return ret;
        }

        enum OutputMode
        {
            GIF = 0,
            JPEG = 1,
            Video = 2
        }

        static void WriteFile(string path, OutputMode outputMode)
        {
            Console.Write($"\n>> Writing file: {path}");
            byte[] fileBytes = File.ReadAllBytes(path);
            //MemoryStream fileStream = new MemoryStream(fileBytes);
            //FileStream fileStream = new FileStream(path, FileMode.OpenOrCreate);

            // zstd compression
            var compressed = zstd_compress(fileBytes, 3);
            MemoryStream fileStream = new MemoryStream(compressed);

            BinaryReader binReader = new BinaryReader(fileStream);
            var files = Directory.GetFiles("./", "out*.*");
            if (files.Length > 0) { Console.WriteLine("Clearing all old output files"); }
            foreach (var f in files) { File.Delete(f); }
            fileStream.Position = 0; // reset
            using (Image<Rgba32> image = new Image<Rgba32>((int)res.X, (int)res.Y))
            {
                int o = 0;
                BitArray bits = new BitArray(0);
                bool eol = false;
                long currentRow = fileStream.Position;
                int whitePixs = 0;
                int blackPixs = 0;
                Image<Rgba32> gif = new Image<Rgba32>((int)res.X, (int)res.Y);
                while (fileStream.Position < fileStream.Length)
                {
                    int b = 8;
                    float fileSplits = fileStream.Length / (res.X * res.Y) * 8.0f * 16.0f; // 8 bity = 1 byte * 4x4 (16) pixels = 1 pixel block
                    Console.Write($"\rGenerating output image {o + 1}/{(int)MathF.Ceiling(fileSplits)}");
                    //Console.Write($"\rGenerating output image {((float)fileStream.Position / (float)fileStream.Length) / 100.0f}%");
                    currentRow = fileStream.Position;
                    for (int h = 0; h < res.Y; h += 4)
                    {
                        //Console.WriteLine($"h: {h}");
                        for (int w = 0; w < res.X; w += 4)
                        {
                            if (fileStream.Position <= fileStream.Length && !eol)
                            {
                                if (b > bits.Length - 1)
                                {
                                    if (fileStream.Position < fileStream.Length)
                                    {
                                        byte bytes = binReader.ReadByte();
                                        bits = new BitArray(new byte[] { bytes });
                                        b = 0;
                                    }
                                    else
                                    {
                                        eol = true;
                                        Console.Write($"\nEOF Position: {fileStream.Position - 1}");
                                        //image[w, h] = Color.Blue;
                                        //image[w, h] = new Rgba32(10, 0, 10, 255);
                                        IBrush brush = Brushes.Solid(Color.Blue);
                                        IPath rect = new RectangularPolygon(w, h, 4, 4);
                                        IPen pen = Pens.Solid(Color.Blue, 1);
                                        image.Mutate(x => x.Fill(brush, rect).Draw(pen, rect));
                                        //break;
                                    }
                                }
                                if (!eol) //if (fileStream.Position <= fileStream.Length)
                                {
                                    if (bits[b] == false)
                                    {
                                        //image[w, h] = Color.Black; // also works on ImageFrame<T>
                                        IBrush brush = Brushes.Solid(Color.Black);
                                        IPath rect = new RectangularPolygon(w, h, 4, 4);
                                        IPen pen = Pens.Solid(Color.Black, 1);
                                        image.Mutate(x => x.Fill(brush, rect).Draw(pen, rect));
                                        blackPixs++;

                                    }
                                    else
                                    {

                                        //image[w, h] = Color.White; // also works on ImageFrame<T>
                                        IBrush brush = Brushes.Solid(Color.White);
                                        IPath rect = new RectangularPolygon(w, h, 4, 4);
                                        IPen pen = Pens.Solid(Color.White, 1);
                                        image.Mutate(x => x.Fill(brush, rect).Draw(pen, rect));
                                        whitePixs++;
                                    }
                                    b++;
                                }
                            }
                            else
                            {
                                //Console.WriteLine($"End of filestream. Filling with null");
                                /*if (!eol)
                                {
                                    eol = true;
                                    Console.Write($"\nEOF Position: {fileStream.Position - 1}");
                                    //image[w, h] = Color.Blue;
                                    //image[w, h] = new Rgba32(10, 0, 10, 255);
                                    IBrush brush = Brushes.Solid(Color.Blue);
                                    IPath rect = new RectangularPolygon(w, h, 4, 4);
                                    image.Mutate(x => x.Fill(brush, rect).Draw(Color.Blue, 1, rect));
                                }
                                else
                                {*/
                                /*if (OutputMode.Video == outputMode)
                                {
                                    //image[w, h] = Color.Blue;
                                    IBrush brush = Brushes.Solid(Color.Blue);
                                    IPath rect = new RectangularPolygon(w, h, 4, 4);
                                    IPen pen = Pens.Solid(Color.Blue, 1);
                                    image.Mutate(x => x.Fill(brush, rect).Draw(pen, rect));
                                }
                                else
                                {*/
                                    //image[w, h] = Color.Black; 
                                    IBrush brush = Brushes.Solid(Color.Black);
                                    IPath rect = new RectangularPolygon(w, h, 4, 4);
                                    IPen pen = Pens.Solid(Color.Black, 1);
                                    image.Mutate(x => x.Fill(brush, rect).Draw(pen, rect));
                                //} // also works on ImageFrame<T>
                                //}
                            }
                        }
                    }

                    if (outputMode == OutputMode.GIF || outputMode == OutputMode.Video)
                    {
                        var metadata = gif.Frames.RootFrame.Metadata.GetGifMetadata();
                        metadata.FrameDelay = 0;
                        var gifMetaData = gif.Metadata.GetGifMetadata();
                        gifMetaData.RepeatCount = 0;
                        gifMetaData.ColorTableMode = GifColorTableMode.Local;
                        gif.Frames.AddFrame(image.Frames.RootFrame);
                    }
                    else if (outputMode == OutputMode.JPEG)
                    {
                        using (var ms = new MemoryStream())
                        {
                            JpegEncoder encoder = new JpegEncoder();
                            encoder.Quality = 100;
                            encoder.ColorType = JpegColorType.Rgb;
                            image.Save(ms, encoder);
                            //ms.Position = memoryStream.Position;
                            //ms.Write(Encoding.ASCII.GetBytes("JUN!"));
                            //ms.SetLength(memoryStream.Position);
                            // image.SaveAsJpeg($"./out{o}.jpg", encoder);
                            File.WriteAllBytes($"./out{o}.jpg", ms.ToArray());
                        }
                    }
                    o++;
                }
                if (outputMode == OutputMode.GIF)
                {
                    Console.Write("\nGenerating GIF");
                    using (var ms = new MemoryStream())
                    {
                        GifEncoder encoder = new GifEncoder();
                        gif.Save(ms, encoder);
                        //ms.Position = memoryStream.Position;
                        //ms.Write(Encoding.ASCII.GetBytes("JUN!"));
                        //ms.SetLength(memoryStream.Position);
                        File.WriteAllBytes($"./out.gif", ms.ToArray());
                    }
                    //image.SaveAsGif($"./out.gif");
                    //WriteCustomHeader(path, (uint)memoryStream.Position); //uint32 eof
                    //TamperHeader(path, (uint)memoryStream.Position); //uint32 eof
                }
                else if (outputMode == OutputMode.Video)
                {
                    var settings = new VideoEncoderSettings(width: gif.Width, height: gif.Height, framerate: 10, codec: VideoCodec.VP9); //H264
                    settings.EncoderPreset = EncoderPreset.UltraFast;
                    settings.CRF = 0; // we need every frame!
                    settings.VideoFormat = ImagePixelFormat.Yuv444;
                    using (var video = MediaBuilder.CreateContainer($"{AppDomain.CurrentDomain.BaseDirectory}/out.mp4").WithVideo(settings).Create())
                    {
                        //MemoryStream ms = new MemoryStream();
                        //image.SaveAsBmp(ms);
                        //Image<Bgr24> bmpImg = Image.Load<Bgr24>(ms.ToArray());
                        Console.Write($"\n");
                        for (int b = 0; b < gif.Frames.Count + 1; b++)
                        {
                            if (b == 0) { continue; } // ImageSharp puts the first frame last 
                            byte[] pixelBytes = new byte[gif.Width * gif.Height * Unsafe.SizeOf<Rgba32>()];
                            if (b == gif.Frames.Count) { gif.Frames[7].CopyPixelDataTo(pixelBytes); } else { gif.Frames[b].CopyPixelDataTo(pixelBytes); }
                            ImageData imageData = new ImageData(pixelBytes, ImagePixelFormat.Rgba32, gif.Width, gif.Height);
                            video.Video.AddFrame(imageData);
                            Console.Write($"\rEncoding MP4 {(int)(((float)b / (float)gif.Frames.Count) * 100f)}%");
                        }
                        Console.Write($"\rEncoding MP4 100%");
                    }
                }
                Console.Write($"\nOut White pixels: {whitePixs}");
                Console.Write($"\nOut Black pixels: {blackPixs}");
            }
        }

        static byte[] GenExportGif(string path, ref bool eof_ref, ref int eof_end_bits_ref)
        {
            bool eof = false;
            int eof_end_bits = 0;
            int undefined_pixels = 0;
            Image<Rgba32> image = Image.Load<Rgba32>($"{path}");

            image.Frames.RemoveFrame(0); // https://stackoverflow.com/a/56781434 
                                         //image.Frames.RemoveFrame(image.Frames.Count - 1); // this is a test. remove if it breaks something
                                         //Console.Write($"\n");
            int whitePixs = 0;
            int blackPixs = 0;
            Console.Write($"\n");
            long eof_pos = 0;
            long byte_counter = 0;
            byte[] ba = new byte[1280 * 720 * image.Frames.Count + 1];
            BitArray bits = new BitArray(ba.Length);
            int b = 0;
            for (int i = 0; i < image.Frames.Count; i++)
            {
                //Console.WriteLine($"Reading input image {i+1}/{files.Length}");
                Console.Write($"\rReading input image {i + 1}/{image.Frames.Count}");
                //Image<Rgba32> image = Image.Load<Rgba32>($"./out{i}.jpg");
                ImageFrame<Rgba32> u = image.Frames[i];
                int width = image.Width;
                int height = image.Height;
                Rgba32[] pixelArray = new Rgba32[width * height];
                image.Frames[i].CopyPixelDataTo(pixelArray);
                int tolerance = 200;
                for (int x = 0; x < pixelArray.Length; x += 4)
                {
                    var offset = image.Width * 0.4f;
                    if (x % image.Width == 0 && x != 0)
                    {
                        x += (3 * image.Width);
                        if (x >= pixelArray.Length) { break; }
                    } //4*width - 1
                    if (pixelArray[x].R <= tolerance && pixelArray[x].B >= tolerance)// blue
                                                                                     //if (pixel.R != 0 && pixel.G == 0 && pixel.B != 0)// gray
                    {
                        eof = true;
                        //bits.CopyTo(ba, 0);
                        eof_pos = byte_counter - 1;
                        eof_end_bits++;
                        //bits.Set(b, false);
                        // break;
                    }
                    else if (pixelArray[x].R >= tolerance && pixelArray[x].B >= tolerance) // white
                    {
                        //if (!eof) bits.Set(b, true); else eof_end_bits++; ;
                        bits.Set(b, true);
                        whitePixs++;
                        byte_counter++;
                    }
                    else if (pixelArray[x].R <= tolerance && pixelArray[x].B <= tolerance)// black
                    {
                        if (!eof) { blackPixs++; } else eof_end_bits++;
                        bits.Set(b, false);
                        byte_counter++;
                    }
                    else
                    {
                        undefined_pixels++;
                    }
                    b++;
                    /*if (b / 8 == 7200)
                    {
                        Console.Write("\nPeep!");
                    }*/
                }
                bits.CopyTo(ba, 0);
                //byteList.Add(BitArrayToByteArray(bits));
                //if (eof) break;
            }
            Console.Write($"\nUndefined pixels: {undefined_pixels}");
            Console.Write($"\nIn White pixels: {whitePixs}");
            Console.Write($"\nIn Black pixels: {blackPixs}");
            var eof_s = eof ? "EOF found" : "WARNING: EOF NOT FOUND! FILE MAY BE CORUPTED!";
            Console.Write($"\n{eof_s} - {eof_pos / 8}");
            eof_end_bits_ref = (int)eof_pos + 8; //eof_end_bits * 16;
            eof_ref = eof;
            return ba;
        }

        static byte[] GenExportVideo(string path, ref bool eof_ref, ref int eof_end_bits_ref)
        {
            bool eof = false;
            int eof_end_bits = 0;
            int undefined_pixels = 0;

            var file = MediaFile.Open(path);

            int whitePixs = 0;
            int blackPixs = 0;
            Console.Write($"\n");
            long eof_pos = 0;
            long byte_counter = 0;
            byte[] ba = new byte[1280 * 720 * (int)file.Video.Info.NumberOfFrames + 1];
            BitArray bits = new BitArray(ba.Length);
            int b = 0;
            Console.Write($"\n");
            for (int i = 0; i < file.Video.Info.NumberOfFrames; i++)
            {
                Console.Write($"\rReading input image {i + 1}/{file.Video.Info.NumberOfFrames}");
                //Console.Write($"\rReading input image {i + 1}/{image.Frames.Count}");
                //ImageData imageData = new ImageData(null, ImagePixelFormat.Bgr24, 1280, 720);
                byte[] pixelBytes = new byte[1280 * 720 * Unsafe.SizeOf<Bgr24>()];
                file.Video.TryGetNextFrame(pixelBytes);
                Image<Bgr24> image = Image.LoadPixelData<Bgr24>(pixelBytes, 1280, 720);//imageData.ToBitmap();                
                //ImageFrame<Rgba32> u = image.Frames[i];
                int width = image.Width;
                int height = image.Height;
                Bgr24[] pixelArray = new Bgr24[width * height];
                image.CopyPixelDataTo(pixelArray);
                int tolerance = 200; // default 200
                for (int x = 0; x < pixelArray.Length; x += 4)
                {
                    var offset = image.Width * 0.4f;
                    if (x % image.Width == 0 && x != 0)
                    {
                        x += (3 * image.Width);
                        if (x >= pixelArray.Length) { break; }
                    } //4*width - 1
                    if (pixelArray[x].R <= tolerance && pixelArray[x].B >= tolerance)// blue
                                                                                     //if (pixel.R != 0 && pixel.G == 0 && pixel.B != 0)// gray
                    {
                        eof = true;
                        //bits.CopyTo(ba, 0);
                        eof_pos = byte_counter - 1;
                        eof_end_bits++;
                        //bits.Set(b, false);
                        // break;
                    }
                    else if (pixelArray[x].R >= tolerance && pixelArray[x].B >= tolerance) // white
                    {
                        //if (!eof) bits.Set(b, true); else eof_end_bits++; ;
                        bits.Set(b, true);
                        whitePixs++;
                        byte_counter++;
                    }
                    else if (pixelArray[x].R <= tolerance && pixelArray[x].B <= tolerance)// black
                    {
                        if (!eof) { blackPixs++; } else eof_end_bits++;
                        bits.Set(b, false);
                        byte_counter++;
                    }
                    else
                    {
                        undefined_pixels++;
                    }
                    b++;
                    /*if (b / 8 == 7200)
                    {
                        Console.Write("\nPeep!");
                    }*/
                }
                bits.CopyTo(ba, 0);
                //if (eof) break;
            }
            Console.Write($"\nUndefined pixels: {undefined_pixels}");
            Console.Write($"\nIn White pixels: {whitePixs}");
            Console.Write($"\nIn Black pixels: {blackPixs}");
            var eof_s = eof ? "EOF found" : "WARNING: EOF NOT FOUND! FILE MAY BE CORUPTED!";
            Console.Write($"\n{eof_s} - {eof_pos / 8}");
            eof_end_bits_ref = (int)eof_pos + 8; //eof_end_bits * 16;
            eof_ref = eof;
            return ba;
        }

        static IEnumerable<string> NaturalSort(IEnumerable<string> list) // https://stackoverflow.com/a/10000192
        {
            int maxLen = list.Select(s => s.Length).Max();
            Func<string, char> PaddingChar = s => char.IsDigit(s[0]) ? ' ' : char.MaxValue;

            return list.Select(s => new
            {
                OrgStr = s,
                SortStr =
                Regex.Replace(s, @"(\d+)|(\D+)", m => m.Value.PadLeft(maxLen, PaddingChar(m.Value)))
            })
                .OrderBy(x => x.SortStr).Select(x => x.OrgStr);
        }

        static byte[] GenExportJpeg(string path, ref bool eof_ref, ref int eof_end_bits_ref)
        {
            bool eof = false;
            int eof_end_bits = 0;
            int undefined_pixels = 0;
            var files = Directory.GetFiles(path, "out*.jpg");
            files = NaturalSort(files).ToArray();
            int whitePixs = 0;
            int blackPixs = 0;
            Console.Write($"\n");
            long eof_pos = 0;
            long byte_counter = 0;
            byte[] ba = new byte[1280 * 720 * files.Length + 1];
            BitArray bits = new BitArray(ba.Length);
            int b = 0;
            for (int i = 0; i < files.Length; i++)
            {
                byte[] fileBytes = File.ReadAllBytes(files[i]);
                Console.Write($"\rReading input image {i + 1}/{files.Length}");
                FileInfo file = new FileInfo(files[i]);
                Image<Rgba32> image = Image.Load<Rgba32>($"{path}/{file.Name}");
                int width = image.Width;
                int height = image.Height;
                Rgba32[] pixelArray = new Rgba32[width * height];
                image.CopyPixelDataTo(pixelArray);
                int tolerance = 200;
                for (int x = 0; x < pixelArray.Length; x += 4)
                {
                    var offset = image.Width * 0.4f;
                    if (x % image.Width == 0 && x != 0)
                    {
                        x += (3 * image.Width);
                        if (x >= pixelArray.Length) { break; }
                    } //4*width - 1
                    if (pixelArray[x].R <= tolerance && pixelArray[x].B >= tolerance)// blue
                                                                                     //if (pixel.R != 0 && pixel.G == 0 && pixel.B != 0)// gray
                    {
                        eof = true;
                        //bits.CopyTo(ba, 0);
                        eof_pos = byte_counter - 1;
                        eof_end_bits++;
                        //bits.Set(b, false);
                        // break;
                    }
                    else if (pixelArray[x].R >= tolerance && pixelArray[x].B >= tolerance) // white
                    {
                        //if (!eof) bits.Set(b, true); else eof_end_bits++; ;
                        bits.Set(b, true);
                        whitePixs++;
                        byte_counter++;
                    }
                    else if (pixelArray[x].R <= tolerance && pixelArray[x].B <= tolerance)// black
                    {
                        if (!eof) { blackPixs++; } else eof_end_bits++;
                        bits.Set(b, false);
                        byte_counter++;
                    }
                    else
                    {
                        undefined_pixels++;
                    }
                    b++;
                    /*if (b / 8 == 7200)
                    {
                        Console.Write("\nPeep!");
                    }*/
                }
                bits.CopyTo(ba, 0);
                //byteList.Add(BitArrayToByteArray(bits));

                //if (eof) break;
            }
            Console.Write($"\nUndefined pixels: {undefined_pixels}");
            Console.Write($"\nIn White pixels: {whitePixs}");
            Console.Write($"\nIn Black pixels: {blackPixs}");
            var eof_s = eof ? "EOF found" : "WARNING: EOF NOT FOUND! FILE MAY BE CORUPTED!";
            Console.Write($"\n{eof_s} - {eof_pos / 8}");
            eof_end_bits_ref = (int)eof_pos + 8; //eof_end_bits * 16;
            eof_ref = eof;
            return ba;
        }

        static void VideoToFrames(string path) // https://github.com/radek-k/FFMediaToolkit#code-samples
        {
            int i = 0;
            var file = MediaFile.Open(path);
            if (!Directory.Exists("./tmp-mp4"))
            {
                Directory.CreateDirectory("./tmp-mp4");
            }
            else
            {
                var files = Directory.GetFiles("./tmp-mp4", "out*.*");
                if (files.Length > 0) { Console.WriteLine("\nClearing all old output files"); }
                foreach (var f in files) { File.Delete(f); }
            }

            // while (file.Video.TryGetNextFrame(out var imageData))
            for (int b = 0; b < file.Video.Info.NumberOfFrames; b++)
            {
                Console.Write($"\rGenerating input image {i + 1}/{file.Video.Info.NumberOfFrames}");
                byte[] pixelBytes = new byte[1280 * 720 * Unsafe.SizeOf<Bgr24>()];
                file.Video.TryGetNextFrame(pixelBytes);
                Image<Bgr24> image = Image.LoadPixelData<Bgr24>(pixelBytes, 1280, 720);//imageData.ToBitmap();      
                image.SaveAsJpeg($"./tmp-mp4/out{i++}.jpg");
                //imageData.ToBitmap().Save($"./tmp-mp4/out{i++}.jpg");
                // See the #Usage details for example .ToBitmap() implementation
                // The .Save() method may be different depending on your graphics library
            }
            Console.Write("\n");
            file.Dispose();
        }

        static void ReadFile(string name, string path, OutputMode outputMode)
        {
            //var files = Directory.GetFiles("./", "out*.jpg");
            //var files = Directory.GetFiles("./", "out.gif");
            bool eof = false;
            int eof_end_bits = 0;
            byte[] data = new byte[(1280 * 720) * 7];

            switch (outputMode)
            {
                case OutputMode.GIF:
                    Console.Write($"\n>> Reading file: {path}");
                    data = GenExportGif(path, ref eof, ref eof_end_bits);
                    break;
                case OutputMode.JPEG:
                    Console.Write($"\n>> Reading files in: {path}");
                    data = GenExportJpeg(path, ref eof, ref eof_end_bits);
                    break;
                case OutputMode.Video:
                    Console.Write($"\n>> Reading file: {path}");
                    data = GenExportVideo(path, ref eof, ref eof_end_bits);
                    break;
            }

            Console.Write($"\nSaving output");
            if (!Directory.Exists("./export")) { Directory.CreateDirectory("./export"); }
            Array.Resize(ref data, (eof_end_bits / 8)); // would be nice to know how to prevent this 

            var decompressed_data = zstd_decompress(data, 3);

            File.WriteAllBytes($"./export/{name}", decompressed_data);
            //if (files.Length > 0) { Console.WriteLine("Clearing all output files"); foreach (var f in files) { File.Delete(f); } }
            Console.WriteLine($"Done!");
        }

        static void ClearConsole()
        {
            Console.Clear();
            Console.Write("infinite storage glitch - by memorix101");
        }

        static void Main(string[] args)
        {
            FFmpegLoader.FFmpegPath = "./ffmpeg/";

            ClearConsole();

            string name = "profile.jpg";
            string path = $"./{name}";
            string input_path = $"./out.gif";
            OutputMode outputMode = OutputMode.JPEG;

#if RELEASE
            bool quit = false;
            while (!quit)
            {
                Console.Write("\rChoose a task:\n0 - Write a file\n1 - Read a file\n2 - Quit\n");
                string input = Console.ReadLine();
                switch (input)
                {
                    case "0":
                        ClearConsole();
                        Console.Write("\rEnter file path:\n");
                        path = Console.ReadLine();
                        ClearConsole();
                        Console.Write("\rOutput format (0 - GIF, 1 - JPEG, 2 - MP4):\n0 for default - GIF\n");
                        string f = Console.ReadLine();
                        int _outMode = int.Parse(f);
                        WriteFile(path, (OutputMode)_outMode);
                        break;
                    case "1":
                        ClearConsole();
                        Console.Write("\rEnter file path (Directory for JPGs):\n");
                        path = Console.ReadLine();
                        ClearConsole();
                        Console.Write("\rInput format (0 - GIF, 1 - JPEG, 2 - MP4):\n0 for default - GIF\n");
                        string fi = Console.ReadLine();
                        int _ioutMode = int.Parse(fi);
                        Console.Write("\rEnter output filename with extention (e.g.: text.txt)\n");
                        name = Console.ReadLine();
                        ReadFile(name, path, (OutputMode)_ioutMode);
                        break;
                    case "2":
                        quit= true;
                        break;
                }
            }
#else
            WriteFile(path, OutputMode.GIF);
            ReadFile(name, "./out.gif", OutputMode.GIF); // gif
            //ReadFile(name, "25dD9ha.gif", OutputMode.GIF); // gif
            //ReadFile(name, "./", OutputMode.JPEG); // jpg
            //ReadFile(name, "./tmp-mp4", OutputMode.JPEG); // mp4
            //WriteFile(path, OutputMode.Video);
            //VideoToFrames("./igs   csharp test   15 fps v2.mp4");
            //ReadFile(name, "./tmp-mp4", OutputMode.JPEG); // mp4
            //ReadFile(name, "./infinite-storage-glitch-csharp demo.mp4", OutputMode.Video); // mp4
            //ReadFile(name, "./test.mp4", OutputMode.Video); // mp4
#endif
        }
    }
}