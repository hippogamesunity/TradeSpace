﻿using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Common;
using Assets.Scripts.Data;
using Assets.Scripts.Environment;
using UnityEngine;

namespace Assets.Scripts
{
    public static class RouteEngine
    {
        private class Node
        {
            public string System;
            public Node Previous;
            public long Distance;
        }

        public static List<RouteNode> FindRoute(RouteNode departure, RouteNode arrival, float speed)
        {
            departure.Time = DateTime.UtcNow;

            if (departure.System == arrival.System)
            {
                return FindSystemRoute(departure, arrival, speed);
            }

            var route = new List<RouteNode>();
            var galaxyRoute = FindRoute(departure.System, arrival.System, 99999);

            for (var j = 0; j < galaxyRoute.Count; j++)
            {
                RouteNode dpt, arv;

                if (j == 0)
                {
                    arv = Env.Systems[departure.System].Select(i => i.Value).Where(i => i is Gates).Single(i => i.ToGates().ConnectedSystem == galaxyRoute[1]).ToRouteNode();
                    arv.Time = departure.Time.AddSeconds((departure.Position - arv.Position).magnitude / speed);
                    route.Add(departure);
                    route.Add(arv);

                    continue;
                }

                if (j < galaxyRoute.Count - 1)
                {
                    dpt = Env.Systems[galaxyRoute[j]].Select(i => i.Value).Where(i => i is Gates).Single(i => i.ToGates().ConnectedSystem == galaxyRoute[j - 1]).ToRouteNode();
                    arv = Env.Systems[galaxyRoute[j]].Select(i => i.Value).Where(i => i is Gates).Single(i => i.ToGates().ConnectedSystem == galaxyRoute[j + 1]).ToRouteNode();
                }
                else
                {
                    dpt = Env.Systems[galaxyRoute[j]].Select(i => i.Value).Where(i => i is Gates).Single(i => i.ToGates().ConnectedSystem == galaxyRoute[j - 1]).ToRouteNode();
                    arv = arrival;
                }

                dpt.Time = route.Last().Time.AddSeconds((Env.Galaxy[galaxyRoute[j - 1]].Position - Env.Galaxy[galaxyRoute[j]].Position).magnitude / speed);
                arv.Time = dpt.Time.AddSeconds((dpt.Position - arv.Position).magnitude / speed);

                route.AddRange(FindSystemRoute(dpt, arv, speed));
            }

            return route;
        }

        private static List<RouteNode> FindSystemRoute(RouteNode departure, RouteNode arrival, float speed)
        {
            const float radius = 100;
            var route = new List<RouteNode>();

            if (Geometry.IntersectionLineCircle(Vector2.zero, radius, departure.Position, arrival.Position))
            {
                var tangentA = FindTangent(radius, departure.Position, arrival.Position);
                var tangentB = FindTangent(radius, arrival.Position, tangentA);
                var polyline = new List<Vector2> { departure.Position, tangentA };

                for (var i = 1; i < Math.Abs(Vector2.Angle(tangentA, tangentB)); i += 10)
                {
                    var sign = Vector3.Cross(tangentA, tangentB).z < 0 ? -1 : 1;

                    polyline.Add(Quaternion.Euler(0, 0, sign * i) * tangentA);
                }

                polyline.Add(tangentB);
                polyline.Add(arrival.Position);

                for (var i = 0; i < polyline.Count; i++)
                {
                    var locationName = i == 0 ? departure.LocationName : i == polyline.Count - 1 ? arrival.LocationName : null;
                    var time = i == 0 ? departure.Time : route[i - 1].Time.AddSeconds((polyline[i] - polyline[i - 1]).magnitude / speed);

                    route.Add(new RouteNode
                    {
                        System = departure.System,
                        LocationName = locationName,
                        Position = polyline[i],
                        Time = time
                    });
                }
            }
            else
            {
                arrival.Time = departure.Time.AddSeconds((arrival.Position - departure.Position).magnitude / speed);

                route.Add(departure);
                route.Add(arrival);
            }

            return route;
        }

        private static Vector2 FindTangent(float radius, Vector2 vector, Vector2 direction)
        {
            var magnitude = Mathf.Sqrt(Mathf.Pow(vector.magnitude, 2) - Mathf.Pow(radius, 2));
            var angle = Mathf.Rad2Deg * Mathf.Acos(magnitude / vector.magnitude);
            var tangentA = -(Vector2) (Quaternion.Euler(0, 0, angle) * vector).normalized * magnitude + vector;
            var tangentB = -(Vector2) (Quaternion.Euler(0, 0, -angle) * vector).normalized * magnitude + vector;

            return Mathf.Abs(Vector2.Angle(direction - vector, tangentA)) < Mathf.Abs(Vector2.Angle(direction - vector, tangentB))
                ? tangentA
                : tangentB;
        }

        private static List<Node> _nodes;
        private static long _maxDistance;

        private static List<string> FindRoute(string departure, string arrival, long maxDistance)
        {
            var node = new Node { System = departure };

            _nodes = new List<Node> { node };
            _maxDistance = maxDistance;

            FindDijkstraVertices(node);

            node = _nodes.SingleOrDefault(i => i.System == arrival);

            if (node == null)
            {
                return null;
            }

            var route = new List<Node> { node };

            while (true)
            {
                var prev = route.Last().Previous;

                if (prev == null)
                {
                    route.Reverse();

                    return route.Select(i => i.System).ToList();
                }

                route.Add(prev);
            }
        }

        private static void FindDijkstraVertices(Node current)
        {
            foreach (var nextSystem in Env.Routes[current.System].Keys)
            {
                var distance = current.Distance + Env.Routes[current.System][nextSystem];

                if (distance > _maxDistance) continue;

                var visitedNode = _nodes.SingleOrDefault(i => i.System == nextSystem);

                if (visitedNode != null)
                {
                    if (visitedNode.Distance > distance)
                    {
                        visitedNode.Distance = distance;
                        visitedNode.Previous = current;

                        FindDijkstraVertices(visitedNode);
                    }
                }
                else
                {
                    var node = new Node
                    {
                        System = nextSystem,
                        Distance = distance,
                        Previous = current
                    };

                    _nodes.Add(node);

                    FindDijkstraVertices(node);
                }
            }
        }
    }
}