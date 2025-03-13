export function setChipClass(id) {
    // gotta wait for a bit to let MudChip's click handler to modify class
    setTimeout(
        () => {
            const mudChip = document.getElementById(id)
            if (mudChip.className.endsWith("mud-chip-selected")) {
                mudChip.className = mudChip.className.replace("mud-chip-text", "mud-chip-filled");
            }
        },
        20
    )
}