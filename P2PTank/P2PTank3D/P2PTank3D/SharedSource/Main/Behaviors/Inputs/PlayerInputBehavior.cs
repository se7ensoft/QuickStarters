﻿using P2PTank.Components;
using P2PTank.Entities.P2PMessages;
using P2PTank.Managers;
using P2PTank.Scenes;
using WaveEngine.Common.Input;
using WaveEngine.Common.Math;
using WaveEngine.Components.Graphics2D;
using WaveEngine.Framework;
using WaveEngine.Framework.Diagnostic;
using WaveEngine.Framework.Graphics;
using WaveEngine.Framework.Physics2D;
using WaveEngine.Framework.Services;
using P2PTank.Services;
using P2PTank.Entities;
using P2PTank.Behaviors.Inputs;

namespace P2PTank.Behaviors
{
    public class PlayerInputBehavior : Behavior
    {
        public string PlayerID { get; set; }

        [RequiredComponent]
        private TankComponent tankComponent = null;

        [RequiredComponent]
        private Transform2D transform = null;

        [RequiredComponent]
        private RigidBody2D rigidBody = null;

        private GamePlayManager gamePlayManager;

        private Transform2D barrelTransform = null;

        private Sprite barrel;

        private P2PManager peerManager;

        private float shootTimer;

        private Joystick joystick;

        private FireButton fireButton;

        public PlayerInputBehavior(P2PManager peerManager, string playerID)
        {
            this.PlayerID = playerID;
            this.peerManager = peerManager;
        }

        protected override void ResolveDependencies()
        {
            base.ResolveDependencies();

            var virtualJoystickScene = WaveServices.ScreenContextManager.CurrentContext.FindScene<VirtualJoystickScene>();

            if (virtualJoystickScene != null)
            {
                this.joystick = virtualJoystickScene.EntityManager.Find<Joystick>("joystick");
                this.fireButton = virtualJoystickScene.EntityManager.Find<FireButton>("fireButton");
            }

            var barrelEntity = this.Owner.FindChild(GameConstants.EntitynameTankBarrel);
            this.barrel = barrelEntity.FindComponent<Sprite>();
            this.barrelTransform = barrelEntity.FindComponent<Transform2D>();

            this.gamePlayManager = this.Owner.Scene.EntityManager.FindComponentFromEntityPath<GamePlayManager>(GameConstants.ManagerEntityPath);
        }

        protected override void Update(System.TimeSpan gameTime)
        {
            Input input = WaveServices.Input;

            float elapsedTime = (float)gameTime.TotalSeconds;

            var playerCommand = new PlayerCommand();

            this.HandleKeyboard(input, ref playerCommand);
            this.HandlePad(input, ref playerCommand);
            this.HandleVirtualJoystick(ref playerCommand);
            this.RunInputCommands(playerCommand, elapsedTime);

            this.HandleNetworkMessages();
        }

        private void HandleVirtualJoystick(ref PlayerCommand playerCommand)
        {
            if (this.joystick == null || this.fireButton == null)
            {
                return;
            }

            Vector2 leftThumb = this.joystick.Direction;
            bool isShooting = this.fireButton.IsShooting;

            if (leftThumb != Vector2.Zero)
            {
                playerCommand.SetMove(leftThumb.Y);
                playerCommand.SetRotate(leftThumb.X);
            }

            if (isShooting)
            {
                playerCommand.SetShoot();
            }
        }

        private async void HandleNetworkMessages()
        {
            if (this.peerManager != null)
            {
                if (this.rigidBody.Awake)
                {
                    var moveMessage = new MoveMessage()
                    {
                        PlayerId = this.PlayerID,
                        X = this.transform.LocalPosition.X,
                        Y = this.transform.LocalPosition.Y,
                    };
                    await this.peerManager.SendBroadcastAsync(this.peerManager.CreateMessage(P2PMessageType.Move, moveMessage));

                    var rotateMessage = new RotateMessage()
                    {
                        PlayerId = this.PlayerID,
                        Rotation = this.transform.Rotation,
                    };
                    await this.peerManager.SendBroadcastAsync(this.peerManager.CreateMessage(P2PMessageType.Rotate, rotateMessage));
                }
            }

            if (this.lastBarrelRotation != this.barrelTransform.Rotation)
            {
                var barrelRotateMessage = new BarrelRotate()
                {
                    PlayerId = this.PlayerID,
                    Rotation = this.barrelTransform.Rotation,
                };

                this.lastBarrelRotation = this.barrelTransform.Rotation;
                await this.peerManager.SendBroadcastAsync(this.peerManager.CreateMessage(P2PMessageType.BarrelRotate, barrelRotateMessage));
            }
        }

        private float lastBarrelRotation;

        private void RunInputCommands(PlayerCommand playerCommand, float elapsedTime)
        {
            if (this.joystick != null && this.fireButton != null)
            {
                this.Move(playerCommand.AbsoluteMove, elapsedTime);
                this.Rotate(this.joystick.Direction, elapsedTime);
            }
            else
            {
                this.Move(playerCommand.Move, elapsedTime);
                this.Rotate(playerCommand.Rotate, elapsedTime);
                this.RotateBarrel(playerCommand.RotateBarrel, elapsedTime);
            }

            this.Shoot(playerCommand.Shoot, elapsedTime);
        }

        private void HandlePad(Input input, ref PlayerCommand playerCommand)
        {
            GamePadState gamepadState = input.GamePadState;

            if (gamepadState.IsConnected)
            {
                Vector2 leftThumb = this.ApplyDeadZone(gamepadState.ThumbSticks.Left);
                Vector2 rightthumb = this.ApplyDeadZone(gamepadState.ThumbSticks.Right);

                if (leftThumb != Vector2.Zero)
                {
                    playerCommand.SetMove(-leftThumb.Y);
                    playerCommand.SetRotate(leftThumb.X);
                }

                if (rightthumb != Vector2.Zero)
                {
                    playerCommand.SetRotateBarrel(rightthumb.X);
                }

                if (gamepadState.Buttons.RightShoulder == ButtonState.Pressed
                    || gamepadState.Triggers.Right > 0.5f)
                {
                    playerCommand.SetShoot();
                }
            }
        }

        private Vector2 ApplyDeadZone(Vector2 vector)
        {
            var output = vector;

            var deadZone = GameSettings.GamePadDeadZone;

            // Scaled radial DeadZone, the Right way to do a DeadZone:
            // https://web.archive.org/web/20130418234531/http://www.gamasutra.com/blogs/JoshSutphin/20130416/190541/Doing_Thumbstick_Dead_Zones_Right.php
            var magnitude = output.Length();
            if (magnitude < deadZone)
            {
                output = Vector2.Zero;
            }
            else
            {
                output.Normalize();
                output *= ((magnitude - deadZone) / (1 - deadZone));
            }

            return output;
        }

        private void HandleKeyboard(Input input, ref PlayerCommand playerCommand)
        {
            if (input.KeyboardState.Up == ButtonState.Pressed)
            {
                playerCommand.SetMove(-1.0f);
            }
            else if (input.KeyboardState.Down == ButtonState.Pressed)
            {
                playerCommand.SetMove(1.0f);
            }

            if (input.KeyboardState.Left == ButtonState.Pressed)
            {
                playerCommand.SetRotate(-1.0f);
            }
            else if (input.KeyboardState.Right == ButtonState.Pressed)
            {
                playerCommand.SetRotate(1.0f);
            }

            if (input.KeyboardState.A == ButtonState.Pressed)
            {
                playerCommand.SetRotateBarrel(-1.0f);
            }
            else if (input.KeyboardState.D == ButtonState.Pressed)
            {
                playerCommand.SetRotateBarrel(1.0f);
            }

            if (input.KeyboardState.Space == ButtonState.Pressed)
            {
                playerCommand.SetShoot();
            }
        }

        private void Move(float forward, float elapsedTime)
        {
            if (forward == 0)
            {
                return;
            }

            var orientation = this.transform.Orientation;

            float fixedStep = (1f / 60f);
            this.rigidBody.LinearVelocity = forward * (orientation * Vector3.UnitY * fixedStep * this.tankComponent.CurrentSpeed).ToVector2();
        }

        private void Move(Vector2 forward, float elapsedTime)
        {
            float fixedStep = (1f / 60f);
            this.rigidBody.LinearVelocity = forward * (fixedStep * this.tankComponent.CurrentSpeed);
        }

        private void Rotate(float left, float elapsedTime)
        {
            if (left == 0)
            {
                return;
            }

            var roll = left * this.tankComponent.CurrentRotationSpeed * elapsedTime;
            this.rigidBody.AngularVelocity = roll;
        }

        private void Rotate(Vector2 orientation, float elapsedTime)
        {
            var angle = Vector2.Angle(Vector2.UnitY, orientation * new Vector2(1, -1));

            if (double.IsInfinity(angle) || double.IsNaN(angle))
                return;

            this.rigidBody.SetTransform(this.rigidBody.Transform2D.Position, angle);
        }

        private void RotateBarrel(float left, float elapsedTime)
        {
            if (left == 0)
            {
                return;
            }

            var roll = 
                left * this.tankComponent.CurrentRotationBarrelSpeed * elapsedTime;

            this.barrelTransform.Orientation = 
                this.barrelTransform.Orientation * Quaternion.CreateFromYawPitchRoll(0.0f, 0.0f, roll);
        }

        private void Shoot(bool shoot, float elapsedTime)
        {
            if (shootTimer > 0)
            {
                Labels.Add("Canshoot", false);

                this.shootTimer -= elapsedTime;
            }
            else
            {
                Labels.Add("Canshoot", true);

                if (shoot)
                {
                    var position = this.transform.LocalPosition;

                    // Rotation angle must be absolute
                    var angle = this.barrelTransform.Rotation;

                    var direction = new Vector2((float)System.Math.Sin(angle), -(float)System.Math.Cos(angle));

                    this.gamePlayManager.ShootPlayerBullet(position, direction, this.tankComponent.Color, peerManager);

                    this.shootTimer = this.tankComponent.CurrentShootInterval;
                }
            }
        }

        public void Hit(float damage)
        {
            this.tankComponent.CurrentLive -= damage;

            if (this.tankComponent.CurrentLive <= 50)
            {
                this.HitTank(this.tankComponent.CurrentLive);
            }

            if (this.tankComponent.CurrentLive <= 0)
            {
                this.DestroyTank();
            }
        }

        private async void HitTank(double life)
        {
            var hitMessage = new HitPlayerMessage() { PlayerId = this.PlayerID, PlayerLife = life };
            await peerManager.SendBroadcastAsync(peerManager.CreateMessage(P2PMessageType.HitPlayer, hitMessage));
        }

        private async void DestroyTank()
        {
            this.gamePlayManager.DestroyTank(this.Owner);
            var audioService = WaveServices.GetService<AudioService>();
            audioService.Play(Audio.Sfx.Explosion_wav);

            var destroyMessage = new DestroyPlayerMessage() { PlayerId = this.PlayerID };
            await peerManager.SendBroadcastAsync(peerManager.CreateMessage(P2PMessageType.DestroyPlayer, destroyMessage));

            ((GamePlayScene)this.Owner.Scene).CreateCountDown();
        }
    }
}