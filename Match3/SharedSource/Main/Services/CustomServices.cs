﻿using Match3.Services.Navigation;
using Match3.Services.Audio;
using WaveEngine.Framework.Services;

namespace Match3.Services
{
    public class CustomServices
    {
        static CustomServices()
        {
            WaveServices.RegisterService(new AudioPlayer());
            WaveServices.RegisterService(new NavigationService());
            WaveServices.RegisterService(new GameLogic());
        }

        public static AudioPlayer AudioPlayer
        {
            get { return WaveServices.GetService<AudioPlayer>(); }
        }

        public static NavigationService NavigationService
        {
            get { return WaveServices.GetService<NavigationService>(); }
        }

        public static GameLogic GameLogic
        {
            get { return WaveServices.GetService<GameLogic>(); }
        }
    }
}
