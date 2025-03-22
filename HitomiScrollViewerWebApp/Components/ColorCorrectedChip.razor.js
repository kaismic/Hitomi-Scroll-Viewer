export function correctChipVariantClass(id) {
    // gotta wait for a bit to let MudChip's click handler to modify class
    setTimeout(
        () => {
            const mudChip = document.getElementById(id);
            mudChip.className = mudChip.className.replace("mud-chip-text", "mud-chip-filled");
        },
        20
    )
}