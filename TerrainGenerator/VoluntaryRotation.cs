using System;
using UnityEngine;

namespace Fujin.TerrainGenerator
{
    public class VoluntaryRotation : MonoBehaviour
    {
        [SerializeField] private bool stopRotation;
        private const float Duration = 2f;
        private float _elapsedTime;
        private Phase _currentPhase = Phase.RotateX;
        private readonly int _phaseCount = Enum.GetValues(typeof(Phase)).Length;
        private enum Phase
        {
            RotateX,
            RotateY,
            RotateZ,
        }

        private void Update()
        {
            if (!stopRotation)
            {
                _elapsedTime += Time.deltaTime;
                if (_elapsedTime >= Duration)
                {
                    _elapsedTime = 0f;
                    _currentPhase = (Phase)Enum.ToObject(typeof(Phase), ((int)_currentPhase+1) % _phaseCount);
                }

                switch (_currentPhase)
                {
                    case Phase.RotateX:
                        transform.rotation = Quaternion.Euler(360f * _elapsedTime / Duration, 0, 0);
                        break;
                    case Phase.RotateY:
                        transform.rotation = Quaternion.Euler(0, 360f * _elapsedTime / Duration, 0);
                        break;
                    case Phase.RotateZ:
                        transform.rotation = Quaternion.Euler(0, 0, 360f * _elapsedTime / Duration);
                        break;
                }
            }
        }
    }
}