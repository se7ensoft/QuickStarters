﻿using System;

namespace Match3.Game
{
    public class BoardGenerator
    {
        private CandyColors[] candyColors;

        private Random random;

        public BoardGenerator()
        {
            this.candyColors = (CandyColors[])Enum.GetValues(typeof(CandyColors));
            this.random = new Random((int)DateTime.Now.Ticks);
        }

        public Candy[][] Generate(int sizeN, int sizeM)
        {
            var boardStatus = new Candy[sizeN][];
            for (int i = 0; i < boardStatus.Length; i++)
            {
                boardStatus[i] = new Candy[sizeM];
                for (int j = 0; j < boardStatus[i].Length; j++)
                {
                    boardStatus[i][j] = new Candy
                    {
                        Color = this.GetRandomCandyColor(),
                        Type = CandyTypes.Regular
                    };
                }
            }

            return boardStatus;
        }

        public CandyColors GetRandomCandyColor()
        {
            var randomColorIndex = this.random.Next(this.candyColors.Length);
            return this.candyColors[randomColorIndex];
        }
    }
}
