﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;


namespace Chess
{
    internal enum Rank // Rank enum for pieces
    {
        PAWN,
        ROOK,
        NIGH,
        BISH,
        QUEE,
        KING
    }
    internal enum Dir // Dir enum for moves
    {
        F,
        L,
        B,
        R
    }
    internal static class Calcs
    {
        //
        // Piece handling
        //
        static internal List<ChessPiece> pieces = new List<ChessPiece>(); // Global piece list [used by all]
        static internal List<ChessPiece> bP = new List<ChessPiece>(); // Black pieces
        static internal List<ChessPiece> wP = new List<ChessPiece>(); // White pieces
        static internal TableLayoutPanel board; // Board variable
        static internal ChessPiece WK; // White king
        static internal ChessPiece BK; // Black king

        static internal ChessPiece CheckPiece(PictureBox checkbox) // Get or create a piece from picturebox.
        { 
            if (checkbox == null || checkbox.BackColor == Color.DarkGray)
                return null;
            
            foreach (ChessPiece piece in pieces)
            { // Get box
                if (piece.box == checkbox)
                    return piece;
            }
            ChessPiece newpiece = new ChessPiece(checkbox, board); // Create box
            pieces.Add(newpiece);
            return newpiece;
        }
        //
        // Check check
        //
        static internal List<ChessPiece> CheckCheck(bool isWhite) // f team is in check
        {
            if (!board.Controls.Contains(isWhite ? WK.box : BK.box)) return new List<ChessPiece>() { isWhite ? BK : WK };
            List<ChessPiece> result = new List<ChessPiece>();
            foreach (ChessPiece pc in isWhite ? bP : wP)
            {
                if (CalcMovesBB(pc).Contains(isWhite ? WK.posPT : BK.posPT))
                    result.Add(pc);
            }
            return result;
        }
        //
        // NM Check (Can check for stalemate, is SM if the third output is "false"
        //
        static internal bool NMCheck(bool isWhite, List<ChessPiece> Checks) // Returns if no moves.
        {
            if (!board.Controls.Contains(isWhite ? WK.box : BK.box)) return false;
            foreach (ChessPiece pc in isWhite ? wP : bP)
            {
                if (CalcMovesG(pc, Checks).Count != 0)
                    return true;
            }
            return false;
        }


        //[[
        // Move calcs
        //]]

        //  These next few functions are general, and a loop for lines/diag
        // Function for the loop
        //
        static private int[] LoopFunc(int[] xy, List<Dir> direction)
        {
            int x = xy[0];
            int y = xy[1];

            foreach (Dir drct in direction)
            {
                switch (drct) // Direction handling
                {
                    case Dir.F:
                        y--;
                        break;
                    case Dir.R:
                        x++;
                        break;
                    case Dir.B:
                        y++;
                        break;
                    case Dir.L:
                        x--;
                        break;
                }
            }
            return new int[2] { x, y };
        }
        //
        // Check for the loop
        //
        static private bool CheckVals(params int[] vals)
        {
            foreach (int x in vals)
            {
                if (x < 0 || x > 7)
                    return false;
            }
            return true;
        }

        //
        // Loop to calculate Diag/Lines
        //
        static private List<Point> LoopCalc(ChessPiece piece, List<Dir> direction, int distance = 10)
        {
            List<Point> result = new List<Point>();
            TableLayoutPanelCellPosition pos = board.GetPositionFromControl(piece.box);
            int row = pos.Row;
            int col = pos.Column;
            int counter = 0;
            for (int[] xy = new int[2] { col, row }; CheckVals(xy); xy = LoopFunc(xy, direction))
            {
                if (distance == counter++) break; // Only do it as many time as specified
                Point pt = new Point(xy[0], xy[1]);
                PictureBox box = (PictureBox)board.GetControlFromPosition(xy[0], xy[1]);
                if (box == null)
                    result.Add(pt);
                else
                {
                    if (box == piece.box) continue;
                    if (box.BackColor == Color.DarkGray) break;
                    var pieceC = CheckPiece(box);
                    if (!pieceC.canCollide) // Make sure the box has collision 
                    {
                        result.Add(pt);
                        continue;
                    }
                    if (pieceC.isWhite != piece.isWhite) // Allow for capture, Disallow tempboxes
                        result.Add(pt);
                    break;
                }

            }
            return result;
        }


        //
        // Intermediate function: Calculates lines
        //
        static private List<Point> CalcLines(ChessPiece piece, int distance = 8)
        {
            List<Point> resF = LoopCalc(piece, new List<Dir>() { Dir.F }, distance);
            List<Point> resR = LoopCalc(piece, new List<Dir>() { Dir.R }, distance);
            List<Point> resB = LoopCalc(piece, new List<Dir>() { Dir.B }, distance);
            List<Point> resL = LoopCalc(piece, new List<Dir>() { Dir.L }, distance);
            return resF // Use loop to calculate lines each direction
                .Concat(resR)
                .Concat(resB)
                .Concat(resL)
                .ToList();
        }
        //
        // Intermediate function: Calculate diag
        //
        static private List<Point> CalcDiag(ChessPiece piece, int distance = 8)
        {
            List<Point> resFR = LoopCalc(piece, new List<Dir>() { Dir.F, Dir.R }, distance);
            List<Point> resBR = LoopCalc(piece, new List<Dir>() { Dir.B, Dir.R }, distance);
            List<Point> resBL = LoopCalc(piece, new List<Dir>() { Dir.B, Dir.L }, distance);
            List<Point> resFL = LoopCalc(piece, new List<Dir>() { Dir.F, Dir.L }, distance);
            return resFR // Use loop to calculate lines each diagonal
                .Concat(resBR)
                .Concat(resBL)
                .Concat(resFL)
                .ToList();
        }

        //
        // The next functions are rank-specific
        //

        //
        // Pawn
        //
        static private List<Point> CalcPawn(ChessPiece piece)
        {
            //
            // Vars
            //
            List<Point> result = new List<Point>();
            int offset = piece.isWhite ? -1 : 1;
            TableLayoutPanelCellPosition pos = board.GetPositionFromControl(piece.box);
            int row = pos.Row;
            int col = pos.Column;
            //
            // Straight move 
            //
            List<Point> fwd = LoopCalc(piece,
                piece.isWhite ? new List<Dir>() { Dir.F } : new List<Dir>() { Dir.B },
                piece.canDouble ? 3 : 2); // Calculate while allowing for double moves

            foreach (Point pt in fwd)
            {
                PictureBox box = (PictureBox)board.GetControlFromPosition(pt.X, pt.Y);
                if (box == null)
                    result.Add(pt);
            }

            // Loop for other attacks
            for (int x = 1; x >= -1; x -= 2)
            {
                if (!CheckVals(x + col) || !CheckVals(row + offset))
                    continue;

                // Diag attacks
                PictureBox pbox = (PictureBox)board.GetControlFromPosition(col + x, row + offset);
                if (pbox != null && pbox.BackColor != Color.DarkGray)
                {
                    ChessPiece ppiece = CheckPiece(pbox);
                    if (ppiece.isWhite != piece.isWhite)
                        result.Add(new Point(col + x, row + offset));
                }

                // En Passant
                pbox = (PictureBox)board.GetControlFromPosition(col + x, row);
                if (pbox != null && pbox.BackColor != Color.DarkGray)
                {
                    ChessPiece ppiece = CheckPiece(pbox);
                    if ((ppiece.isWhite != piece.isWhite) && ppiece.PassElig)
                        result.Add(new Point(col + x, row + offset));
                }
            }

            return result;
        }
        //
        // Rook
        //
        static private List<Point> CalcRook(ChessPiece piece)
        {
            return CalcLines(piece);
        }
        //
        // Knight
        //
        static private List<Point> CalcKnight(ChessPiece piece)
        {

            TableLayoutPanelCellPosition pos = board.GetPositionFromControl(piece.box);
            int y = pos.Row;
            int x = pos.Column;
            List<Point> tempmoves = new List<Point>()
                { // Calculate each point individually, TODO: optimise
                    new Point(x+1,y+2),
                    new Point(x-1,y+2),
                    new Point(x+1,y-2),
                    new Point(x-1,y-2),
                    new Point(x+2,y+1),
                    new Point(x-2,y+1),
                    new Point(x+2,y-1),
                    new Point(x-2,y-1)
                };
            List<Point> result = new List<Point>();
            foreach (Point pt in tempmoves)
            {
                if (CheckVals(pt.X, pt.Y))
                    result.Add(pt);
            }
            return result;
        }
        //
        // Bishop
        //
        static private List<Point> CalcBishop(ChessPiece piece)
        {
            return CalcDiag(piece); // Diagonal extending to end of board
        }
        //
        // Queen
        //
        static private List<Point> CalcQueen(ChessPiece piece)
        {
            List<Point> str = CalcLines(piece); // Line to end of board
            List<Point> diag = CalcDiag(piece); // Diag to end of board
            return str.Concat(diag)
                .ToList(); // Merge lists
        }
        //
        // King
        //
        static private List<Point> CalcKing(ChessPiece piece)
        {
            List<Point> str = CalcLines(piece, 2); // Line 1 long
            List<Point> diag = CalcDiag(piece, 2); // Diag 1 long
            return str.Concat(diag)
                .ToList(); // Merge lists
        }



        //
        // Calculate moves public class, for other files to use [basic]
        //
        static public List<Point> CalcMovesBB(ChessPiece piece)
        {
            List<Point> moves;
            List<Point> result = new List<Point>();
            List<Point> final = new List<Point>();
            switch (piece.pieceRank)
            { // Access rank-specific methods
                case Rank.PAWN:
                    moves = CalcPawn(piece);
                    break;
                case Rank.ROOK:
                    moves = CalcRook(piece);
                    break;
                case Rank.NIGH:
                    moves = CalcKnight(piece);
                    break;
                case Rank.BISH:
                    moves = CalcBishop(piece);
                    break;
                case Rank.QUEE:
                    moves = CalcQueen(piece);
                    break;
                case Rank.KING:
                    moves = CalcKing(piece);
                    break;
                default:
                    moves = new List<Point>();
                    break;
            }
            foreach (Point pt in moves)
            { //Remove invalid moves
                PictureBox box = (PictureBox)board.GetControlFromPosition(pt.X, pt.Y);
                if (box != null)
                {
                    if (box.BackColor == Color.DarkGray) continue;
                    ChessPiece pc = CheckPiece(box);
                    if (pc.isWhite != piece.isWhite)
                        result.Add(pt);
                }
                else result.Add(pt);
            }
            return result;
        }
        static public List<Point> CalcMovesG(ChessPiece piece)
        {
            List<Point> result = CalcMovesBB(piece);
            List<Point> final = new List<Point>();
            // Stop them willfully checking themselves.
            foreach (Point pt in result)
            {
                PictureBox box = piece.box;
                int scol = piece.pos.Column, srow = piece.pos.Row;
                piece.canCollide = false;
                PictureBox temp = Tbox(pt);
                if (CheckCheck(piece.isWhite).Count == 0)
                    final.Add(pt);
                piece.canCollide = true;
                board.Controls.Remove(temp);
            }
            return final;
        }
        //
        // Add temporary box for move calculation
        //
        static private PictureBox Tbox(Point pt, Color clr)
        {
            PictureBox box = new PictureBox();
            box.Name = string.Format("TEMP_{0}{1}", pt.X.ToString(), pt.Y.ToString());
            box.BackColor = clr;
            board.Controls.Add(box, pt.X, pt.Y);
            box.Dock = DockStyle.Fill;
            box.Margin = new Padding(1);
            return box;
        }
        static private PictureBox Tbox(Point pt) 
        {
            PictureBox box = new PictureBox();
            box.Name = string.Format("TEMP_{0}{1}", pt.X.ToString(), pt.Y.ToString());
            box.BackColor = Color.DarkGray;
            board.Controls.Add(box, pt.X, pt.Y);
            box.Dock = DockStyle.Fill;
            box.Margin = new Padding(1);
            return box;
        }
        //
        // Overload for move calculation, for moving in check
        //
        static public List<Point> CalcMovesG(ChessPiece piece, List<ChessPiece> checkingPieces)
        {
            List<Point> tempMoves = CalcMovesG(piece);
            if (checkingPieces.Count == 0) return tempMoves; // Make sure they're actually in check
            List<Point> result = new List<Point>();
            List<Point> otherMoves = new List<Point>();
            foreach(ChessPiece pc in piece.isWhite ? bP : wP)
            {
                otherMoves = otherMoves.Union(CalcMovesBB(pc)).ToList();
            } // Get all possible moves for other team

            foreach (Point pt in tempMoves)
            {
                if (piece.pieceRank == Rank.KING) // Check if king can dodge
                    if (!otherMoves.Contains(pt)){
                        //Tbox(pt, Color.Green);
                        result.Add(pt);}
                
                if (checkingPieces.Count == 1)
                {
                    if (checkingPieces[0].posPT == pt){ // Check if a piece can take
                        result.Add(pt);
                    //Tbox(pt,Color.Blue);
                }
                    else
                    {
                        if (otherMoves.Contains(pt) && piece.pieceRank != Rank.KING )
                        {
                            PictureBox box = Tbox(pt);
                            if (!CalcMovesBB(checkingPieces[0]) // Check if a piece can block
                                .Contains(piece.isWhite ? WK.posPT : BK.posPT)){
                                result.Add(pt);
                                Tbox(pt,Color.Yellow);
                            }
                            board.Controls.Remove(box);
                        }
                    }
                }
            }

            return result;
        }

    }
}
    //
    // Piece class
    //


            /*List<ChessPiece> WC = new List<ChessPiece>();
            List<ChessPiece> BC = new List<ChessPiece>();
            if (WK == null || BK == null)
                throw new Exception("One or more kings are missing");
            Point WP = new Point(WK.pos.Column, WK.pos.Row);
            Point BP = new Point(BK.pos.Column, BK.pos.Row);
            for (int i = 0; i < pieces.Count; i++)
           // foreach (ChessPiece pc in pieces.Where(i => i.box.BackColor != Color.DarkGray))
            {
                ChessPiece pc = pieces[i];
                List<Point> moves = CalcMovesBB(pc);
                if (moves.Contains(WP) && !pc.isWhite)
                    WC.Add(pc);
                if (moves.Contains(BP) && pc.isWhite)
                    BC.Add(pc);
            }
            return new List<ChessPiece> { WC, BC };*/



/*List<Point> movesTemp = CalcMovesG(piece);
if (checkingPieces.Count == 0) return movesTemp;
List<Point> moves2 = new List<Point>();
bool iW = piece.isWhite;
List<Point> otherMoves = new List<Point>();
foreach (ChessPiece pc in pieces)
{
    if (pc.isWhite != iW)
        otherMoves = otherMoves.Union(CalcMovesG(piece)).ToList();
}
Console.WriteLine(piece.pieceRank.ToString());
foreach (Point pt in movesTemp)
{
                
    if (piece.pieceRank == Rank.KING
        || !otherMoves.Contains(pt))
        moves2.Add(pt);
}
foreach (Control ct in board.Controls)
{
    if (ct.BackColor == Color.DarkGray)
        board.Controls.Remove(ct);
}
foreach (Point pt in moves2)
    Console.WriteLine("{0}, {1}", pt.X, pt.Y);
return moves2;*/


/*
 * 
 * Old code:
 
    internal class Pawn : ChessPiece
    {
        internal Pawn(PictureBox Box) : base(Box) { }
   
        internal bool canDouble;
        internal bool canPassLeft;
        internal bool canPassRight;
        internal List<TableLayoutPanelCellPosition> CalcMoves(PictureBox pbox, TableLayoutPanel board)
        {
            List<TableLayoutPanelCellPosition> result = new List<TableLayoutPanelCellPosition>();
            TableLayoutPanelCellPosition pos = board.GetPositionFromControl(pbox);
            int row = pos.Row;
            int col = pos.Column;


            throw new NotImplementedException();
        }
    }
    internal class Rook : ChessPiece
    {
        internal Rook(PictureBox Box) : base(Box) { }

    }
    internal class Knight : ChessPiece
    {
        internal Knight(PictureBox Box) : base(Box) { }

    }
    internal class Bishop : ChessPiece
    {
        internal Bishop(PictureBox Box) : base(Box) { }

    }
    internal class Queen : ChessPiece
    {
        internal Queen(PictureBox Box) : base(Box) { }

    }
        internal class King : ChessPiece
    {
        internal King(PictureBox Box) : base(Box) { }

    }
 
 * 
 *             List<Point> WM = pieces.Where(p => p.isWhite)
                .Select(piece => CalcMovesG(piece, Checks))
                .SelectMany(x => x)
                .ToList();
            List<Point> BM = pieces.Where(p => !p.isWhite)
                .Select(piece => CalcMovesG(piece, Checks))
                .SelectMany(x => x)
                .ToList();
 */
