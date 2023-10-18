# EditorManagement
EditorManagement aims to make the Project Arrhythmia (Legacy) editor more powerful than ever.

Features this mod contains:

- New layer system that allows you to go well beyond 5 editor layers. (Rather than having 5 buttons, it's a singular input field that allows you to set the layer all the way up to the integer limit)
- Level list that can load from any directory within "application path/beatmaps" and can be sorted by different types such as level name and artist name. Path saving and loading also works for themes and prefabs.
- Prefabs can now be created internally rather than just externally.
- Adds marker looping so you can loop between a set end marker and a set start marker.
- Little reminder pops up at a set repeat time to remind you to have a break! (Completely optional)
- Fixes naming of prefabs and themes, now allowing you to type in names any way you want. (Prefabs use to not allow spaces and themes used to require the first letter of each word to be uppercase AND didn't allow numbers or other symbols)
- Autosave changes, can now be customized at a set repeat rate and file limit. Autosaves now save in "level path/autosaves" rather than just "level path".
- Last editor position (Layer, timeline scrollbar, audio time, zoom) now saves to "level path/editor.lse" and can also load from there whenever you open a level. (Audio time saves to "level path/level.lsb" as "timeline_pos")
- Adds Multi Object editor with options to change the group selected objects layer, depth and more. (Includes syncing of objects)
- Object Search for finding a specific object you can't find anywhere in your timeline!
- Scroll wheeling on most input fields involving numbers. (Hold Shift to increase / decrease both X and Y variables if that exists. Hold Ctrl to increase / decrease the amount by a large sum. Hold Alt to do the same but with a smaller sum.)
- Only used themes now save to level.lsb. (It used to save every theme to the file)
- More tooltips.
- Full support for other mods (EventsCore, CreativePlayers and ObjectModifiers).

![2023-02-16_04 27 37](https://user-images.githubusercontent.com/125487712/219107098-69ce2d9f-473a-4970-bbb3-7b10a8e0c8d8.png)

https://user-images.githubusercontent.com/125487712/219871989-d8198341-0c4a-4d11-9982-e4426cc241a0.mp4

- Configurable settings for all kinds of editor UI stuff.

![2023-02-16_04 30 34](https://user-images.githubusercontent.com/125487712/219107289-bd08403f-f7a5-4f94-8736-b19e513107a8.png)

- Adds a button to the metadata editor that takes you to the song artist link you set.

![2023-02-16_04 28 03](https://user-images.githubusercontent.com/125487712/219107418-e4b75e3d-f437-41d9-bd22-11f15eb33a3b.png)

- Makes the settings dialog (window) more useful where it shows a ton of information about the currently loaded level. (E.G. object count, time in editor, text object count, etc)

![2023-02-16_04 27 41](https://user-images.githubusercontent.com/125487712/219107482-c5ef0295-a018-466d-8c43-75a6d69f193b.png)

- Fixes save as so it now properly works, including bringing back the old save as UI elements.

![2023-02-16_04 28 15](https://user-images.githubusercontent.com/125487712/219107546-cd1f19ca-2270-44c6-9973-47175e876e68.png)

- Brings back the unused internal new level popup brower.

![2023-02-16_04 28 23](https://user-images.githubusercontent.com/125487712/219107586-67a1f6f5-e5d7-4364-a0fd-fe104ba4bbb3.png)
