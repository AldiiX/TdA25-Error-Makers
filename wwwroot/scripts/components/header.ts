function sdfdsfoisdj() {
    const header = document.querySelector('header') as HTMLElement;
    if(header === null) return;

    if(window.scrollY > 0) {
        header.classList.add('ontop');
    } else header.classList.remove('ontop');
}

window.onscroll = sdfdsfoisdj;
window.onload = sdfdsfoisdj;