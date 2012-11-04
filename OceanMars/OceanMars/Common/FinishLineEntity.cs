using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    public class FinishLineEntity : LineEntity
    {
        public FinishLineEntity(Vector2 endPoint1, Vector2 endPoint2, Entity parent)
            : base(new Vector2(-1, 0), new Vector2(1, 0), parent)
        {
            //Drawing hack? (definitely a hack, look at what I'm passing to the base... wtf?)
            double length = 1;
            double angle = 0;
            Vector2 centre = new Vector2(0, 0);

            // Calculate the distance between two endPoints for scale
            length = Math.Sqrt(Math.Pow((endPoint2.Y - endPoint1.Y), 2) + Math.Pow((endPoint2.X - endPoint1.X), 2));

            Vector2 intendedVec = endPoint2 - endPoint1;
            intendedVec.Normalize();
            Vector2 actualVec = Vector2.UnitX;

            // Calculate angle between vectors
            angle = Math.Acos(Vector2.Dot(intendedVec, actualVec));
            float sign = Vector3.Cross(new Vector3(intendedVec.X, intendedVec.Y, 0),
                new Vector3(actualVec.X, actualVec.Y, 0)).Z;

            if (sign < 0) angle = 2 * Math.PI - angle;

            // Calculate the distance from the centre of the line to the origin
            centre.X = (endPoint1.X + endPoint2.X) / 2;
            centre.Y = (endPoint1.Y + endPoint2.Y) / 2;

            // Apply the transforms
            this.transform =
                Matrix.CreateScale((float)length / 2, 1, 1) * //NB: We divide by 2 because the length starts at 2
                Matrix.CreateRotationZ((float)angle) *
                Matrix.CreateTranslation(centre.X, -1 * centre.Y, 0); //NB: Multiply the y value by -1 because y goes down when drawing
        }
    }
}
