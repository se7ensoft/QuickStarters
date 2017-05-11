﻿using MultiplayerTopDownTank.Entities;
using System.Runtime.Serialization;
using WaveEngine.Common.Math;
using WaveEngine.Framework;

namespace MultiplayerTopDownTank.Components
{
    [DataContract(Namespace = "MultiplayerTopDownTank.Components")]
    public class BulletEmitter : Component
    {
        private int bulletMax;
        private Bullet[] bullets;
        private int bulletIndex;

        protected override void DefaultValues()
        {
            base.DefaultValues();
            this.bulletMax = 5;
        }

        private void InitBulletPool()
        {
            this.bullets = new Bullet[this.bulletMax];

            for (int i = 0; i < this.bulletMax; i++)
            {
                Bullet bullet = new Bullet();
                this.bullets[i] = bullet;
                this.EntityManager.Add(bullet.Entity);
            }
        }

        /// <summary>
        /// Shoots the specified position.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="direction">The direction.</param>
        public void Shoot(Vector2 position, Vector2 direction)
        {
            if (this.bullets == null)
            {
                this.InitBulletPool();
            }

            Bullet bullet = this.bullets[this.bulletIndex];
            bullet.Position = position;
            bullet.Direction = direction;

            this.bulletIndex = (this.bulletIndex + 1) % this.bulletMax;
        }

        public void DestrotBullet(Bullet bullet)
        {
            bullet.IsVisible = false;
        }
    }
}
