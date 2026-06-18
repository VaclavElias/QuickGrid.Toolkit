export function setColumnTitles(tableId, titles) {
    const table = document.getElementById(tableId);

    if (!table) return;

    const headers = table.querySelectorAll('th');

    headers.forEach((th, index) => {
        if (titles[index]) {
            th.setAttribute('title', titles[index]);
        }
    });
}

export function addOrUpdateFooter(tableId, footer) {
    const table = document.getElementById(tableId);

    if (!table) return;

    let tfoot = table.querySelector('tfoot');

    if (!tfoot) {
        tfoot = document.createElement('tfoot');
        table.appendChild(tfoot);
    }

    tfoot.innerHTML = footer;
}