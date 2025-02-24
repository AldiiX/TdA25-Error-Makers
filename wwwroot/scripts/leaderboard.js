"use strict";
document.addEventListener("DOMContentLoaded", () => {
    const leaderboardTable = document.querySelector("#leaderboard tbody");
    if (!leaderboardTable) {
        console.error("Tabulka leaderboard nebyla nalezena!");
        return;
    }
    const apiUrl = "/api/v2/leaderboard";
    fetch(apiUrl)
        .then(response => response.json())
        .then((data) => {
        leaderboardTable.innerHTML = "";
        data.forEach((player, index) => {
            const row = document.createElement("tr");
            row.innerHTML = `
                    <td>${index + 1}</td>
                    <td>${player.name}</td>
                    <td>${player.elo}</td>
                `;
            leaderboardTable.appendChild(row);
        });
    })
        .catch((error) => console.error("Chyba při načítání dat:", error));
});
