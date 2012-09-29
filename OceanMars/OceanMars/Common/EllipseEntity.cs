using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    class EllipseEntity : Entity
    {
        public Vector2 collisionEllipse;

        public EllipseEntity(Vector2 size) : base(size)
        {
            collisionEllipse = size;
        }

        public float intersect(Vector2 planeOrigin, Vector2 planeNormal, Vector2 rayOrigin, Vector2 rayVector)
        {
            float d = -Vector2.Dot(planeNormal, planeOrigin);
            float numer = Vector2.Dot(planeNormal, planeOrigin) + d;
            float denom = Vector2.Dot(planeNormal, rayVector);
            return -(numer / denom);
        }

        public float intersectEllipse(Vector2 sO, float sR, Vector2 rO, Vector2 rV)
        {
            /*
            Vector2 Q = sO - rO;
            double c = length of Q;
            double v = Q * rV;
            double d = sR*sR - (c*c – v*v);

            // If there was no intersection, return -1

            if (d < 0.0) return -1.0;

            // Return the distance to the [first] intersecting point

            return v - sqrt(d);
             * */
            return 0;
        } 

        public void testCollision(List<Entity> entities)
        {
            //Assumes that the state checks velocity to see if anything is actually moving
            //Assumes that the state checks AABBs to see if testing collisions makes sense

            float distanceToNearest = -1;
            bool hasCollided = false;

            foreach (Entity entity in entities) 
            {
                if (entity is SliderEntity)
                {
                    SliderEntity slider = (SliderEntity)entity;
                    Matrix transformSliderToLocal = getWorldTransform() * Matrix.Invert(slider.getWorldTransform());

                    //find the SliderEntity's end points and normal
                    Vector2[] sliderEndPoints = {
                        Vector2.Transform(slider.endPoints[0], transformSliderToLocal),
                        Vector2.Transform(slider.endPoints[1], transformSliderToLocal)
                                                };
                    Vector2 sliderNormal = Vector2.Transform((sliderEndPoints[1] - sliderEndPoints[0]), 
                        Matrix.CreateRotationZ((float)(Math.PI/2.0f)));

                    //TODO: ignoring plane embedded in ellipse FOR NOW

                    //Calculate the ellipse intersection point
                    Vector2 ellipseRadiusVector = new Vector2(-sliderNormal.X * collisionEllipse.X,
                                                              -sliderNormal.Y * collisionEllipse.Y);
                    Vector2 ellipseIntersectionPoint = ellipseRadiusVector;

                    //calculate the plane intersection point
                    float t = intersect(ellipseIntersectionPoint, velocity, sliderEndPoints[0], sliderNormal);
                    Vector2 lineIntersectionPoint = ellipseIntersectionPoint + Vector2.Normalize(velocity) * t;

                    //check if our line intersection point is the same as our line segment intersection point
                    if (lineIntersectionPoint.X < sliderEndPoints[0].X ||
                        lineIntersectionPoint.Y < sliderEndPoints[0].Y )
                    {
                        lineIntersectionPoint = sliderEndPoints[0];
                    }
                    else if (lineIntersectionPoint.X > sliderEndPoints[1].X ||
                             lineIntersectionPoint.Y > sliderEndPoints[1].Y )
                    {
                        lineIntersectionPoint = sliderEndPoints[1];
                    }

                    //finally, are we intersecting?
                    t = 0;// intersectEllipse(new Vector2(0, 0), ellipseRadiusVector, lineIntersectionPoint, -velocity);
                    if (t >= 0.0f && t <= velocity.Length() &&
                        (t < distanceToNearest || !hasCollided))
                    {
                        distanceToNearest = t;
                        hasCollided = true;
                    }
                }
                else //entity is EllipseEntity
                {
                }
            }

            if (!hasCollided)
            {
                //TRANSLATE
            }
        }
    }
}
