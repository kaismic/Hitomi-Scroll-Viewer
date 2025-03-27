﻿/**
 * 
 * @param {string} targetValue
 * @param {('id'|'class')} targetValueType
 * @param {string} observeeValue
 * @param {('id'|'class')} observeeValueType
 */
function setFillHeightResizeObserver(targetValue, targetValueType, observeeValue, observeeValueType) {
    // set target's height to fill the remaining space of its parent
    const resizeObserver = new ResizeObserver(
        () => {
            const target = targetValueType == "id" ? document.getElementById(targetValue) : document.getElementsByClassName(targetValue)[0]
            if (!target) {
                return;
            }
            const parent = target.parentNode;
            if (!parent) {
                return;
            }
            const computedStyle = getComputedStyle(parent);
            let targetHeight = parent.clientHeight;
            targetHeight -= parseFloat(computedStyle.paddingTop) + parseFloat(computedStyle.paddingBottom); // remove padding
            const parsedGap = parseInt(computedStyle.gap)
            if (parsedGap) {
                targetHeight -= parsedGap * (parent.children.length - 1) // remove gap
            }
            for (const child of parent.children) {
                if (child !== target) {
                    targetHeight -= child.offsetHeight;
                    targetHeight -= child.marginTop ? child.marginTop : 0;
                    targetHeight -= child.marginBottom ? child.marginBottom : 0;
                }
            }
            target.style.height = targetHeight + "px";
        }
    );
    resizeObserver.observe(observeeValueType == "id" ? document.getElementById(observeeValue) : document.getElementsByClassName(observeeValue)[0]);
}