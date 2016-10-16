# jClipCornLink
Starts jClipCorn by proxy

This is simply a small exe that looks at `%appdata%\jCLipCorn\jClipCornLink.cfg` for a list of paths to check for jClipCorn instances.

The first one that exists is started.

I use this to pin jClipCorn to the Windows7 taskbar (otherwise it is sometimes automatically removed and sometimes the icon is not correctly displayed)

Feel free to do whatever you want with it


##Example jClipCornLink.cfg file

~~~
# jClipCornLink example config file

# Place this file in %appdata%\jClipCorn\jClipCornLink.cfg

# The rules are evaluated top to bottom
# The first file that is found gets executed


# You can specify drives by their label
<?vLabel="MyHDD">Filme\jClipCorn.exe
<?vLabel="MyHDD">Filme\jClipCorn.jar

<?vLabel="MyOtherHDD">Filme\jClipCorn.exe
<?vLabel="MyOtherHDD">Filme\jClipCorn.jar

# You can alse specifiy drives by their driveletter
<?vLetter="O">Filme\jClipCorn.exe

# And you can reference the durrent drive
<?self[dir]>Filme\jClipCorn.jar


# You can also refernce the current working directory
<?self>jClipCorn.jar

# And you can dynamically specify version numbers. We automatically use the highest version found
# (VERSION-4 is Mayor; VERSION-3 is Minor; VERSION-2 is PATCH and VERSION-1 is BUILD)
# But you can specify as many parts of the version number as you want
<?vLabel="MyHDD">Filme\jClipCorn {VERSION-3}.{VERSION-2}.{VERSION-1}.jar
~~~


##Download

Download via the [GitHub Releases page](https://github.com/Mikescher/jClipCornLink/releases) or clone and compile it yourself ;)