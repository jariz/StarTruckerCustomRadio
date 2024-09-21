# StarTruckerCustomRadio

A mod for Star Trucker that replaces the build in radio station with your own.

- ♻ Replace songs, adverts and [stings](https://en.wikipedia.org/wiki/Sting_(musical_phrase)) with your own files.
- 🤠 Supports most[1] audio formats
- 🎸 Shows title and artists in game if present in file metadata[2].
- 🧒 Easy installation, no modding knowledge required: just drop it in, it will create a folder, you plop in your files and you're done.
- ✏ Configurable:
	- Radio station name and frequency can be configured.
	- Folder paths can be changed.
	- You can also choose to leave the default stings/adverts/songs enabled if you wish.
	- More [below](#configuration)

----
_[1] if CSCore supports it, we can play it, see full list: https://github.com/filoe/cscore#supported-features_  
_[2] if TagLib supports it, we can read metadata from it, yadda yadda: https://github.com/taglib/taglib#taglib-audio-metadata-library_

## Installation

### 1. Install MelonLoader
Get the MelonLoader installer [from their website here](https://melonwiki.xyz/#/?id=requirements) and install it to Star Trucker.

Finding the game path:

**On Steam**, the game can be found by right clicking it > Browse local files.  
While installing MelonLoader, choose `Star Trucker.exe`.

**On GamePass**, click the game, click the `...` button, 'manage', files tab, and click 'browse'.  
**IMPORTANT**: While installing MelonLoader on gamepass, you should choose `gamelauncherhelper.exe` instead of `Star Trucker.exe` (!!!)

### 2. Install the mod
Get the [latest release from here](https://github.com/jariz/StarTruckerCustomRadio/releases).  
Drop the zip's contents in your game folder.

### 3. First run
1. Start the game, if all went well you should see the following warning message:
![Star_Trucker_ljcRT1fRyH](https://github.com/user-attachments/assets/9fb09ae5-efc5-4117-b572-a74c9c8a23e1)

2. Close the game.

3. The mod has created a `StarTruckerCustomRadio` folder in your local user's music folder.  
You can now add files to the `Songs`, `Adverts` and `Stings` folder, once done, restart. You're done.

## Configuration


## Wishlist

This mod is feature complete and I'm personally done with it, but some fun ideas you reading this (if you have moderate c# experience) could implement (PR's are welcomed!):

- Make it possible to have multiple radio stations (however, I'm sure the devs of the game will add this themselves at some point though through a DLC or something)
- Web streams so you can listen to real radio from the game.  
  Currently the files are already streamed from disk so streaming it from network should be fairly simple.

Inspired by the [RadioExt mod for CP2077](https://github.com/justarandomguyintheinternet/CP77_radioExt)

## Troubleshooting

### Media foundation missing (Error while attempting to decode: CSCore.MediaFoundation.MediaFoundationException)

Most of the file formats should work out of the box, however, for some formats the encoding library we use ([CSCore](https://github.com/filoe/cscore)) might fall back to encoding libs on your system.  
This requires the 'media feature pack' to be enabled in your system, [you can find those instructions here](https://support.microsoft.com/en-us/windows/media-feature-pack-for-windows-n-8622b390-4ce6-43c9-9b42-549e5328e407).  
If that doesn't work, you're probably better off just converting the file to a format we do support.

### Radio stops playing songs!

The game's logic expects there to be at least 10 tracks, the mod will warn you for this (on screen!) if it isn't the case.  
This is a problem that could probably be patched out, but not something I really personally care about fixing in the mod as the solution to end users is fairly straight forward.

### It doesn't work on my non-windows platform!

For my personal goals for this project windows compat is enough, however, PR's are welcome!
