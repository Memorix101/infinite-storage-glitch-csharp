### infinite-storage-glitch-csharp

![](https://i.imgur.com/ZwUBg46.gif)

This is my own implementation based on the idea by [DvorakDwarf's Infinite-Storage-Glitch](https://github.com/DvorakDwarf/Infinite-Storage-Glitch) in C# .NET 6.
I don't know but it should work on Linux as well.
Sadly, not on macOS becasue of limitations with [FFMediaToolkit](https://github.com/radek-k/FFMediaToolkit).

It is not compatible with DvorakDwarf's version, but the idea is similar.

The programm uses [FFMpeg 5.1.2 for Windows](https://www.gyan.dev/ffmpeg/builds/packages/ffmpeg-5.1.2-full_build-shared.7z)

### What is different?

This program can not only save your files (any file) in a MP4, but also embed it in a GIF or many JPGs (depending on size).

A demo video can be found here: [YouTube](https://youtu.be/8UzyYN0uwlM)

Also, there is a demo GIF (Download it only in original size!) [Imgur](https://i.imgur.com/ZwUBg46.gif)

### How does it work and limitations

The images use pixels in 4x4 size (1 pixel is a 16 pixel block).
The EOF (end of file) is marked on the last image as a blue pixel. I also thought about to replace that idea with a "metadata" first frame. Maybe I'll add that later.
The output resolution is 1280x720 (720p).

It is necessary that the output file does not get resized and/or loses frames, otherwise your data might be corrupted and lost. So, make sure only to download the original or a close to the original output file. Compression is dangerous here and actually the challenge of the project. 4x4 pixels seem to work well with this.

##### Keep in mind that embedded files are larger than your original one!

### About the project

The code is not clean and "hacked together" in 48hrs.
It needs optimization and clean up.
Also, I am thinking about adding additional compression.
This is a proof of concept project for educational purposes. This might violate the YouTube TOS (or any other service).
You use it at your own risk.