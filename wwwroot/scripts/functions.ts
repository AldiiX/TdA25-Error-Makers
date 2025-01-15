interface Vue {}

export function redirect(href: string, event: Event): void {
    let element: HTMLElement = event.currentTarget as HTMLElement;
    if(element) element.style.pointerEvents = 'none';

    window.location.href = href;
}

export function getCurrentFileName(): string|undefined {
    let fullPath = window.location.pathname;
    return fullPath.split("/").pop();
}

export function getCurrentSubdomain(): string|null {
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

export function getRootDomain(): string|null {
    const hostname = window.location.hostname;
    const parts = hostname.split(".");

    // Pokud adresa obsahuje méně než dvě tečky, nebo je to IP adresa, není to pod doménou
    if (parts.length < 3 || parts.every(part => !isNaN(parseInt(part)))) {
        return null;
    }

    return parts.slice(-2).join(".");
}

export function addCookie(prop: string, value: string|number, expires: Date|null = null): void {
    let today = new Date();
    let expirationDate = new Date();

    if(expires !== null) expirationDate = expires;
    else expirationDate.setFullYear(today.getFullYear() + 1);


    document.cookie = `${prop}=${value}; path=/;${getRootDomain() ? `domain=.${getRootDomain()};` : ''}; expires=${expirationDate.toString()}`;
}

export function getCookie(prop: string): string|null {
    const cookies = document.cookie.split("; ");
    for (let i = 0; i < cookies.length; i++) {
        const cookie = cookies[i].split("=");
        if (cookie[0] === prop) {
            return cookie[1];
        }
    }

    return null;
}

export function removeCookie(prop: string): void {
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

export function addAnnouncement(vue: any, text: string, type: string = 'info', timeout = 5000) {
    /*
    *
    * Vyžaduje frontend ve vue, kde je nutné, aby vue.annoucements bylo []
    *
    * */

    const announcement = { text: text, id: ("a" + generateUUID()) };
    const annParent = document.getElementById('announcements');
    if(annParent == null) return;

    vue.announcements.push(announcement);

    // vytvoření divu s anouncmentem
    const announcementDiv = document.createElement('div');
    announcementDiv.className = type;
    announcementDiv.id = announcement.id;
    announcementDiv.innerText = announcement.text;
    annParent.appendChild(announcementDiv);

    // odebrání anouncementu po 5s
    setTimeout(() => {
        (document.querySelector(`#announcements #${announcement.id}`) as HTMLElement).classList.add('fade-out');

        setTimeout(() => {
            annParent.removeChild(document.getElementById(announcement.id) as HTMLElement);
            vue.announcements.filter((ann: any) => ann.id !== announcement.id);
        }, 500);
    }, timeout);
}

export function setWebTheme(theme: string): void {
    addCookie('webtheme', theme);
    const link = document.getElementById('webtheme-link') as HTMLLinkElement;
    if(!link) return;

    link.href = `/css/themes/${theme.toLowerCase()}.css`;
}

export function toggleWebTheme(): void {
    const currentTheme = getCookie('webtheme');
    if(currentTheme === 'dark') setWebTheme('light');
    else setWebTheme('dark');
}

export function openModal(vue: Vue|any, modalId: string|null): void {
    if(modalId === null) {
        const el = document.querySelector('.modal-' + vue.modalOpened);

        if(el) {
            el.classList.add("animation-fadeout-03");
            setTimeout(() => {
                vue.modalOpened = null;
                el.classList.remove("animation-fadeout-03");
            }, 300);
        } else {
            vue.modalOpened = null;
        }

        enableScroll();
        return;
    }

    disableScroll();
    vue.modalOpened = modalId;
}

export function deepClone(obj: any): any {
    return JSON.parse(JSON.stringify(obj));
}

export function generateUUID(): string {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'
        .replace(/[xy]/g, function (c) {
            const r = Math.random() * 16 | 0,
                v = c === 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
}

export function scrollToElement(elementId: string): void {
    const element: HTMLElement = document.getElementById(elementId) as HTMLElement;
    element.scrollIntoView({behavior: "smooth"});
}

















// region prevent scrolling
const keys: any = {37: 1, 38: 1, 39: 1, 40: 1};

function preventDefaultForScrollKeys(e: any) {
    if (keys[e.keyCode]) {
        preventDefault(e);
        return false;
    }
}

let supportsPassive = false;
try {
    // @ts-ignore
    window.addEventListener("test", null, Object.defineProperty({}, 'passive', {
        get: function () { supportsPassive = true; }
    }));
} catch(e) {}

let wheelOpt = supportsPassive ? { passive: false } : false;
let wheelEvent = 'onwheel' in document.createElement('div') ? 'wheel' : 'mousewheel';

function preventDefault(e: any|Event) {
    e.preventDefault();
}
export function disableScroll() {
    window.addEventListener('DOMMouseScroll', preventDefault, false); // older FF
    window.addEventListener(wheelEvent, preventDefault, wheelOpt); // modern desktop
    window.addEventListener('touchmove', preventDefault, wheelOpt); // mobile
    window.addEventListener('keydown', preventDefaultForScrollKeys, false);
}

export function enableScroll() {
    window.removeEventListener('DOMMouseScroll', preventDefault, false);
    // @ts-ignore
    window.removeEventListener(wheelEvent, preventDefault, wheelOpt);
    // @ts-ignore
    window.removeEventListener('touchmove', preventDefault, wheelOpt);
    window.removeEventListener('keydown', preventDefaultForScrollKeys, false);
}
// endregion














(window as any).addCookie = addCookie;