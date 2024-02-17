# EditorManagement
EditorManagement aims to make the Project Arrhythmia (Legacy) editor more powerful than ever.

Features this mod contains:

- New layer system that allows you to go well beyond 5 editor layers. Rather than having 5 buttons, it's a singular input field that allows you to set the editor layer all the way up to 2147483646 layers. Event Keyframes now render on their own editor layer type.
- Level list that can load from any directory within "application path/beatmaps" and can be sorted by different types such as level name and artist name. Path saving and loading also works for themes and prefabs.
- Prefabs can now be created internally rather than just externally.
- Adds marker looping so you can loop between a set end marker and a set start marker.
- Fixes naming of prefabs and themes, now allowing you to type in names any way you want. (Before, Prefabs didn't allow spaces and Themes required the first letter of each word to be uppercase AND didn't allow numbers or other symbols)
- Autosave changes, can now be customized at a set repeat rate and file limit. Autosaves now save in "level path/autosaves" rather than just "level path". Autosaves can also now be accessed by holding shift when you click on a level to load. Doing that will open a Autosave popup, containing a list of autosaves / backups. Clicking on an autosave / backup will load it. The backup button will change it from an autosave to a backup and back if it already is a backup. Autosaves get automatically deleted if the limit is reached, but backups do not.
- Last editor position (Layer, timeline scrollbar, audio time, zoom) now saves to "level path/editor.lse" and can also load from there whenever you open a level. (Audio time saves to "level path/level.lsb" as "timeline_pos")
- Adds Multi Object editor with options to change the group selected objects layer, depth and more. (Includes syncing of objects and replacing certain text values like name and object text)
- Object Search for finding a specific object you can't find anywhere in your timeline!
- Scroll wheeling on most input fields involving numbers. (Hold Shift to increase / decrease both X and Y variables if that exists. Hold Ctrl to increase / decrease the amount by a large sum. Hold Alt to do the same but with a smaller sum.)
- Only used themes now save to level.lsb. (It used to save every theme to the file)
- More tooltips.
- Full support for other mods (EventsCore, CreativePlayers and ObjectModifiers).
- In-editor Project Planner with Documents, To do lists, Character planners, etc.
- VG to LS and LS to VG conversion system, meaning modern PA levels can be converted to modded Legacy format and modded Legacy levels can be converted to modern PA format.
- Opening levels from anywhere on your computer in the editor.
- Basically instant object / keyframe selection and deletion, no more freezing.
- Pretty fast pasting and expanding of prefabs, though not quite instant.
- Copy / Paste Beatmap Object Keyframe data.
- Fully customizable keybinds with various functions.
- Custom editor layer colors and marker editor colors.
- Custom Prefab Types with icons, based on modern PA Prefab Type icons but implemented in a Legacy way.
- Mostly un-limited values for Event Keyframe values, audio pitch and origin offset.
- Edit-able Prefab Object offset values.
- Multi Keyframe editing.
- Lots of debugging functions.
- Viewing the Game Timeline in Editor mode, rather than just Preview mode.
- Full object dragging in Preview system along with rotation / scale dragging.
- Configurable settings for all kinds of editor UI stuff.
- Adds a button to the metadata editor that takes you to the song artist link you set.
- Makes the settings dialog (window) more useful where it shows a ton of information about the currently loaded level. (E.G. object count, time in editor, text object count, etc) Includes editing marker colors and editor layer colors.
- Fixes the Save As function so it now properly works, including bringing back the old save as UI elements.
- Brings back the unused internal New Level Popup Browser.
- Supports .wav and .mp3 formats rather than just .ogg.
