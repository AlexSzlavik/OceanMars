using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    class EllipseEntity : Entity
    {
        private const float BIG_FUZZY_EPSILON = 0.5f;
        private const float FUZZY_EPSILON = 0.01f;
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
            Vector2 firstVector = firstPoint - lineIntersectionPoint;
            Vector2 secondVector = secondPoint - lineIntersectionPoint;
            if (firstVector.Length() > secondVector.Length())
            {
                returnPoint = secondPoint;
            }
            else
            {
                returnPoint = firstPoint;
            }
            return returnPoint;
        }

        private Vector2 getClosestPointOnLineSegment(Vector2 lineIntersectionPoint,
                                                     Vector2[] segmentEndPoints,
                                                    out bool inside)
        {
            Vector2 returnPoint = lineIntersectionPoint;
            inside = true;
            if (segmentEndPoints[0].X != segmentEndPoints[1].X)
            {
                if (lineIntersectionPoint.X > segmentEndPoints[0].X)
                {
                    if (lineIntersectionPoint.X > segmentEndPoints[1].X)
                    {
                        inside = false;
                        returnPoint =
                            calculateCloserPoint(lineIntersectionPoint,
                                                 segmentEndPoints[0],
                                                 segmentEndPoints[1]);
                    }
                }
                else if (lineIntersectionPoint.X < segmentEndPoints[1].X)
                {
                    if (lineIntersectionPoint.X < segmentEndPoints[0].X)
                    {
                        inside = false;
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
                    if (lineIntersectionPoint.Y > segmentEndPoints[1].Y)
                    {
                        inside = false;
                        returnPoint =
                            calculateCloserPoint(lineIntersectionPoint,
                                                 segmentEndPoints[0],
                                                 segmentEndPoints[1]);
                    }
                }
                else if (lineIntersectionPoint.Y < segmentEndPoints[1].Y)
                {
                    if (lineIntersectionPoint.Y < segmentEndPoints[0].Y)
                    {
                        inside = false;
                        returnPoint =
                            calculateCloserPoint(lineIntersectionPoint,
                                                 segmentEndPoints[0],
                                                 segmentEndPoints[1]);
                    }
                }
            }
            return returnPoint;
        }


        //intersect(new Vector2(0, 0), -sliderNormal, sliderEndPoints[0], sliderNormal);
        private float intersect(Vector2 planeOrigin, Vector2 planeNormal, Vector2 rayOrigin, Vector2 rayVector)
        {
            //Assuming normal and vector are normalized
            float d = -Vector2.Dot(planeNormal, planeOrigin);
            float numer = Vector2.Dot(planeNormal, rayOrigin) + d;
            float denom = Vector2.Dot(planeNormal, rayVector);
            return -(numer / denom);
        }

        //private float intersectEllipse(Vector2 ellipseRadius, Vector2 rayOrigin)
        //{
        //    //Assuming the ellipse is at the origin
        //    float t = -(rayOrigin.X - ellipseRadius.X)/ellipseRadius.X;
        //    return t;
        //} 

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
                    + ((ray_normal.Y*ray_normal.Y)/(ellipsoid_radius.Y*ellipsoid_radius.Y))
                    + ((ray_normal.Z*ray_normal.Z)/(ellipsoid_radius.Z*ellipsoid_radius.Z));
            float b = ((2 * ray_origin.X * ray_normal.X) / (ellipsoid_radius.X * ellipsoid_radius.X))
                    + ((2*ray_origin.Y*ray_normal.Y)/(ellipsoid_radius.Y*ellipsoid_radius.Y))
                    + ((2*ray_origin.Z*ray_normal.Z)/(ellipsoid_radius.Z*ellipsoid_radius.Z));
            float c = ((ray_origin.X * ray_origin.X) / (ellipsoid_radius.X * ellipsoid_radius.X))
                    + ((ray_origin.Y*ray_origin.Y)/(ellipsoid_radius.Y*ellipsoid_radius.Y))
                    + ((ray_origin.Z*ray_origin.Z)/(ellipsoid_radius.Z*ellipsoid_radius.Z))
                    - 1;

            float d = ((b * b) - (4 * a * c));

	        // Check for actual intersection (if b^2 - 4ac < 0)
            if ( d < 0 ) { return -1; }
            else { d = (float)Math.Sqrt(d); }
            float hit = (-b + d)/(2*a);
            float hitsecond = (-b - d) / (2 * a);

            if( hit < hitsecond) { return hit; }
            else { return hitsecond; }
        }

        public void testCollision(List<Entity> entities)
        {
            //Assumes that the state checks velocity to see if anything is actually moving
            //Assumes that the state checks AABBs to see if testing collisions makes sense

            while (velocity.Length() >= FUZZY_EPSILON)
            {
                Vector2 normalizedVelocity = new Vector2(0, 0);
                SliderEntity shortestSlider = null;
                Vector2 shortestSliderNormal = new Vector2(0, 0);
                Vector2 shortestSliderIntersectionPoint = new Vector2(0, 0);
                float distanceToNearest = -1;
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

                        //if we're moving in the same direction as the normal, we shouldn't collide, so keep going
                        if ((sliderNormal.Y < 0) == (velocity.Y < 0))
                            continue;

                        sliderNormal.Normalize();

                        //Calculate the ellipse intersection point
                        Vector2 ellipseRadiusVector = new Vector2(-sliderNormal.X * collisionEllipse.X,
                                                                 -sliderNormal.Y * collisionEllipse.Y);

                        float t = 0;
                        //is the plane embedded in ellipse?
                        float distance = intersect(sliderEndPoints[0], sliderNormal, new Vector2(0, 0), -sliderNormal);
                        if (Math.Abs(distance) <= ellipseRadiusVector.Length())
                        {
                            lineIntersectionPoint = -sliderNormal * distance;
                        }
                        else
                        {
                            Vector2 ellipseIntersectionPoint = ellipseRadiusVector;
                            normalizedVelocity = Vector2.Normalize(velocity);

                            //calculate the plane intersection point
                            t = intersect(sliderEndPoints[0], sliderNormal, ellipseIntersectionPoint, normalizedVelocity);

                            lineIntersectionPoint = ellipseIntersectionPoint + normalizedVelocity * t;
                        }

                        //check if our line intersection point is the same as our line segment intersection point
                        bool inside;
                        lineIntersectionPoint = getClosestPointOnLineSegment(lineIntersectionPoint, sliderEndPoints, out inside);

                        if (!inside)
                        {
                            t = intersectEllipsoid(Vector3.Zero, new Vector3(ellipseRadiusVector.X, ellipseRadiusVector.Y, 1),
                                                              new Vector3(lineIntersectionPoint.X, lineIntersectionPoint.Y, 0),
                                                              new Vector3(-velocity.X, -velocity.Y, 0));
                        }

                        //finally, are we intersecting?
                        if (t >= 0 && t <= velocity.Length() &&
                            (t < distanceToNearest || !hasCollided))
                        {
                            distanceToNearest = t;
                            hasCollided = true;
                            shortestSlider = slider;
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
                    float dist = Math.Max(distanceToNearest - FUZZY_EPSILON, 0.0f);

                    Vector2 truncatedVelocity = new Vector2(dist * normalizedVelocity.X,
                                                            dist * normalizedVelocity.Y);

                    Vector3 newSource = new Vector3(truncatedVelocity.X,
                                                    truncatedVelocity.Y, 0);
                    transform = transform * Matrix.CreateTranslation(newSource);

                    //new Vector2(newSource.X, newSource.Y)
                    float t = intersect(shortestSliderIntersectionPoint, shortestSliderNormal,
                                                        velocity, shortestSliderNormal);
                    shortestSliderNormal = shortestSliderNormal / shortestSliderNormal.Length() * t;
                    velocity = velocity + shortestSliderNormal - shortestSliderIntersectionPoint;

                    velocity = shortestSlider == null ? velocity : shortestSlider.applyFriction(velocity);

                    //TODO: SHOULD PROBABLY MOVE; MAKE A CONSTANT VAR
                    //test if we're still jumping
                    if (Vector2.Dot(shortestSliderNormal, new Vector2(1, 0)) < 0.5f)
                    {
                        inAir = false;
                    }

                }
                else
                {
                    transform = Matrix.CreateTranslation(new Vector3(velocity.X, velocity.Y, 0)) * transform;
                    break;
                }
            }
        }
    }
}
