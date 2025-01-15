export function redirect(href, event) {
    let element = event.currentTarget;
    if (element)
        element.style.pointerEvents = 'none';
    window.location.href = href;
}
export function getCurrentFileName() {
    let fullPath = window.location.pathname;
    return fullPath.split("/").pop();
}
export function getCurrentSubdomain() {
    const currentHost = window.location.host;
    const parts = currentHost.split('.');
    if (parts.length <= 2) {
        return null;
    }
    if (parts[0] === 'localhost' || /^\d+$/.test(parts[0].replace(/\./g, ''))) {
        return null;
    }
    return parts[0];
}
export function getRootDomain() {
    const hostname = window.location.hostname;
    const parts = hostname.split(".");
    if (parts.length < 3 || parts.every(part => !isNaN(parseInt(part)))) {
        return null;
    }
    return parts.slice(-2).join(".");
}
export function addCookie(prop, value, expires = null) {
    let today = new Date();
    let expirationDate = new Date();
    if (expires !== null)
        expirationDate = expires;
    else
        expirationDate.setFullYear(today.getFullYear() + 1);
    document.cookie = `${prop}=${value}; path=/;${getRootDomain() ? `domain=.${getRootDomain()};` : ''}; expires=${expirationDate.toString()}`;
}
export function getCookie(prop) {
    const cookies = document.cookie.split("; ");
    for (let i = 0; i < cookies.length; i++) {
        const cookie = cookies[i].split("=");
        if (cookie[0] === prop) {
            return cookie[1];
        }
    }
    return null;
}
export function removeCookie(prop) {
    const cookies = document.cookie.split(";");
    cookies.forEach((cookie) => {
        const cookieParts = cookie.split("=");
        if (cookieParts[0].trim() === prop) {
            const cookieName = cookieParts[0].trim();
            const cookieDomain = `.${getRootDomain()}`;
            document.cookie = `${cookieName}=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;${getRootDomain() ? 'domain=' + getRootDomain() + ';' : ''}`;
        }
    });
}
export function setWebTheme(theme) {
    addCookie('webtheme', theme);
    const link = document.getElementById('webtheme-link');
    if (!link)
        return;
    link.href = `/css/themes/${theme.toLowerCase()}.css`;
}
export function toggleWebTheme() {
    const currentTheme = getCookie('webtheme');
    if (currentTheme === 'dark')
        setWebTheme('light');
    else
        setWebTheme('dark');
}
export function openModal(vue, modalId) {
    if (modalId === null) {
        const el = document.querySelector('.modal-' + vue.modalOpened);
        if (el) {
            el.classList.add("animation-fadeout-03");
            setTimeout(() => {
                vue.modalOpened = null;
                el.classList.remove("animation-fadeout-03");
            }, 300);
        }
        else {
            vue.modalOpened = null;
        }
        enableScroll();
        return;
    }
    disableScroll();
    vue.modalOpened = modalId;
}
export function deepClone(obj) {
    return JSON.parse(JSON.stringify(obj));
}
export function generateUUID() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'
        .replace(/[xy]/g, function (c) {
        const r = Math.random() * 16 | 0, v = c === 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}
export function scrollToElement(elementId) {
    const element = document.getElementById(elementId);
    element.scrollIntoView({ behavior: "smooth" });
}
const keys = { 37: 1, 38: 1, 39: 1, 40: 1 };
function preventDefaultForScrollKeys(e) {
    if (keys[e.keyCode]) {
        preventDefault(e);
        return false;
    }
}
let supportsPassive = false;
try {
    window.addEventListener("test", null, Object.defineProperty({}, 'passive', {
        get: function () { supportsPassive = true; }
    }));
}
catch (e) { }
let wheelOpt = supportsPassive ? { passive: false } : false;
let wheelEvent = 'onwheel' in document.createElement('div') ? 'wheel' : 'mousewheel';
function preventDefault(e) {
    e.preventDefault();
}
export function disableScroll() {
    window.addEventListener('DOMMouseScroll', preventDefault, false);
    window.addEventListener(wheelEvent, preventDefault, wheelOpt);
    window.addEventListener('touchmove', preventDefault, wheelOpt);
    window.addEventListener('keydown', preventDefaultForScrollKeys, false);
}
export function enableScroll() {
    window.removeEventListener('DOMMouseScroll', preventDefault, false);
    window.removeEventListener(wheelEvent, preventDefault, wheelOpt);
    window.removeEventListener('touchmove', preventDefault, wheelOpt);
    window.removeEventListener('keydown', preventDefaultForScrollKeys, false);
}
window.addCookie = addCookie;
