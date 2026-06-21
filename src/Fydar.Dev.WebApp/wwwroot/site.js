
queueMicrotask(console.log.bind(console, "%c   ___             __\n /\'___\\           /\\ \\\n/\\ \\__/  __  __   \\_\\ \\     __     _ __\n\\ \\ ,__\\/\\ \\/\\ \\  /\'_` \\  /\'__`\\  /\\`\'__\\\n \\ \\ \\_/\\ \\ \\_\\ \\/\\ \\L\\ \\/\\ \\L\\.\\_\\ \\ \\/\n  \\ \\_\\  \\/`____ \\ \\___,_\\ \\__/.\\_\\\\ \\_\\\n   \\/_/   `/___/> \\/__,_ /\\/__/\\/_/ \\/_/\n             /\\___/\n             \\/__/", "font-family: monospace; white-space: nowrap"));
queueMicrotask(console.log.bind(console, "This website has been made using ASP.NET Core and Blazor.\nLike what you see? I\'m available for hire!\n\nhttps://fydar.dev/contact"));


const clamp = (a, min = 0, max = 1) => Math.min(max, Math.max(min, a));
const invlerp = (x, y, a) => (a - x) / (y - x);
function lerp(start, end, amt) {
    return (1 - amt) * start + amt * end
}

function UpdateParallaxLayerSize() {
    var elements = document.getElementsByClassName("parallax-layer");
    for (let i = 0; i < elements.length; i++) {
        var element = elements[i];

        var containerRect = element.parentElement.parentElement.getBoundingClientRect();
        element.style.width = containerRect.width + "px";
        element.style.height = containerRect.height + "px";
    }
}
function UpdateRelativeElements() {
    var elements = document.getElementsByClassName("parallax-focalanchortop");
    for (let i = 0; i < elements.length; i++) {
        var element = elements[i];
        var clientRect = element.getBoundingClientRect()
        var time = clamp(invlerp(clientRect.height, -clientRect.height, clientRect.top), 0, 1);
        element.style.setProperty("--animation-time", time.toFixed(5));
        element.style.setProperty("--parallax-offset", lerp(-clientRect.height, clientRect.height, time).toFixed(1));
    }
}

UpdateParallaxLayerSize();
UpdateRelativeElements();

window.addEventListener("scroll",
    eventArgs => {
        UpdateRelativeElements();
    }, { passive: true }
);

window.addEventListener("resize",
    eventArgs => {
        UpdateParallaxLayerSize();
        UpdateRelativeElements();
    }, { passive: true }
);



function createStylesheet() {
    const style = document.createElement('style');
    document.head.appendChild(style);
    return style.sheet;
}

function addCSSRule(stylesheet, selector, styles) {
    if (stylesheet.insertRule) { // Modern browsers
        stylesheet.insertRule(`${selector} { ${styles} }`, stylesheet.cssRules.length);
    } else if (stylesheet.addRule) { // Older IE (rarely needed now)
        stylesheet.addRule(selector, styles, -1);
    } else {
        console.error("Adding CSS rule not supported.");
    }
}

function findCSSRule(stylesheet, selector) {
    const rules = stylesheet.cssRules || stylesheet.rules;
    for (let i = 0; i < rules.length; i++) {
        const rule = rules[i];
        if (rule instanceof CSSStyleRule && rule.selectorText === selector) {
            return rule;
        }
    }
    return null;
}

var proceduralStylesheet = createStylesheet();
addCSSRule(proceduralStylesheet, ".card", "--pointer-fixed: 0px 0px;");
const rule = findCSSRule(proceduralStylesheet, ".card");

var lastX = 0;
var lastY = 0;

window.addEventListener("pointermove",
    eventArgs => {
        lastX = eventArgs.clientX;
        lastY = eventArgs.clientY;

        rule.style.setProperty("--pointer-fixed", lastX.toFixed(2) + "px " + lastY.toFixed(2) + "px");
    }, { passive: true }
);

window.document.addEventListener("pointerleave",
    eventArgs => {
        var pointerRelativeElements = document.getElementsByClassName("card");

        for (let i = 0; i < pointerRelativeElements.length; i++) {
            var pointerRelativeElement = pointerRelativeElements[i];
            pointerRelativeElement.classList.add("pointer-none");
        }
    }, { passive: true }
);

window.document.addEventListener("pointerenter",
    eventArgs => {
        var pointerRelativeElements = document.getElementsByClassName("card");

        for (let i = 0; i < pointerRelativeElements.length; i++) {
            var pointerRelativeElement = pointerRelativeElements[i];
            pointerRelativeElement.classList.remove("pointer-none");
        }
    }, { passive: true }
);




let sectionCache = [];
let visibleMenus = [];
const menus = document.querySelectorAll("menu.toc");
const mq = window.matchMedia("(min-width: 992px)");

// Throttle/Debounce helpers
let scrollTick = false;
let resizeTimeout;
let isCacheBuilt = false;

function clearHighlighting() {
    menus.forEach(menu => {
        menu.querySelectorAll("li > a.active").forEach(a => a.classList.remove("active"));
    });
}

function BuildCache() {
    if (window.scrollY === 0) return;

    sectionCache = [];
    visibleMenus = [];
    menus.forEach(menu => {
        if (menu.offsetParent !== null) visibleMenus.push(menu);
    });

    if (visibleMenus.length === 0) return;

    const links = visibleMenus[0].querySelectorAll("li > a");
    const scrollTop = window.scrollY || document.documentElement.scrollTop;

    links.forEach(link => {
        const id = link.hash ? link.hash.substring(1) : null;
        const section = id ? document.getElementById(id) : null;
        if (section) {
            const rect = section.getBoundingClientRect();
            const absoluteTop = rect.top + scrollTop;
            sectionCache.push({
                id: id,
                top: absoluteTop,
                bottom: rect.bottom + scrollTop,
                center: absoluteTop + (rect.height / 2)
            });
        }
    });

    isCacheBuilt = true;
}

function NavHighlighter() {
    if (sectionCache.length === 0 || visibleMenus.length === 0) return;

    const scrollY = window.scrollY;
    const viewportHeight = window.innerHeight;
    const viewportCenter = scrollY + (viewportHeight * 0.333);

    let closestId = null;
    let minDistance = Infinity;

    sectionCache.forEach((data) => {
        const distance = Math.abs(viewportCenter - data.center);
        if (distance < minDistance) {
            minDistance = distance;
            closestId = data.id;
        }
    });

    // Out of bounds check
    if (scrollY < (sectionCache[0].top - (viewportHeight * 0.333)) ||
        (scrollY + viewportHeight) > (sectionCache[sectionCache.length - 1].bottom + (viewportHeight * 0.333))) {
        closestId = null;
    }

    visibleMenus.forEach(menu => {
        menu.querySelectorAll("li > a").forEach(link => {
            const linkId = link.hash ? link.hash.substring(1) : null;
            const shouldBeActive = linkId !== null && linkId === closestId;
            link.classList.toggle("active", shouldBeActive);
        });
    });
}

// Event Handlers
const handleScroll = () => {
    if (!scrollTick) {
        window.requestAnimationFrame(() => {
            if (window.scrollY === 0) {
                clearHighlighting();
                isCacheBuilt = false; // Invalidate cache so it rebuilds on next scroll down
                sectionCache = [];
            } else {
                if (!isCacheBuilt) {
                    BuildCache(); // Lazy execution on first scroll pixel
                }
                NavHighlighter();
            }
            scrollTick = false;
        });
        scrollTick = true;
    }
};

const handleResize = () => {
    clearTimeout(resizeTimeout);
    resizeTimeout = setTimeout(() => {
        if (window.scrollY > 0) {
            BuildCache();
            NavHighlighter();
        } else {
            // Keep state clean if resized at the top of the page
            isCacheBuilt = false;
            sectionCache = [];
        }
    }, 150);
};

function beginNavigationHighlighting() {

    mq.addEventListener("change", updateState);

    updateState();
}

function updateState() {
    if (mq.matches) {
        // Enable
        window.addEventListener("scroll", handleScroll, { passive: true });
        window.addEventListener("resize", handleResize, { passive: true });

        // Handle scenario where page is refreshed while already scrolled down
        if (window.scrollY > 0) {
            BuildCache();
            NavHighlighter();
        }
    } else {
        clearTimeout(resizeTimeout);

        // Disable
        window.removeEventListener("scroll", handleScroll);
        window.removeEventListener("resize", handleResize);

        clearHighlighting();

        sectionCache = [];
        visibleMenus = [];
        isCacheBuilt = false;
    }
}

const delayedUpdate = () => setTimeout(beginNavigationHighlighting, 250);

if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", delayedUpdate);
} else {
    delayedUpdate();
}



function GraphScaler() {
    var elements = document.getElementsByClassName("graph");
    for (let i = 0; i < elements.length; i++) {
        var element = elements[i];
        var clientRect = element.getBoundingClientRect()

        var targetSize = window.getComputedStyle(element).getPropertyValue("--Lodestone-graph-targetsize");
        element.style.setProperty("--Lodestone-graph-scale", clientRect.width / targetSize);
    }
}

GraphScaler();

window.addEventListener("resize",
    eventArgs => {
        GraphScaler();
    }, { passive: true }
);


const getRequiredModifiers = () => {
    const userAgent = navigator.userAgent.toLowerCase();
    const isMac = navigator.platform.toLowerCase().includes('mac');

    // Check for Firefox: Modern browsers use userAgentData, 
    // but Firefox doesn't support it yet, so we check the string.
    const isFirefox = userAgent.includes('firefox') || userAgent.includes('fxios');

    return {
        requiresCtrl: isMac,
        requiresAlt: true,
        // Firefox on Windows/Linux requires Shift. On Mac, it uses Ctrl+Opt.
        requiresShift: isFirefox && !isMac
    };
};

document.addEventListener('DOMContentLoaded', () => {
    // Is Access Keys supported by the browser? If so, use that implemention for Hyperlink activation.
    if (!('accessKey' in document.createElement('a'))) return;

    const config = getRequiredModifiers();

    // Initialize state from sessionStorage (or default to false)
    const savedState = JSON.parse(sessionStorage.getItem('accessKeyState')) || {};
    let keysPressed = {
        Alt: savedState.Alt || false,
        Control: false,
        Shift: savedState.Shift || false
    };

    const altCondition = keysPressed.Alt;
    const ctrlCondition = config.requiresCtrl ? keysPressed.Control : true;
    const shiftCondition = config.requiresShift ? keysPressed.Shift : true;

    const shouldShow = altCondition && ctrlCondition && shiftCondition;

    document.querySelectorAll('.access-hint').forEach(hint => {
        if (shouldShow) {
            hint.classList.toggle('show', true);
            hint.classList.toggle('no-transition', true);
        }
    });

    const updateHintVisibility = () => {
        const altCondition = keysPressed.Alt;
        const ctrlCondition = config.requiresCtrl ? keysPressed.Control : true;
        const shiftCondition = config.requiresShift ? keysPressed.Shift : true;

        const shouldShow = altCondition && ctrlCondition && shiftCondition;

        document.querySelectorAll('.access-hint').forEach(hint => {
            hint.classList.toggle('show', shouldShow);
            hint.classList.toggle('no-transition', false);
        });

        // Save state for the next page load.
        sessionStorage.setItem('accessKeyState', JSON.stringify({
            Alt: keysPressed.Alt,
            Shift: keysPressed.Shift
        }));
    };

    const syncKeys = (e) => {
        if (!e || typeof e.getModifierState !== 'function') return;

        keysPressed.Alt = e.getModifierState("Alt");
        keysPressed.Control = e.getModifierState("Control");
        keysPressed.Shift = e.getModifierState("Shift");

        updateHintVisibility();
    };

    // High-frequency syncs (only once to catch initial state)
    window.addEventListener('mousemove', syncKeys, { once: true });
    window.addEventListener('pointermove', syncKeys, { once: true });

    // Interaction syncs
    window.addEventListener('mousedown', syncKeys);
    window.addEventListener('keydown', syncKeys);
    window.addEventListener('keyup', syncKeys);
    window.addEventListener('contextmenu', syncKeys);

    // Navigation/Tab syncs
    window.addEventListener('focus', syncKeys);
    window.addEventListener('pageshow', syncKeys);

    // Reset on blur to prevent "stuck" keys
    window.addEventListener('blur', () => {
        keysPressed = { Alt: false, Control: false, Shift: false };
        updateHintVisibility();
    });
});
