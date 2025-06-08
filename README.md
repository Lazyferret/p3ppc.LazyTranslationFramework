# p3ppc.LazyTranslationFramework
# Persona 3 Portable EXE Text Unhardcoding Mod

![Mod screenshot](https://images.gamebanana.com/img/ss/mods/68456b52b3b94.jpg)

This mod aims to simplify the process of implementing fan translations into *Persona 3 Portable* by unhardcoding text stored within the game's executable (EXE). It provides a more flexible way to handle custom translations without requiring extensive binary edits.

## Key Features:

- **Text Unhardcoding** – Removes hardcoded text strings from the EXE, making translations easier to manage.  
- **Custom Encoding Support** – Works with fan-made text encodings, particularly those designed for replaced fonts.  
- **Modular & Extensible** – Designed to support future improvements and additional language patches.  

### Custom Encoding Setup
Create `CustomEncoding.json` with your character mappings:
```json
"Б": "\\x42",
"В": "\\x56",
"Г": "\\x47",
"Д": "\\x44",
"Е": "\\x45",
"Ë": "\\x80\\xE1",
"Ж": "\\x80\\xE9",
"З": "\\x5A",
"И": "\\x49",
"Й": "\\x80\\xE2",
"К": "\\x4B",
"Л": "\\x4C"
```

### Translating XREF Strings

Add translations for referenced strings:
```json
"0x1405fc884": {
    "text": "Мечи",
    "original": "Sword",
    "occurrences": ["0x1407d89d8"]
},
"0x1405fc88c": {
    "text": "Пентакли",
    "original": "Coin",
    "occurrences": ["0x1407d89e0"]
}
```

### File Replacement
Standard Replacement

Use EmbededFiles.json for file mappings:
```json
{
    "0x140802FF0": {
        "file": "Proceed_PC.bmd",
        "occurrences": ["0x1408030b8"]
    }
}
```
### Replacing Files Without Memory Reallocation
For direct file replacement (no memory reallocation), use EmbededFilesRawReplace.json:

```json
{
    "C0 96 DD 81 8C 94 B5 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 53": {
        "offset": 33,
		"file": "calendar_PC.dat"
    }
}
```

> **Note:** This project is a work in progress and may undergo changes as development continues. Feedback and contributions are welcome!