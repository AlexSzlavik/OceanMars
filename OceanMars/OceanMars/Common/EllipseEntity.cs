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

        public float intersectEllipse(Vector2 ellipseOrigin, Vector2 ellipseRadius, 
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
                    t = intersectEllipse(new Vector2(0, 0), ellipseRadiusVector, lineIntersectionPoint, -velocity);
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
