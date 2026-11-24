# Umwami's Ascend AR

A phone AR story player. Find a table, place a small diorama on it, and watch the Rwandan legend of Kigwa, the first king, play out with narration and subtitles. It tells the first part of the legend in 7 chapters, from his birth in the sky world to the people of Mubari naming him and his companions Ibimanuka. It is a story, not a game.

## Requirements

- Unity 6000.6.0f1 with Android Build Support.
- An ARCore-supported Android phone (Android 10 or newer) with USB debugging on.

## Run it

**On a phone:** open the folder in Unity Hub, then File > Build Profiles > Android > Switch Platform. The scene list (Title, then Story) is already set. Connect the phone and click Build And Run.

**In the Editor:** open `Assets/Scenes/Title.unity` and press Play. The Scan page moves on after 5 seconds, and a click places the diorama.

## Test it

1. Title: press Begin.
2. Scan: move the phone slowly over a table until the page changes.
3. Place: tap the surface to place the diorama, pinch to zoom, twist to turn, then press Start.
4. Story: narration, subtitles (CC button) and characters play through the 7 chapters in about two minutes. Pause, previous, next and the chapter list all work.
5. Cover the camera: the story pauses until tracking returns.
6. After chapter 7 the credits page offers Watch again, Chapters and Title.

## Project layout

- `Assets/Scenes/`: the Title and Story scenes.
- `Assets/MobileARTemplateAssets/UI/`: UI screens and styles.
- `Assets/MobileARTemplateAssets/UI/Scripts/`: our code. `StoryData` holds the chapters and subtitles, `StoryScreen` the pages and playback, `DioramaStage` the placing and scaling, and `StoryDirector` who stands where in each chapter.
- `Assets/Resources/`: dioramas, characters, textures and narration clips.

## Credits

Models by KayKit (CC0), recoloured for this story. The story follows the written accounts of Alexis Kagame and later scholars, simplified for a short telling.
