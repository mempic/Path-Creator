﻿///
///
/// @copyright (c) by MEMPIC LTD
/// @copyright (c) by WWW.MEMPIC.COM
///
///
/// @license http://www.mempic.com/licenses/private
///
/// By exercising the licensed rights you accept and agree to be bound by the
/// terms and conditions of this @license. To the extent this @license
/// may be interpreted as a contract, you are granted the licensed rights
/// in consideration of your acceptance of these terms and conditions,
/// and the licensor grants you such rights in consideration of benefits
/// the licensor receives from making the licensed material available
/// under these terms and conditions.
///
///
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace PathCreation
{
  public partial class BezierPath
  {
    protected bool isCleared;
    protected float previousBezierFinishNormalAngle;

    public BezierPath(
      IEnumerable<Vector3> points,
      bool isClosed                     = false,
      PathSpace space                   = PathSpace.xyz,
      ControlMode controlMode           = ControlMode.Automatic,
      List<float> perAnchorNormalsAngle = null
    )
    {
      var pointsArray = points.ToArray();

      if(pointsArray.Length < 2)
      {
        Debug.LogError("Path requires at least 2 anchor points.");
      }
      else
      {
        this.controlMode = controlMode;
        this.perAnchorNormalsAngle = perAnchorNormalsAngle;

        switch(this.controlMode)
        {
          default:
            this.points = points.ToList();
            break;
          case ControlMode.Automatic:
            this.points = new List<Vector3>
            {
              pointsArray[0],
              Vector3.zero,
              Vector3.zero,
              pointsArray[1]
            };

            for(var i = 2; i < pointsArray.Length; i++)
            {
              AddSegmentToEnd(pointsArray[i]);
              perAnchorNormalsAngle.Add(0);
            }
            break;
        }
      }

      this.Space = space;
      this.IsClosed = isClosed;
    }

    public BezierPath(BezierPath target)
    {
      this.points = target.points;
    }

    public void ClearPath()
    {
      int minPointsCount = 4;

      int attempt = 0;
      while(points.Count > minPointsCount && ++attempt < 10000)
      {
        DeleteSegment(0);
      }

      for(int i = 0; i < NumPoints; i++)
      {
        SetPoint(i, Vector3.forward * i);
      }

      perAnchorNormalsAngle.Clear();
      for(int i = 0; i < NumAnchorPoints; i++)
      {
        perAnchorNormalsAngle.Add(0);
      }

      this.previousBezierFinishNormalAngle = 0;
      this.isCleared = true;
    }

    public void EncapsulatePath(PathCreator pathCreator)
    {
      var bezierPath = pathCreator.bezierPath;

      if(!isCleared)
      {
        Vector3 lastAnchorSecondControl = points[points.Count - 1];
        Vector3 firstAnchorSecondControl = points[points.Count - 1];

        points.Add(lastAnchorSecondControl);
        points.Add(firstAnchorSecondControl);
      }
      else
      {
        perAnchorNormalsAngle.Clear();
      }

      for(int i = 0; i < bezierPath.NumPoints; i++)
      {
        var point = pathCreator.transform.TransformPoint(bezierPath.GetPoint(i));

        if(!isCleared)
        {
          points.Add(point);

          if(i == 1)
          {
            //soft direction after merge based on previous control points
            var endConnectionPoint = points[points.Count - 5] - points[points.Count - 6];
            var startConnectionPoint = points[points.Count - 2] - points[points.Count - 1];
            var anchorsDistance = points[points.Count - 2] - points[points.Count - 5];

            SetPoint(points.Count - 4, points[points.Count - 5] + endConnectionPoint.normalized * anchorsDistance.magnitude * 0.4f);
            SetPoint(points.Count - 3, points[points.Count - 2] + startConnectionPoint.normalized * anchorsDistance.magnitude * 0.4f);
          }
        }
        else
        {
          SetPoint(i, point);
        }

        if(isCleared && i == 3)
        {
          isCleared = false;
        }

        if(i % 3 == 0)
        {
          var normalsDifference = this.globalNormalsAngle - bezierPath.globalNormalsAngle;
          var angle = bezierPath.perAnchorNormalsAngle[i / 3] + normalsDifference + this.previousBezierFinishNormalAngle;

          perAnchorNormalsAngle.Add(angle);

          if(i == bezierPath.NumPoints - 1)
          {
            this.previousBezierFinishNormalAngle = angle;
          }
        }
      }

      NotifyPathModified();
    }

    public void EncapsulatePathWithoutDuplicates(PathCreator pathCreator)
    {
      var bezierPath = pathCreator.bezierPath;

      // Trying to find any duplicates and remove it segment if found
      for(int i = 0; i < bezierPath.NumPoints; i++)
      {
        var point = pathCreator.transform.TransformPoint(bezierPath.GetPoint(i));
        var epsilon = 0.1f;

        if(Vector3.Distance(point, this.GetPoint(this.NumPoints - 1)) < epsilon)
        {
          bezierPath.DeleteSegment(0);
        }
      }

      this.EncapsulatePath(pathCreator);
    }
  }
}
