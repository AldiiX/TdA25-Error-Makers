"use strict";
window.onscroll = function () {
    const header = document.querySelector('header');
    if (header === null)
        return;
    if (window.scrollY > 0) {
        header.classList.add('ontop');
    }
    else
        header.classList.remove('ontop');
};
