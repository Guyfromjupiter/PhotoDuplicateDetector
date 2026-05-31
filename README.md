#Photo Duplicate and similar detector

This is a tool I build which takes a folder pathh, checks for imagee file and thenn uses SHA256 and Phash to compare, the similar photos would be added into a new folder.

##Features
- Getting Image paths and storring it in a list
- SHA 256
- Phash
- optimized Phash
- Parallel programming
- serialization of resize funtion
- DCT II normalized version used for image processing

##Dependacies
- .NET 9.0
- System.Drawing.Common (10.0.8) 
- Window OS

##NOTICE
-this uses GDI+

## Installation

### Requirements
- Windows OS
- .NET 9.0 SDK or Runtime

### Steps
```bash
git clone https://github.com/Guyfromjupiter/PhotoDuplicateDetector
cd your-repo
dotnet restore
dotnet build
```

##How this works
- Image Scanner first ask how many directory you want to check similar image from and then takes the specified number of directory path in a list
- it uses the direcory path and look into files and sub directory. for subdirectory i used recursion can be seen in ProcessDirectory(string targetDirectory) in ImageScanner file
- IsImageFile(string) function checks extensions s only path we get is of image (IsImageFile is a functiion in ImageScanner)
- after this all SHA256HashLayer.Sub1()
- this starts converting paths in ImageScanner.ImagePath into SHA256. this create image hash so we have to open image file or else it will hash thee path
- and then compare it.
- after this we do phash
- Resizing image
- turning it to grayscale
- DCT (discrete cosine transform) to get the frequency of the image
- perceptual hashing
- hamming distance
- I will completee the rest after completion of project

## Roadmap
- GPU acceleration
- Rotation invariant hashing
- GUI interface

## Contributing
Pull requests are welcome.
For major changes, open an issue first.
