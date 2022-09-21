using System;
using Altzone.Scripts.Config;
using System.Collections;
using System.Collections.Generic;
using Battle.Scripts.Battle.Game;
using UnityEngine;
using UnityEngine.Assertions;

namespace Battle.Scripts.Battle
{
    public static class Extensions
    {
        // https://www.tutorialsteacher.com/csharp/csharp-extension-method
        public static int GetRow(this Tuple<int, int> gridPos)
        {
            if (gridPos == null)
            {
                return -1;
            }
            return gridPos.Item1;
        }

        public static int GetCol(this Tuple<int, int> gridPos)
        {
            if (gridPos == null)
            {
                return -1;
            }
            return gridPos.Item2;
        }

        public static void Demo()
        {
            var gripPos = new Tuple<int, int>(2, 4);
            var row = gripPos.GetRow();
            var col = gripPos.GetCol();
            Assert.AreEqual(2, row);
            Assert.AreEqual(4, col);
        }
    }
    
    public interface IGridManager
    {
        bool[,] _gridEmptySpaces { get; set; }

        Vector2 GridPositionToWorldPoint(int col, int row, bool isRotated);

        int[] CalcRowAndColumn(Vector2 targetPosition, bool isRotated);
    }

    public class GridPos : Tuple<int, int>
    {
        public int Row => Item1;
        public int Col => Item2;
        
        public GridPos(int row, int col) : base(row, col)
        {
        }
    }
    
    /// <summary>
    /// Example interface for <c>GridManager</c>.
    /// </summary>
    /// <remarks>
    /// The gird is zero based and origo is in bottom left corner.
    /// </remarks>
    internal interface IGridManagerProposal
    {
        /// <summary>
        /// Gets grid width aka row count.
        /// </summary>
        int RowCount { get; }

        /// <summary>
        /// Gets grid height aka column count.
        /// </summary>
        int ColCount { get; }
        
        /// <summary>
        /// The <c>GameCamera</c> that we use.
        /// </summary>
        GameCamera GameCamera { get; set; }
        
        /// <summary>
        /// Gets grid state for given row and column.
        /// </summary>
        bool GridState(int row, int col);
        bool GridState(GridPos gridPos);
        
        /// <summary>
        /// Tries to set grid state for given row and column.
        /// </summary>
        /// <returns>True if state was changed, false otherwise.</returns>
        bool TrySetGridState(int row, int col, bool state);
        bool TrySetGridState(GridPos gridPos, bool state);

        /// <summary>
        /// Converts grid row and column position to world x,y coordinates.
        /// </summary>
        Vector2 GridPositionToWorldPoint(int row, int col);
        Vector2 GridPositionToWorldPoint(GridPos gridPos);

        /// <summary>
        /// Converts world x,y coordinates to grid row and column position.
        /// </summary>
        /// <returns>Tuple where Item1 is row and Item2 is col.</returns>
        void WorldPointToGridPosition(Vector2 targetPosition, out int row, out int col);
        GridPos WorldPointToGridPosition(Vector2 targetPosition);
    }
    
    internal class GridManager : MonoBehaviour, IGridManager
    {
        private int _gridWidth;
        private int _gridHeight;

        public bool[,] _gridEmptySpaces { get; set; }

        private void Awake()
        {
            var runtimeGameConfig = RuntimeGameConfig.Get();
            var variables = runtimeGameConfig.Variables;
            var battleUi = runtimeGameConfig.BattleUi;

            _gridWidth = variables._battleUiGridWidth;
            _gridHeight = variables._battleUiGridHeight;

            _gridEmptySpaces = new bool[_gridWidth, _gridHeight];
            for (int i = 0; i < _gridWidth; i++)
            {
                for (int j = 0; j < _gridHeight; j++)
                {
                    _gridEmptySpaces[i, j] = true;
                }
            }
        }

        public Vector2 GridPositionToWorldPoint(int col, int row, bool isRotated)
        {
            var viewportPosition = new Vector2();
            viewportPosition.x = (float)col / _gridWidth + 0.5f / _gridWidth;
            viewportPosition.y = (float)row / _gridHeight + 0.5f / _gridHeight;
            Vector2 worldPosition = Camera.main.ViewportToWorldPoint(viewportPosition);
            if (isRotated)
            {
                worldPosition.x = -worldPosition.x;
                worldPosition.y = -worldPosition.y;
            }
            return worldPosition;
        }

        public int[] CalcRowAndColumn(Vector2 worldPosition, bool isRotated)
        {
            if (isRotated)
            {
                worldPosition.x = -worldPosition.x;
                worldPosition.y = -worldPosition.y;
            }
            var viewportPosition = Camera.main.WorldToViewportPoint(worldPosition);
            var col = (int)(viewportPosition.x * _gridWidth);
            var row = (int)(viewportPosition.y * _gridHeight);
            return new int[] { col, row };
        }
    }
}
