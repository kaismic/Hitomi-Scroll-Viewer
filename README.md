# Hitomi Scroll Viewer
A viewer for [hitomi.la (NSFW)](https://hitomi.la) with features such as auto scrolling, searching by tags and downloading.

Built using C# .NET 6, WinUI 3

## Preview
<div>
    <img src="images/preview_0.png" style="width: 100%;">
    <img src="images/preview_1.png" style="width: 100%;">
    <img src="images/preview_2.png" style="width: 100%;">
</div>

## Features
- Searching galleries by custom tags
- Auto scrolling / auto page turning
- Download/Bookmark galleries
- Image zooming in/out

## Controls
- Doubleclick to switch between pages

In image watching page:
- Press spacebar to start/stop auto page turning/auto scrolling
- Press 'L' key to enable/disable loop when auto page turning/auto scrolling
- Press 'V' key to change view mode (Default/Scroll)
- Hold 'Ctrl' key and use mouse wheel to zoom in/out
- In Default mode:
    - Use left/right keys to switch between images

## Notes
It is not recommended to increase the maximum concurrent request above 1 when the gallery contains more than 20 images because hitomi.la throws 503 error on rapid request above its API rate limit
