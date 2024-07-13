# Void color editor
Fixes the gross bitrate distroying dithered void on Halo 3: MCC (1.3385.0.0)

![tool](https://github.com/TermaciousTrickocity/Void-fix/assets/62641541/1eb421e2-125a-4d40-85a8-fc20abb0768a)

> huge thanks to Lord Zedd

## How to fix in the rare event MCC gets an update!
- AOB scan for: `8D 3C 1B 8D E8` with only `Executable` checked in the memory scan options.
- Look for `B9 10000000` (which corresponds to `mov ecx, 00000010`) and save the corresponding address.
- The saved address is your `voidDrawAddress` (2 bytes). To remove the dithering effect on the void, change `(0x10, 0xB9)` to `(0xFF, 0xB9)`.
- Returning to the AOB scan from earlier, the first three bytes are the RGB values for the void (in BGR order due to little-endian byte ordering).
- Change the proper addresses for `voidRedColorAddress`, `voidGreenColorAddress`, and `voidBlueColorAddress`.
