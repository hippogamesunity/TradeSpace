﻿using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Behaviour;
using Assets.Scripts.Environment;

namespace Assets.Scripts.Views
{
    public class Galaxy : UIScreen
    {
        public UISprite Background;

        protected override void Initialize()
        {
            var pathLines = new List<PathLine>();

            foreach (var location in Env.Galaxy.Values)
            {
                var instance = PrefabsHelper.InstantiateSystem(Panel);

                instance.name = location.Name;
                instance.GetComponent<SystemButton>().Initialize(location);

                foreach (var system in Env.Routes[location.System].Keys)
                {
                    if (pathLines.Any(i => i.Source == system && i.Destination == location.System))
                    {
                        continue;
                    }

                    GetComponent<NativeRenderer>().DrawHyperLine(Panel, location.Position, Env.Galaxy[system].Position, location.Color.SetAlpha(0.1f), Env.Galaxy[system].Color.SetAlpha(0.1f));
                }
            }

            Background.enabled = true;
            Open<Ships>();
            Open<Route>();
            Open<ShipSelect>();
        }

        protected override void Cleanup()
        {
            Panel.Clear();
            Background.enabled = false;
            Close<Ships>();
            Close<Route>();
        }
    }
}