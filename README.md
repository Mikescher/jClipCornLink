# jClipCornLink
Starts jClipCorn by proxy

This is simply a small exe that looks at `%appdata%\jCLipCorn\jClipCornLink.cfg` for a list of paths to check for jClipCorn instances.

The first one that exists is started.

I use this to pin jClipCorn to the Windows7 taskbar (otherwise it is sometimes automatically removed and sometimes the icon is not correctly displayed)

Feel free to do whatever you want with it





##Example jClipCornLink.cfg file

~~~
<?vLabel="MyExternalDrive">Filme\jClipCorn.exe
<?vLabel="MyExternalDrive">Filme\jClipCorn.jar

<?vLabel="OtherExternalDrive">Filme\jClipCorn.exe
<?vLabel="OtherExternalDrive">Filme\jClipCorn.jar

<?vLetter="O">Filme\jClipCorn.exe
<?vLetter="O">Filme\jClipCorn.jar

#<?self[dir]>Filme\jClipCorn.exe
#<?self[dir]>Filme\jClipCorn.jar
~~~
