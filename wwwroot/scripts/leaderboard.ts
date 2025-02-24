document.addEventListener("DOMContentLoaded", () => {
    const leaderboardTable = document.querySelector("#leaderboard tbody");

    if (!leaderboardTable) {
        console.error("Tabulka leaderboard nebyla nalezena!");
        return;
    }

    const apiUrl: string = "/api/v2/leaderboard";

    interface Player {
        name: string;
        elo: number;
    }

    fetch(apiUrl)
        .then(response => response.json())
        .then((data: Player[]) => {
            leaderboardTable.innerHTML = "";

            data.forEach((player: Player, index: number) => {
                const row: HTMLTableRowElement = document.createElement("tr");

                row.innerHTML = `
                    <td>${index + 1}</td>
                    <td>${player.name}</td>
                    <td>${player.elo}</td>
                `;

                leaderboardTable.appendChild(row);
            });
        })
        .catch((error: Error) => console.error("Chyba při načítání dat:", error));
});
