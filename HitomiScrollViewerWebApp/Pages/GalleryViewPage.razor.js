const MAIN_CONTAINER_ID = "main-container";
const SCROLL_IMAGE_CONTAINER_ID = "scroll-image-container";
let scrollDistance = 1;
let scrollInterval = 8000;
let autoScrollMode = 0;
let loop = false;
let intervalId = null;
let dotNetObject = null;

export function setDotNetObject(obj) { dotNetObject = obj; }
export function setAutoScrollMode(value) { autoScrollMode = value; }
export function setScrollInterval(value) {
    scrollInterval = value;
    if (intervalId) {
        startAutoScroll();
    }
}
export function setScrollDistance(value) { scrollDistance = value; }
export function setLoop(value) { loop = value; }

export function startAutoScroll() {
    stopAutoScroll();
    switch (autoScrollMode) {
        // continuous
        case 0:
            intervalId = setInterval(
                () => {
                    scrollBy(scrollDistance);
                },
                8 // milliseconds
            );
            break;
        // by page
        case 1:
            intervalId = setInterval(
                () => {
                    scrollToNextPage();
                },
                scrollInterval
            );
            break;
    }
}

export function stopAutoScroll() {
    if (intervalId) {
        clearInterval(intervalId);
        intervalId = null;
    }
}

/**
 * 
 * @param {number} delta
 */
function scrollBy(delta) {
    const mainContainer = document.getElementById(MAIN_CONTAINER_ID);
    // copied from: https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollHeight#determine_if_an_element_has_been_totally_scrolled
    const scrollReachedBottom = Math.abs(mainContainer.scrollHeight - mainContainer.clientHeight - mainContainer.scrollTop) <= 1;
    if (scrollReachedBottom()) {
        if (loop) {
            mainContainer.scrollTop = 0;
        } else {
            stopAutoScroll();
            dotNetObject.invokeMethodAsync("OnAutoScrollStop");
        }
    } else {
        mainContainer.scrollBy(0, delta);
    }
}

/**
 * @returns {number}
 */
function getCurrentImageIndex() {
    const scrollImageContainer = document.getElementById(SCROLL_IMAGE_CONTAINER_ID);
    // TODO implement calculation
}

function scrollToNextPage() {
    const scrollImageContainer = document.getElementById(SCROLL_IMAGE_CONTAINER_ID);
    const nextIndex = getCurrentImageIndex() + 1;
    if (nextIndex >= scrollImageContainer.childElementCount) {
        if (loop) {
            scrollImageContainer.scrollTo(0, 0);
        } else {
            stopAutoScroll();
            dotNetObject.invokeMethodAsync("OnAutoScrollStop");
        }
    } else {
        scrollToIndex(nextIndex);
    }
}

export function scrollToIndex(index) {
    const scrollImageContainer = document.getElementById(SCROLL_IMAGE_CONTAINER_ID);
    if (index < 0 || index >= scrollImageContainer.childElementCount) {
        return;
    }
    const item = scrollImageContainer.children[index];
    if (item) {
        item.scrollIntoView({ behavior: "smooth", block: "center" });
    }
}