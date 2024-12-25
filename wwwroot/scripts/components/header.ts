function sdfdsfoisdj() {
    const header = document.querySelector('header') as HTMLElement;
    if(header === null) return;

    if(window.scrollY > 0) {
        header.classList.add('scrolled');
    } else header.classList.remove('scrolled');
}

window.onscroll = sdfdsfoisdj;
window.onload = sdfdsfoisdj;