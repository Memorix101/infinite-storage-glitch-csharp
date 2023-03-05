# infinite-storage-glitch-csharp

![](https://i.imgur.com/ZwUBg46.gif)

This is my own implementation based on the idea by [DvorakDwarf's Infinite-Storage-Glitch](https://github.com/DvorakDwarf/Infinite-Storage-Glitch) in C# .NET 6.
I'm not sure but it should work on Linux as well.
Sadly, not on macOS becasue of limitations with [FFMediaToolkit](https://github.com/radek-k/FFMediaToolkit).

It is not compatible with DvorakDwarf's version, but the idea is similar.

The program uses [FFMpeg 5.1.2 for Windows](https://www.gyan.dev/ffmpeg/builds/packages/ffmpeg-5.1.2-full_build-shared.7z). Linux needs to install it via terminal. Makes sure to use shared binaries on Windows. On Linux download FFmpeg using your package manager.

### What is different?

This program can not only save your files (any file) in a MP4, but also embed it in a GIF or many JPGs (depending on size).

A demo video (v0.2.0) can be found here: [YouTube](https://youtu.be/8UzyYN0uwlM)

A demo video with metadata can be found here (**only for v0.2.2+**): [YouTube](https://www.youtube.com/watch?v=mNebJd2W7Lo)

Also, there is a demo GIF (v0.2.0) (**Download it only in original size!**): [Imgur](https://i.imgur.com/ZwUBg46.gif)

### How does it work and limitations

Each frame uses pixels in 4x4 size (1 pixel is a 16 pixel block). RGB values are used to represent bit values. So what the program does is translating the bytes (8 bits = 1 byte) of a file to bits represented in pixels. 0 value bit = black -> RGBA32(0,0,0,255) and 1 value bit = white -> RGBA32(255,255,255,255). [RGBA32](https://docs.sixlabors.com/api/ImageSharp/SixLabors.ImageSharp.PixelFormats.Rgba32.html)(red, green, blue, alpha).
In v0.2.0 the EOF (end of file) is marked as a blue pixel on the last frame RGBA32(0,0,255,255).
v0.2.2+ has a metadata first frame which stores the EOF (uint32) and the original filename after a magic number (ISGv2). No blue pixel anymore.
The output resolution is 1280x720 (720p).

![Pixel block example](https://i.imgur.com/pzIPSMt.png)

**It is importaint that the output file does not get resized and/or loses frames, otherwise your data might be corrupted.** So, make sure only to download the original or a close to the original output file. Compression is dangerous here and actually the challenge of the project. 4x4 pixels seem to work well with this.

![Example frame](https://i.imgur.com/TIcaRLm.jpg)

**Keep in mind that embedded files are larger than your original one!**

### About the project

The code is not clean and "hacked together".
It needs optimization and clean up.
The project uses [Zstd compression](https://facebook.github.io/zstd/).
This is a proof of concept project for educational purposes. This might violate the YouTube TOS (or any other service).
You use it at your own risk.

### Dependencies (nuget packages)

[FFMediaToolkit](https://github.com/radek-k/FFMediaToolkit) <br/>
[ZstdSharp](https://github.com/oleg-st/ZstdSharp) <br/>
[ImageSharp](https://github.com/SixLabors/ImageSharp) <br/>
[ImageSharp.Drawing](https://github.com/SixLabors/ImageSharp.Drawing) <br/>