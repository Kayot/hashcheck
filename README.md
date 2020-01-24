# hashcheck
A dotnet core console application that can verify file integrity

This program creates a checkfile named what ever you want and stored where ever you want. The path is relative, so moving the data to another drive or share won't affect the process.

## Usage

Using just the program will show it's help.
```
Hash Check v0.1
Usage: hashcheck [action] [target]

-cf [filename]          Creates a check file
-vf [filename]          Checks a check file
-uf [filename]          Updates a check file (Removes missing, adds new)
```

Example Usage
You have a backup hard disk currently mounted as d:\ and it has several folders with linux isos in them. Backup drives are vulnerable to bitrot just like any other piece of magnetic equipment. Let's create a check file.

`hashcheck -cf d:\Applications.hashcheck d:\Applications`

What this does is, `-cf` Create (Check) File. I'm putting the file on the root of d: because I want it to travel with my backup drive. The `d:\Application.hashcheck` is the file I'm creating. I can name it anything I want with any extention as it really doesn't care. In this case I'm naming it after the folder. I could target d:\, but I would prefer to check my folders individually. This program should ignore the file it's creating during it's creation process. Next is `d:\Applications` this is the target of my file. This program will go into that folder and create SHA1 hashes of every file it has access to, get file size, modification date, and create date. It will store this data in the check file for later varification.

Once the process is compelete, the program exits.

Next we'll check the data.

`hashcheck -vf d:\Applications.hashcheck d:\Applications`

The `-vf` is for verify files. 

`d:\Applications.hashcheck` is the check file I'm using to check the data at `d:\Applications`. It will go into the directly and output any issues to the console. It will only verify the SHA1 hash if the files are the same size. Please note that this path is reletive, meaning that if the Applications folder is moved to a sub folder or another drive, this process will still work, for instance;

Let's say the data is move to drive e:\storage, to use the check file you would use the following command (assuming you didn't move the check file)

`hashcheck -vf d:\Applications.hashcheck e:\storage\Applications`

Or if you move the check file to the root of drive e:.

`hashcheck -vf e:\Applications.hashcheck e:\storage\Applications`

Or if you move the check file to the storage folder

`hashcheck -vf e:\storage\Applications.hashcheck e:\storage\Applications`

Paths are relative. That is the point of this project. Ease of use.

Experimental;

I haven't tested this under linux as of yet, however the usage is about the same, only target mount points instead of drives.

## Tested

I've tested this in Windows 10 (1909)
