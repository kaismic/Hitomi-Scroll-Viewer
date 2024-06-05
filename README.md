# Hitomi Scroll Viewer
[![GitHub latest release](https://img.shields.io/github/release/kaismic/Hitomi-Scroll-Viewer.svg?logo=github)](https://github.com/kaismic/Hitomi-Scroll-Viewer/releases/latest)
[![GitHub downloads count latest release](https://img.shields.io/github/downloads/kaismic/Hitomi-Scroll-Viewer/latest/total.svg?logo=github)](https://github.com/kaismic/Hitomi-Scroll-Viewer/releases/latest)
[![GitHub downloads count total](https://img.shields.io/github/downloads/kaismic/Hitomi-Scroll-Viewer/total.svg?logo=github)](https://github.com/kaismic/Hitomi-Scroll-Viewer/releases)

*Read this in other languages:* [한국어](README-ko-KR.md)

A viewer for hitomi.la (18+) various features.

Built using WinUI 3 with C#.

## Preview
<div align="center">
    <img src="images/preview1.png" style="width: 50%;">
    <img src="images/preview2.png" style="width: 50%;">
</div>

## Features
- Create search links with combinable tag filters
- Download galleries
- Auto scrolling
- Change view direction

## How to install
If you are upgrading the app from version 2.x.x to 2.x.x, please skip step 2. Otherwise, follow all the steps.
1. [Download](https://github.com/kaismic/Hitomi-Scroll-Viewer/releases/latest) the file corresponding to your CPU architecture and unzip the file.
2. Open the unzipped folder and click on the Security Certificate file (.cer) -> Install Certificate-> Local Machine -> Place all certificates in the following store -> Browse -> Trusted People -> Next -> Finish.
3. Run MSIX File (.msix)
4. If a window pops up saying "You must install .NET Desktop Runtime...", install it.


## Usage / Controls
- In the search page:
    - Enter each tag in the text boxes at each new line.
- In the view page:
    - Move the mouse to the top of the screen to show the settings menu.
    - Press the Spacebar to start/stop auto scrolling.
    - Press the `L` key to enable/disable the loop when auto-scrolling.
    - Use the left/up and right/down arrow keys or mouse wheel to switch between pages.
