# Hitomi Scroll Viewer
[![GitHub latest release](https://img.shields.io/github/release/kaismic/Hitomi-Scroll-Viewer.svg?logo=github)](https://github.com/kaismic/Hitomi-Scroll-Viewer/releases/latest)
[![GitHub downloads count latest release](https://img.shields.io/github/downloads/kaismic/Hitomi-Scroll-Viewer/latest/total.svg?logo=github)](https://github.com/kaismic/Hitomi-Scroll-Viewer/releases/latest)
[![GitHub downloads count total](https://img.shields.io/github/downloads/kaismic/Hitomi-Scroll-Viewer/total.svg?logo=github)](https://github.com/kaismic/Hitomi-Scroll-Viewer/releases)

*Read this in other languages:* [한국어](README-ko-KR.md)

A viewer for hitomi.la (18+) with various features.

Built using WinUI 3 with C#.

Currently supported languages: English and Korean

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
If you are upgrading the app from version 2.x.x.x to 2.x.x.x, please skip step 2. Otherwise, follow all the steps.
1. [Download](https://github.com/kaismic/Hitomi-Scroll-Viewer/releases/latest) `Hitomi-Scroll-Viewer.cer` and `Hitomi-Scroll-Viewer_x.x.x.x.msixbundle`.
2. Open `Hitomi-Scroll-Viewer.cer` -> Install Certificate-> Local Machine -> Place all certificates in the following store -> Browse -> Trusted People -> Next -> Finish.
3. Open `Hitomi-Scroll-Viewer_x.x.x.x.msixbundle` and install.
4. Also, if a window pops up saying "You must install .NET Desktop Runtime...", install it.

## Usage / Controls
- In the search page:
    - Enter each tag in the text boxes at each new line.
- In the view page:
    - Move the mouse to the top of the screen to show the settings menu.
    - Press the Spacebar to start/stop auto scrolling.
    - Press the `L` key to enable/disable the loop when auto-scrolling.
    - Use the left/up and right/down arrow keys or mouse wheel to switch between pages.
