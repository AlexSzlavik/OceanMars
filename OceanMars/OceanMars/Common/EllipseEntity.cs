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

        public EllipseEntity(Vector2 size, Entity parent) : base(size, parent)
        {
            collisionEllipse = size;
        }

        
        private Vector2 calculateCloserPoint(Vector2 lineIntersectionPoint, 
                                             Vector2 firstPoint,
                                             Vector2 secondPoint)
        {
            Vector2 returnPoint;
            if (firstPoint.X != secondPoint.X)
            {
                if (Math.Abs(firstPoint.X - lineIntersectionPoint.X) >
                    Math.Abs(secondPoint.X - lineIntersectionPoint.X))
                {
                    returnPoint = secondPoint;
                }
                else
                {
                    returnPoint = firstPoint;
                }
            }
            else
            {
                if (Math.Abs(firstPoint.Y - lineIntersectionPoint.Y) >
                    Math.Abs(secondPoint.Y - lineIntersectionPoint.Y))
                {
                    returnPoint = secondPoint;
                }
                else
                {
                    returnPoint = firstPoint;
                }

            }
            return returnPoint;
        }

        private Vector2 getClosestPointOnLineSegment(Vector2 lineIntersectionPoint,
                                                     Vector2[] segmentEndPoints)
        {
            Vector2 returnPoint = lineIntersectionPoint;
            if (segmentEndPoints[0].X != segmentEndPoints[1].X)
            {
                if (lineIntersectionPoint.X > segmentEndPoints[0].X)
                {
                    if (!(lineIntersectionPoint.X < segmentEndPoints[1].X))
                    {
                        returnPoint =
                            calculateCloserPoint(lineIntersectionPoint,
                                                 segmentEndPoints[0],
                                                 segmentEndPoints[1]);
                    }
                }
                else if (lineIntersectionPoint.X > segmentEndPoints[1].X)
                {
                    if (!(lineIntersectionPoint.X < segmentEndPoints[0].X))
                    {
                        returnPoint =
                            calculateCloserPoint(lineIntersectionPoint,
                                                 segmentEndPoints[0],
                                                 segmentEndPoints[1]);
                    }
                }
            }
            else //if our line is vertical
            {
                if (lineIntersectionPoint.Y > segmentEndPoints[0].Y)
                {
                    if (!(lineIntersectionPoint.Y < segmentEndPoints[1].Y))
                    {
                        returnPoint =
                            calculateCloserPoint(lineIntersectionPoint,
                                                 segmentEndPoints[0],
                                                 segmentEndPoints[1]);
                    }
                }
                else if (lineIntersectionPoint.Y > segmentEndPoints[1].Y)
                {
                    if (!(lineIntersectionPoint.Y < segmentEndPoints[0].Y))
                    {
                        returnPoint =
                            calculateCloserPoint(lineIntersectionPoint,
                                                 segmentEndPoints[0],
                                                 segmentEndPoints[1]);
                    }
                }
            }
            return returnPoint;
        }


        private float intersect(Vector2 planeOrigin, Vector2 planeNormal, Vector2 rayOrigin, Vector2 rayVector)
        {
            rayVector.Normalize();
            float d = -Vector2.Dot(planeNormal, planeOrigin);
            float numer = Vector2.Dot(planeNormal, rayOrigin) + d;
            float denom = Vector2.Dot(planeNormal, rayVector);
            return -(numer / denom);
        }

        private float intersectEllipse(Vector2 ellipseRadius, Vector2 rayOrigin, Vector2 rayVector)
        {
            //Assuming the ellipse is at the origin
            float t = -1;
            if (rayVector.X != 0)   //TODO: change to > FUZZY_EPSILON
                t = (rayOrigin.X - ellipseRadius.X) / rayVector.X;
            else if (rayVector.Y != 0)   //TODO: change to > FUZZY_EPSILON
                t = (rayOrigin.Y - ellipseRadius.Y) / rayVector.Y;
            return t;
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
                    Matrix transformSliderToLocal = worldTransform * Matrix.Invert(slider.worldTransform);

                    //find the SliderEntity's end points and normal
                    Vector2[] sliderEndPoints = {
                        Vector2.Transform(slider.endPoints[0], transformSliderToLocal),
                        Vector2.Transform(slider.endPoints[1], transformSliderToLocal)
                                                };
                    //TODO: Unit normal
                    Vector2 sliderNormal = Vector2.Transform((sliderEndPoints[1] - sliderEndPoints[0]), 
                        Matrix.CreateRotationZ((float)(Math.PI/2.0f)));
                    sliderNormal.Normalize();

                    //TODO: ignoring plane embedded in ellipse FOR NOW

                    //Calculate the ellipse intersection point
                    Vector2 ellipseRadiusVector = new Vector2(-sliderNormal.X * collisionEllipse.X,
                                                              -sliderNormal.Y * collisionEllipse.Y);
                    Vector2 ellipseIntersectionPoint = ellipseRadiusVector;

                    //calculate the plane intersection point
                    float t = intersect(sliderEndPoints[0], sliderNormal, ellipseIntersectionPoint, velocity);
                    if (Math.Abs(t) <= velocity.Length()) //TODO: ignoring plane embedded in ellipse FOR NOW
                    {
                        Vector2 lineIntersectionPoint = ellipseIntersectionPoint + Vector2.Normalize(velocity) * t;

                        //check if our line intersection point is the same as our line segment intersection point
                        lineIntersectionPoint = getClosestPointOnLineSegment(lineIntersectionPoint, sliderEndPoints);
 
                        //finally, are we intersecting?
                        t = intersectEllipse(ellipseRadiusVector, lineIntersectionPoint, -velocity);
                        if (t >= 0.0f && t <= velocity.Length() &&
                            (t < distanceToNearest || !hasCollided))
                        {
                            distanceToNearest = t;
                            hasCollided = true;
                        }
                    }
                }
                else //entity is EllipseEntity
                {
                }
            }

            if (hasCollided)
            {
                //TRANSLATE
                // WARNING: For Testing Purposes
                System.Diagnostics.Debug.WriteLine("There is a collision");
            }
        }
    }
}
