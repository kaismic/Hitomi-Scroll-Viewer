export function setChipSetContainerHeight() {
    // set tag-search-chip-set-container's height to fill the remaining space of its parent
    const leftContainer = document.getElementById("left-container");
    const resizeObserver = new ResizeObserver(
        (entries) => {
            const tagFilterEditorControlPanel = document.getElementById("tag-filter-editor-control-panel");
            if (tagFilterEditorControlPanel) {
                var computedStyle = getComputedStyle(tagFilterEditorControlPanel);
                let controlPanelHeight = tagFilterEditorControlPanel.clientHeight;
                controlPanelHeight -= parseFloat(computedStyle.paddingTop) + parseFloat(computedStyle.paddingBottom); // remove padding
                controlPanelHeight -= parseInt(computedStyle.gap) * (tagFilterEditorControlPanel.children.length - 1) // remove grid gap
                for (const child of tagFilterEditorControlPanel.children) {
                    if (child.id !== "tag-search-chip-set-container") {
                        controlPanelHeight -= child.offsetHeight;
                        controlPanelHeight -= child.marginTop ? child.marginTop : 0;
                        controlPanelHeight -= child.marginBottom ? child.marginBottom : 0;
                    }
                }
                const chipSetContainer = document.getElementById("tag-search-chip-set-container");
                chipSetContainer.style.height = controlPanelHeight + "px";
            }
        }
    );
    resizeObserver.observe(leftContainer);
}