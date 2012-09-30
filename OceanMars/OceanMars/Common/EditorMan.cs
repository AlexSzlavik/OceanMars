using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using OceanMars.Common;
using OceanMars.Client;
using System.Xml.Serialization;
using System.IO;

namespace OceanMars.Common
{
    class EditorMan : FreeEntity
    {
        //TODO: Move all this ellipse crap to a high level entity, inherit from that
        private const float FUZZY_EPSILON = 0.01f;
        public Vector2 collisionEllipse;

        private Vector2? point1 = null;
        private Entity parent = null;
        private List<Vector2[]> walls = null;
        private State state = null;
        private List<Vector2> spawns = null;

        private float intersect(Vector2 planeOrigin, Vector2 planeNormal, Vector2 rayOrigin, Vector2 rayVector)
        {
            //Assuming normal and vector are normalized
            float d = -Vector2.Dot(planeNormal, planeOrigin);
            float numer = Vector2.Dot(planeNormal, rayOrigin) + d;
            float denom = Vector2.Dot(planeNormal, rayVector);
            return -(numer / denom);
        }

        float intersectEllipsoid(Vector3 center, Vector3 ellipsoid_radius, Vector3 ray_origin, Vector3 ray)
        {
            // Center around the ellipsoid
            ray_origin.X = ray_origin.X - center.X;
            ray_origin.Y = ray_origin.Y - center.Y;
            ray_origin.Z = ray_origin.Z - center.Z;
            Vector3 ray_normal = ray;
            ray_normal.Normalize();

            // Scale the ellipsoid and apply the quadratic equation
            float a = ((ray_normal.X * ray_normal.X) / (ellipsoid_radius.X * ellipsoid_radius.X))
                    + ((ray_normal.Y * ray_normal.Y) / (ellipsoid_radius.Y * ellipsoid_radius.Y))
                    + ((ray_normal.Z * ray_normal.Z) / (ellipsoid_radius.Z * ellipsoid_radius.Z));
            float b = ((2 * ray_origin.X * ray_normal.X) / (ellipsoid_radius.X * ellipsoid_radius.X))
                    + ((2 * ray_origin.Y * ray_normal.Y) / (ellipsoid_radius.Y * ellipsoid_radius.Y))
                    + ((2 * ray_origin.Z * ray_normal.Z) / (ellipsoid_radius.Z * ellipsoid_radius.Z));
            float c = ((ray_origin.X * ray_origin.X) / (ellipsoid_radius.X * ellipsoid_radius.X))
                    + ((ray_origin.Y * ray_origin.Y) / (ellipsoid_radius.Y * ellipsoid_radius.Y))
                    + ((ray_origin.Z * ray_origin.Z) / (ellipsoid_radius.Z * ellipsoid_radius.Z))
                    - 1;

            float d = ((b * b) - (4 * a * c));

            // Check for actual intersection (if b^2 - 4ac < 0)
            if (d < 0) { return -1; }
            else { d = (float)Math.Sqrt(d); }
            float hit = (-b + d) / (2 * a);
            float hitsecond = (-b - d) / (2 * a);

            if (hit < hitsecond) { return hit; }
            else { return hitsecond; }
        }

        public EditorMan(Entity parent, State state) : base(new Vector2(21, 21), parent) {
            this.parent = parent;
            this.state = state;
            collisionEllipse = new Vector2(21, 21) / 2.0f;
            walls = new List<Vector2[]>();
            spawns = new List<Vector2>();
        }

        // The user has choosen to create a point on the map
        // This point is either the beginning or the ending of a line
        public Entity pointSet()
        {
            
            Entity result = null;

            // If the first point is not set, set it now
            if (point1 == null)
            {
                point1 = new Vector2(worldTransform.Translation.X, inverseWorldTransform.Translation.Y);
            }
            // Otherwise, create the new wall entity
            else
            {
                Vector2 point2 = new Vector2(worldTransform.Translation.X, inverseWorldTransform.Translation.Y);
                TestWall w = new TestWall(this, (Vector2)point1, point2);
                parent.addChild(w);
                result = w;
                // Add the walls to the list of vector2 arrays for saving later
                Vector2[] worldWallEndpoints = new Vector2[] {
                        Vector2.Transform(w.endPoints[0], w.worldTransform),
                        Vector2.Transform(w.endPoints[1], w.worldTransform)
                                                };
                walls.Add(worldWallEndpoints);

                point1 = null;
            }

            return result;
        }

        // Write the contents of the wall list to a file
        public void saveWalls()
        {
            string filePath = @"../../../../OceanMarsContent/Levels/custom.lvl";
            XmlSerializer serializer = new XmlSerializer(typeof(List<Vector2[]>));

            TextWriter textWriter = new StreamWriter(filePath);

            // Create a list containing all of the spawn and wall details
            List<Vector2[]> outList = new List<Vector2[]>();
            Vector2[] spawnVectors = new Vector2[spawns.Count];
            for (int i = 0; i < spawns.Count; i++)
            {
                spawnVectors[i] = spawns[i];
            }
            outList.Add(spawnVectors);
            foreach (Vector2[] v in walls)
            {
                outList.Add(v);
            }

            // Write out the list and close the file
            serializer.Serialize(textWriter, outList);
            textWriter.Close();

        }

        public void loadLevel(string filePath, Entity root)
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(List<Vector2[]>));

            TextReader textReader = new StreamReader(filePath);
            List<Vector2[]> level = (List<Vector2[]>)deserializer.Deserialize(textReader);
            textReader.Close();

            TestWall w = null;
            SpawnPointEntity s = null;
            Vector2[] v;

            // First, create the set of spawn points
            v = level[0];
            for (int i = 0; i < v.Length; i++)
            {
                spawns.Add(v[i]);
                s = new SpawnPointEntity(root, v[i]);
                root.addChild(s);
            }

            // Next, create the set of walls
            for (int i = 1; i < level.Count; i++)
            {
                v = level[i];
                walls.Add(v);
                w = new TestWall(root, v[0], v[1]);
                root.addChild(w);
            }
        }

        private SliderEntity getCollidedWall()
        {
            SliderEntity collisionEntity = null;
            foreach (Entity entity in state.entities.Values.ToList())
            {
                if (entity is SliderEntity)
                {
                    SliderEntity slider = (SliderEntity)entity;
                    Vector2 lineIntersectionPoint = new Vector2(0, 0);
                    Matrix transformSliderToLocal = slider.worldTransform * inverseWorldTransform;

                    Vector2[] worldSliderEndpoints = {
                        Vector2.Transform(slider.endPoints[0], slider.worldTransform),
                        Vector2.Transform(slider.endPoints[1], slider.worldTransform)
                                                };

                    //find the SliderEntity's end points and normal
                    Vector2[] sliderEndPoints = {
                        Vector2.Transform(slider.endPoints[0], transformSliderToLocal),
                        Vector2.Transform(slider.endPoints[1], transformSliderToLocal)
                                                };

                    //System.Diagnostics.Debug.WriteLine(sliderEndPoints[0].X + "," + sliderEndPoints[0].Y + "\n" + sliderEndPoints[1].X + "," + sliderEndPoints[1].Y + "\n");

                    //TODO: Unit normal
                    Vector2 sliderNormal = Vector2.Transform((sliderEndPoints[1] - sliderEndPoints[0]),
                        Matrix.CreateRotationZ((float)(-Math.PI / 2.0f)));
                    sliderNormal.Normalize();

                    //Calculate the ellipse intersection point
                    Vector2 ellipseRadiusVector = new Vector2(-sliderNormal.X * collisionEllipse.X,
                                                             -sliderNormal.Y * collisionEllipse.Y);

                    //is the plane embedded in ellipse?
                    float distance = intersect(sliderEndPoints[0], sliderNormal, new Vector2(0, 0), -sliderNormal);
                    
                    // Check if we are even within range of hitting a damn thing
                    if (distance < 0) continue;

                    //return the first entity we hit
                    if (Math.Abs(distance) <= ellipseRadiusVector.Length())
                    {
                        collisionEntity = slider;
                        break;
                    }
                }
            }
            return collisionEntity;
        }

        public void deleteOverlappingWall()
        {
            SliderEntity wall = getCollidedWall();
            if (wall != null)
            {
                System.Diagnostics.Debug.WriteLine(wall.id);
            }
        }
    }
}
