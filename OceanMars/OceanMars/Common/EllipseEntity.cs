using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    class EllipseEntity : Entity
    {
        private const float BIG_FUZZY_EPSILON = 20;
        private const float FUZZY_EPSILON = 0.0001f;
        public Vector2 collisionEllipse;

        public EllipseEntity(Vector2 size, Entity parent) : base(size, parent)
        {
            collisionEllipse = size/2.0f;
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


        //intersect(shortestSliderIntersectionPoint, shortestSliderNormal, velocity, shortestSliderNormal);
        private float intersect(Vector2 planeOrigin, Vector2 planeNormal, Vector2 rayOrigin, Vector2 rayVector)
        {
            //Assuming normal and vector are normalized
            float d = -Vector2.Dot(planeNormal, planeOrigin);
            float numer = Vector2.Dot(planeNormal, rayOrigin) + d;
            float denom = Vector2.Dot(planeNormal, rayVector);
            return -(numer / denom);
        }

        private float intersectEllipse(Vector2 ellipseRadius, Vector2 rayOrigin)
        {
            //Assuming the ellipse is at the origin
            float t = -(rayOrigin.X - ellipseRadius.X)/ellipseRadius.X;
            return t;
        } 

        public void testCollision(List<Entity> entities)
        {
            //Assumes that the state checks velocity to see if anything is actually moving
            //Assumes that the state checks AABBs to see if testing collisions makes sense

            Vector2 normalizedVelocity = new Vector2(0, 0);
            Vector2 shortestSliderNormal = new Vector2(0, 0);
            Vector2 shortestSliderIntersectionPoint = new Vector2(0, 0);
            float distanceToNearest = -1;
            bool TEMP_NAME_STOP = false;

            while (!TEMP_NAME_STOP)
            {
                bool hasCollided = false;
                foreach (Entity entity in entities)
                {
                    if (entity is SliderEntity)
                    {
                        SliderEntity slider = (SliderEntity)entity;
                        Vector2 lineIntersectionPoint = new Vector2(0, 0);
                        Matrix transformSliderToLocal = slider.worldTransform * inverseWorldTransform;

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
                        float distance = intersect(new Vector2(0, 0), -sliderNormal, sliderEndPoints[0], sliderNormal);
                        if (Math.Abs(distance) <= ellipseRadiusVector.Length())
                        {
                            lineIntersectionPoint = -sliderNormal * distance;
                        }
                        else
                        {
                            Vector2 ellipseIntersectionPoint = ellipseRadiusVector;
                            normalizedVelocity = Vector2.Normalize(velocity);

                            //calculate the plane intersection point
                            float d = intersect(sliderEndPoints[0], sliderNormal, ellipseIntersectionPoint, normalizedVelocity);

                            if (Math.Abs(d) <= velocity.Length())
                            {
                                lineIntersectionPoint = ellipseIntersectionPoint + normalizedVelocity * d;

                                //check if our line intersection point is the same as our line segment intersection point
                                lineIntersectionPoint = getClosestPointOnLineSegment(lineIntersectionPoint, sliderEndPoints);
                            }
                        }

                        //finally, are we intersecting?
                        float t = intersectEllipse(ellipseRadiusVector, lineIntersectionPoint);
                        if (t <= velocity.Length() &&
                            (t < distanceToNearest || !hasCollided))
                        {
                            distanceToNearest = t - FUZZY_EPSILON;
                            hasCollided = true;
                            shortestSliderNormal = sliderNormal;
                            shortestSliderIntersectionPoint = lineIntersectionPoint;
                        }
                    }
                    else //entity is EllipseEntity
                    {
                    }
                }

                if (hasCollided)
                {
                    transform = transform *
                                Matrix.CreateTranslation(new Vector3(distanceToNearest * velocity.X,
                                                                     distanceToNearest * velocity.Y, 0));

                    float t = intersect(shortestSliderIntersectionPoint, shortestSliderNormal,
                                                        velocity, shortestSliderNormal);
                    shortestSliderNormal = shortestSliderNormal / shortestSliderNormal.Length() * t;
                    velocity = velocity + shortestSliderNormal - shortestSliderIntersectionPoint;
                    if (velocity.Length() < FUZZY_EPSILON)
                    {
                        TEMP_NAME_STOP = true;
                    }
                }
                else
                {
                    transform = Matrix.CreateTranslation(new Vector3(velocity.X, velocity.Y, 0)) * transform;
                    TEMP_NAME_STOP = true;
                }
            }
        }
    }
}
