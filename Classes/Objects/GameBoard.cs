using System.Text.Json;

namespace TdA25_Error_Makers.Classes.Objects;





public class GameBoard {

    private string[,] Board { get; set; }
    public enum Player { X, O }
    //private byte Size { get; set; } = 15;

    public GameBoard(string[,] board) {
        Board = board;
    }

    public GameBoard(List<List<string>> board) {
        Board = new string[15, 15];

        for (int row = 0; row < 15; row++) {
            for (int col = 0; col < 15; col++) {
                Board[row, col] = board[row][col];
            }
        }
    }

    public GameBoard(string? boardJson) {
        var deserializedBoard = JsonSerializer.Deserialize<List<List<string>>>(boardJson);
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

    public GameBoard(byte size = 15) {
        Board = new string[size, size];
        //Size = size;
        InitializeBoard();
    }

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

        return round;
    }

    public bool ValidateBoard() {
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
        boardSizeCheck = Board.GetLength(0) == 15 && Board.GetLength(1) == 15;


        return sumCheck && boardSizeCheck && validValues;
    }

    private void InitializeBoard() {
        for (int row = 0; row < 15; row++)
        {
            for (int col = 0; col < 15; col++)
            {
                Board[row, col] = "";
            }
        }
    }



    /*public void SetCell(int row, int col, string value) {
        if(value != "X" && value != "O" && value != "") {
            throw new ArgumentOutOfRangeException("Invalid value.");
        }

        if (!IsValidPosition(row, col)) {
            throw new ArgumentOutOfRangeException("Invalid board position.");
        }

        Board[row, col] = value;
    }

    public string GetCell(int row, int col) {
        if (IsValidPosition(row, col)) {
            return Board[row, col];
        }

        throw new ArgumentOutOfRangeException("Invalid board position.");
    }*/

    private bool IsValidPosition(int row, int col) {
        return row >= 0 && row < 15 && col >= 0 && col < 15;
    }

    public void ResetBoard() {
        InitializeBoard();
    }

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
                    if (CheckHorizontal(row, col) || CheckVertical(row, col) || CheckDiagonal(row, col)) {
                        return Board[row, col] == "X" ? Player.X : Player.O;
                    }
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

                // reset pole
                Board[row, col] = "";
            }
        }

        return null;
    }


    private bool CheckWin(int row, int col) => CheckHorizontal(row, col) || CheckVertical(row, col) || CheckDiagonal(row, col);

    private bool CheckHorizontal(int row, int col) {
        if (col + 4 >= 15) return false;
        for (int i = 0; i < 5; i++) {
            if (Board[row, col + i] != Board[row, col] || Board[row, col] == "") {
                return false;
            }
        }
        return true;
    }

    private bool CheckVertical(int row, int col) {
        if (row + 4 >= 15) return false;
        for (int i = 0; i < 5; i++) {
            if (Board[row + i, col] != Board[row, col] || Board[row, col] == "") {
                return false;
            }
        }
        return true;
    }

    private bool CheckDiagonal(int row, int col) {
        // 1. diagonální směr (\)
        if (row + 4 < 15 && col + 4 < 15) {
            bool diagonalMatch1 = true;
            for (int i = 0; i < 5; i++) {
                if (Board[row + i, col + i] != Board[row, col] || Board[row, col] == "") {
                    diagonalMatch1 = false;
                    break;
                }
            }
            if (diagonalMatch1) return true;
        }

        // 2. diagonální směr (/)
        if (row + 4 < 15 && col - 4 >= 0) {
            bool diagonalMatch2 = true;
            for (int i = 0; i < 5; i++) {
                if (Board[row + i, col - i] != Board[row, col] || Board[row, col] == "") {
                    diagonalMatch2 = false;
                    break;
                }
            }
            if (diagonalMatch2) return true;
        }

        return false;
    }
}