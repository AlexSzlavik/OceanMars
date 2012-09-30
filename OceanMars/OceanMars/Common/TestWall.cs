using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    class TestWall : SliderEntity
    {

            public TestWall (Entity parent, Vector2 point1, Vector2 point2) : base (new Vector2(-1, 0), new Vector2(1, 0), parent) {
                double length = 1;
                double angle = 0;
                Vector2 centre = new Vector2 (0, 0);

                friction = 0.90f;

                // Calculate the distance between two points for scale
                length = Math.Sqrt(Math.Pow((point2.Y - point1.Y), 2) + Math.Pow((point2.X - point1.X), 2));

                Vector2 intendedVec = point2 - point1;
                intendedVec.Normalize();
                Vector2 actualVec = Vector2.UnitX;

                // Calculate angle between vectors
                angle = Math.Acos(Vector2.Dot(intendedVec, actualVec));
                float sign = Vector3.Cross(new Vector3(intendedVec.X, intendedVec.Y, 0),
                    new Vector3(actualVec.X, actualVec.Y, 0)).Z;

                if (sign < 0) angle = 2 * Math.PI - angle;
                
                // Calculate the distance from the centre of the line to the origin
                centre.X = (point1.X + point2.X) / 2;
                centre.Y = (point1.Y + point2.Y) / 2;
                
                // Apply the transforms
                this.transform =
                    Matrix.CreateScale((float)length/2, 1, 1) * //NB: We divide by 2 because the length starts at 2
                    Matrix.CreateRotationZ((float)angle) *
                    Matrix.CreateTranslation(centre.X, -1*centre.Y, 0); //NB: Multiply the y value by -1 because y goes down when drawing
            }
    }
}
