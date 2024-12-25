﻿using System.Text.Json;

namespace TdA25_Error_Makers.Classes.Objects;





public class GameBoard {

    // propy
    private string[,] Board { get; set; }

    private HashSet<(int row, int col)>? WinningCells { get; set; }

    public enum Player { X, O }



    // konstruktory
    private GameBoard(string[,] board) {
        Board = board;
    }

    private GameBoard(List<List<string>> board) {
        Board = new string[15, 15];

        for (int row = 0; row < 15; row++) {
            for (int col = 0; col < 15; col++) {
                Board[row, col] = board[row][col];
            }
        }
    }

    private GameBoard(string boardJson) {
        List<List<string>>? deserializedBoard = null;
        deserializedBoard = JsonSerializer.Deserialize<List<List<string>>>(boardJson);

        if (deserializedBoard == null) {
            Board = new string[15, 15];
            InitializeBoard();
            return;
        }

        var board = new string[15, 15];
        for (int row = 0; row < 15; row++) {
            for (int col = 0; col < 15; col++) {
                board[row, col] = deserializedBoard[row][col];
            }
        }

        Board = board;
    }

    private GameBoard() {
        byte size = 15;
        Board = new string[size, size];
        InitializeBoard();
    }



    // statické metody
    public static GameBoard Parse(string[,] board) => new(board);

    public static GameBoard Parse(List<List<string>> board) => new(board);

    public static GameBoard Parse(string board) => new(board);

    public static bool TryParse(string? board, out GameBoard gameBoard) {
        if (board == null) {
            gameBoard = new GameBoard();
            return false;
        }

        try {
            gameBoard = new GameBoard(board);
            return true;
        } catch {
            gameBoard = new GameBoard();
            return false;
        }
    }

    public static bool TryParse(string[,] board, out GameBoard gameBoard) {
        try {
            gameBoard = new GameBoard(board);
            return true;
        } catch {
            gameBoard = new GameBoard();
            return false;
        }
    }

    public static bool TryParse(List<List<string>> board, out GameBoard gameBoard) {
        try {
            gameBoard = new GameBoard(board);
            return true;
        } catch {
            gameBoard = new GameBoard();
            return false;
        }
    }

    public static GameBoard CreateNew() => new();



    // instance metody
    public override string ToString() {
        var boardToList = new List<List<string>>();
        for (int row = 0; row < 15; row++) {
            var rowList = new List<string>();
            for (int col = 0; col < 15; col++) {
                rowList.Add(Board[row, col]);
            }

            boardToList.Add(rowList);
        }

        return JsonSerializer.Serialize(boardToList);
    }

    public ushort GetRound() {
        ushort round = 1;

        for (int row = 0; row < 15; row++) {
            for (int col = 0; col < 15; col++) {
                if (Board[row, col] != "") {
                    round++;
                }
            }
        }

        return (ushort)(round - 1);
    }

    public bool IsValid() {
        var bl = this.ToList();
        bool sumCheck = true;
        bool boardSizeCheck = true;
        bool validValues = true;


        // kontrola počtu X a O
        int xCount = 0;
        int oCount = 0;
        for (int row = 0; row < 15; row++) {
            for (int col = 0; col < 15; col++) {
                if (Board[row, col] == "X") {
                    xCount++;
                } else if (Board[row, col] == "O") {
                    oCount++;
                }
            }
        }

        // kontrola hodnot
        for (int row = 0; row < 15; row++) {
            for (int col = 0; col < 15; col++) {
                if (Board[row, col] != "" && Board[row, col] != "X" && Board[row, col] != "O") {
                    validValues = false;
                    break;
                }
            }
        }



        sumCheck = xCount == oCount || xCount == oCount + 1;
        boardSizeCheck = bl.Count == 15 && bl.Any(row => row.Count == 15);


        return sumCheck && boardSizeCheck && validValues;
    }

    /**
     * metoda, která nastaví všechny hodnoty na "" (prázdné)
     */
    private void InitializeBoard() {
        for (int row = 0; row < 15; row++) {
            for (int col = 0; col < 15; col++) {
                Board[row, col] = "";
            }
        }
    }

    /**
     * metoda, která nastaví všechny hodnoty na "" (prázdné);
     * alias pro InitializeBoard()
     */
    private void ResetBoard() { InitializeBoard(); }

    public List<List<string>> ToList() {
        var boardToList = new List<List<string>>();
        for (int row = 0; row < 15; row++) {
            var rowList = new List<string>();
            for (int col = 0; col < 15; col++) {
                rowList.Add(Board[row, col]);
            }

            boardToList.Add(rowList);
        }

        return boardToList;
    }

    public Game.GameState GetGameState() {
        if (CheckIfSomeoneWon() != null) return Game.GameState.FINISHED;
        if (CheckIfSomeoneCanWin() != null) return Game.GameState.ENDGAME;

        return GetRound() > 5 ? Game.GameState.MIDGAME : Game.GameState.OPENING;
    }

    public Player? GetWinner() => CheckIfSomeoneWon();

    public HashSet<(int row, int col)>? GetWinningCells() => WinningCells != null ? [..WinningCells] : null;

    /**
     * metoda na zjištění, kdo je na tahu (vždy začíná X)
     */
    public Player GetNextPlayer() {
        int xCount = 0;
        int oCount = 0;
        for (int row = 0; row < 15; row++) {
            for (int col = 0; col < 15; col++) {
                if (Board[row, col] == "X") {
                    xCount++;
                } else if (Board[row, col] == "O") {
                    oCount++;
                }
            }
        }

        return xCount == oCount ? Player.X : Player.O;
    }

    public Player? CheckIfSomeoneWon() {
        for (int row = 0; row < 15; row++) {
            for (int col = 0; col < 15; col++) {
                if (Board[row, col] != "") {
                    WinningCells = new HashSet<(int, int)>();

                    if (CheckHorizontal(row, col) ||
                        CheckVertical(row, col) ||
                        CheckDiagonal(row, col)) {
                        return (Board[row, col] == "X" ? Player.X : Player.O);
                    }

                    WinningCells = null;
                }
            }
        }

        return null;
    }

    public Player? CheckIfSomeoneCanWin() {
        for (int row = 0; row < 15; row++) {
            for (int col = 0; col < 15; col++) {
                if (Board[row, col] != "") continue;

                // simulace tahu aktuálního hráče
                Player? currentPlayer = GetNextPlayer();

                switch (currentPlayer) {
                    case Player.X: {
                        // simulace X
                        Board[row, col] = "X";
                        if (CheckIfSomeoneWon() == Player.X) {
                            Board[row, col] = "";
                            return Player.X;
                        }

                        break;
                    }
                    case Player.O: {
                        // simulace O
                        Board[row, col] = "O";
                        if (CheckIfSomeoneWon() == Player.O) {
                            Board[row, col] = "";
                            return Player.O;
                        }

                        break;
                    }
                }

                Board[row, col] = "";
            }
        }

        return null;
    }

    private bool CheckHorizontal(int row, int col) {
        if (col + 4 >= 15) return false;

        var tempWinningCells = new HashSet<(int, int)>();

        for (int i = 0; i < 5; i++) {
            if (Board[row, col + i] != Board[row, col] || Board[row, col] == "") {
                return false;
            }

            tempWinningCells.Add((row, col + i));
        }

        WinningCells = tempWinningCells;
        return true;
    }

    private bool CheckVertical(int row, int col) {
        if (row + 4 >= 15) return false;

        var tempWinningCells = new HashSet<(int, int)>();

        for (int i = 0; i < 5; i++) {
            if (Board[row + i, col] != Board[row, col] || Board[row, col] == "") {
                return false;
            }

            tempWinningCells.Add((row + i, col));
        }

        WinningCells = tempWinningCells;
        return true;
    }

    private bool CheckDiagonal(int row, int col) {
        // 1. diagonální směr (\)
        if (row + 4 < 15 && col + 4 < 15) {
            var tempWinningCells = new HashSet<(int row, int col)>();
            bool diagonalMatch1 = true;

            for (int i = 0; i < 5; i++) {
                if (Board[row + i, col + i] != Board[row, col] || Board[row, col] == "") {
                    diagonalMatch1 = false;
                    break;
                }

                tempWinningCells.Add((row + i, col + i));
            }

            if (diagonalMatch1) {
                WinningCells = tempWinningCells;
                return true;
            }
        }

        // 2. diagonální směr (/)
        if (row + 4 < 15 && col - 4 >= 0) {
            var tempWinningCells = new HashSet<(int, int)>();
            bool diagonalMatch2 = true;

            for (int i = 0; i < 5; i++) {
                if (Board[row + i, col - i] != Board[row, col] || Board[row, col] == "") {
                    diagonalMatch2 = false;
                    break;
                }

                tempWinningCells.Add((row + i, col - i));
            }

            if (diagonalMatch2) {
                WinningCells = tempWinningCells;
                return true;
            }
        }

        return false;
    }
}