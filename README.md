# Hitomi Scroll Viewer
A viewer for hitomi.la (18+) with features such as auto scrolling, searching by tags and downloading.

Built using C# .NET 6, WinUI 3

## Preview
<div>
    <img src="images/preview_0.png" style="width: 100%;">
    <img src="images/preview_1.png" style="width: 100%;">
    <img src="images/preview_2.png" style="width: 100%;">
    <img src="images/preview_3.png" style="width: 100%;">
</div>

## Features
- Search galleries with multiple tag filters
- Auto scrolling / Auto page turning
- Download galleries
- Change view direction
- Image zooming in/out

## How to install
1. Unzip one of the x86, x64 or arm64 zip files
2. Open Hitomi-Scroll-Viewer_x.x.x.x_x64.cer (or x86, arm64) -> Install Certificate-> Local Machine -> Place all certificates in the following store -> Browse -> Trusted Root Certification Authorities-> Next -> Finish
3. Open Hitomi-Scroll-Viewer_x.x.x.x_x64.msix
4. Install .NET 6.0 Desktop Runtime if it also pops up

## Controls
- Doubleclick to switch between pages

In image watching page:
- Press Spacebar to start/stop auto page turning/auto scrolling
- Press `L` key to enable/disable loop when auto page turning/auto scrolling
- Press `V` key to change view mode (Default/Scroll)
- Hold `Ctrl` key and use mouse wheel or `+`, `-` key to zoom in/out
- In Default mode:
    - Use left / right keys to switch between images

## Notes
- It is not recommended to download a large number of galleries together or downloading with large thread number because hitomi.la throws 503 error on rapid request above its API rate limit.
