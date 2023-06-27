# Hitomi Scroll Viewer

## Preview (old version)
<div>
    <img src="images/preview_0.png" style="width: 100%;">
    <img src="images/preview_1.png" style="width: 100%;">
    <img src="images/preview_2.png" style="width: 100%;">
</div>

## Controls
- Doubleclick to switch between pages

In image watching page:
- Press 'L' key to enable/disable loop
- Press spacebar to start/stop auto page turning/auto scrolling
- Default mode:
    - Use left/right keys/mouse buttons to switch images

## Bugs
### Critical
1. Currently memory leaks occur when you load images and switch between pages. It's happening probably because of this issue: https://github.com/microsoft/microsoft-ui-xaml/issues/5978
### Non-critical
1.
    https://github.com/kaismic/Hitomi-Scroll-Viewer/blob/6b785fa2a59347cdaaae857cecc734238ab5ca9c/Hitomi-Scroll-Viewer/SearchPageComponent/TagContainer.xaml.cs#L13-L19
    Refer to this issue: https://github.com/microsoft/microsoft-ui-xaml/issues/1826
