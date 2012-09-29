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

        private float intersectEllipse(Vector2 ellipseOrigin, Vector2 ellipseRadius, 
            Vector2 rayOrigin, Vector2 rayVector)
        {
            float aa, bb, cc, m;
            float t = -1;
            float a = ellipseRadius.X;
            float b = ellipseRadius.Y;
            float h = ellipseOrigin.X;
            float k = ellipseOrigin.Y;
            //
            if ( rayVector.X != 0 )
            {
                m = rayVector.Y/rayVector.X;
                float c = rayOrigin.Y - m * rayOrigin.X;
                //
                aa = b*b + a*a*m*m;
                bb = 2*a*a*c*m - 2*a*a*k*m - 2*h*b*b;
                cc = b*b*h*h + a*a*c*c - 2*a*a*k*c + a*a*k*k - a*a*b*b;
            }
            else
            {
                //
                // vertical line case
                //
                aa = a*a;
                bb = -2.0f*k*a*a;
                cc = -a*a*b*b + b*b*(ellipseOrigin.X-h)*(ellipseOrigin.X-h);
            }

            float d = bb*bb-4*aa*cc;
            //
            // intersection points : (xi1,yi1) and (xi2,yi2)
            //
            if (d > 0.0)
            {
                float t1 = -1;
                float t2 = -1;
                if ( rayVector.X != 0 )
                {
                    //xi1 = rayOrigin.X + t * rayVector.X
                    //=>
                    //t = (xi1 - rayOrigin.X)/rayVector.X

                    float xi1 = (-bb + (float)Math.Sqrt(d)) / (2 * aa);
                    float xi2 = (-bb - (float)Math.Sqrt(d)) / (2 * aa);
                    t1 = (xi1 - rayOrigin.X) / rayVector.X;
                    t2 = (xi2 - rayOrigin.X) / rayVector.X;
                }
                else
                {
                    float yi1 = (-bb + (float)Math.Sqrt(d)) / (2 * aa);
                    float yi2 = (-bb - (float)Math.Sqrt(d)) / (2 * aa);
                    t1 = (yi1 - rayOrigin.X) / rayVector.X;
                    t2 = (yi2 - rayOrigin.X) / rayVector.X;
                }

                if (t1 < t2)
                {
                    t = t1;
                }
                else
                {
                    t = t2;
                }
            }
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
                        t = intersectEllipse(new Vector2(0, 0), ellipseRadiusVector, lineIntersectionPoint, -velocity);
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
