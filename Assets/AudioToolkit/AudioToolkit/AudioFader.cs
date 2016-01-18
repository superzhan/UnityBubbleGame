using UnityEngine;
using System.Collections;

#pragma warning disable 1591 // undocumented XML code warning

public class AudioFader 
{
    float _fadeOutTotalTime = -1;
    double _fadeOutStartTime = -1;

    float _fadeInTotalTime = -1;
    double _fadeInStartTime = -1;

    public double time
    {
        get;
        set;
    }

    public bool isFadingOutComplete
    {
        get
        {
            if ( _fadeOutStartTime > 0 )
            {
                return _fadeOutTotalTime >= 0 && time >= _fadeOutStartTime + _fadeOutTotalTime;
            } else
                return _fadeOutTotalTime >= 0 && time >= _fadeOutTotalTime;
        }
    }

    public bool isFadingOut
    {
        get
        {
            if ( _fadeOutStartTime > 0 )
            {
                return _fadeOutTotalTime >= 0 && time >= _fadeOutStartTime && time < _fadeOutStartTime + _fadeOutTotalTime;
            } else
                return _fadeOutTotalTime >= 0 && time < _fadeOutTotalTime;
        }
    }

    public bool isFadingOutOrScheduled
    {
        get
        {
            return _fadeOutTotalTime >= 0;
        }
    }

    public bool isFadingIn
    {
        get
        {
            if( _fadeInStartTime > 0 )
            {
                return _fadeInTotalTime > 0 && time >= _fadeInStartTime && ( time - _fadeInStartTime ) < _fadeInTotalTime;
            } else 
                return _fadeInTotalTime > 0 && time < _fadeInTotalTime;
        }
    }

    public void Set0()
    {
        time = 0;

        _fadeOutTotalTime = -1;
        _fadeOutStartTime = -1;

        _fadeInTotalTime = -1;
        _fadeInStartTime = -1;
    }

    public void FadeIn( float fadeInTime, bool stopCurrentFadeOut = false )
    {
        FadeIn( fadeInTime, time, stopCurrentFadeOut );
    }

    public void FadeIn( float fadeInTime, double startToFadeTime, bool stopCurrentFadeOut = false )
    {
        if ( isFadingOutOrScheduled && stopCurrentFadeOut )
        {
            float fo = _GetFadeOutValue();

            _fadeOutTotalTime = -1;
            _fadeOutStartTime = -1;

            _fadeInTotalTime = fadeInTime;
            _fadeInStartTime = startToFadeTime - fadeInTime * fo; // combine with fade-out (Fade-in is faster if fadeout was still running)
        }
        else
        {
            _fadeInTotalTime = fadeInTime;
            _fadeInStartTime = startToFadeTime;
        }
    }

    public void FadeOut( float fadeOutLength, float startToFadeTime )
    {
        if ( isFadingOutOrScheduled )
        {
            double requestedEndOfFadeout = time + startToFadeTime + fadeOutLength;
            double currentEndOfFadeout = _fadeOutStartTime + _fadeOutTotalTime;

            if ( currentEndOfFadeout < requestedEndOfFadeout )
            {
                // current fade-out is already faster than the requested fade-out
                return;
            }
            else
            {
                // combine the two fade-outs
                double alreadyFadedTime = time - _fadeOutStartTime;
                double timeToFinishFade = startToFadeTime + fadeOutLength;

                double currentTimeToFinishFade = currentEndOfFadeout - time;

                double newFadedTime;

                if ( currentTimeToFinishFade != 0 )
                {
                    newFadedTime = alreadyFadedTime * timeToFinishFade / currentTimeToFinishFade;

                    _fadeOutStartTime = time - newFadedTime;
                    _fadeOutTotalTime = (float) ( timeToFinishFade + newFadedTime );
                }
            }

        }
        else
        {
            _fadeOutTotalTime = fadeOutLength;
            _fadeOutStartTime = time + startToFadeTime;
        }
    }

    public float Get( out bool finishedFadeOut )
    {
        float fadeVolume = 1;

        finishedFadeOut = false;

        if ( isFadingOutOrScheduled )
        {
            fadeVolume *= _GetFadeOutValue();

            if ( fadeVolume == 0 )
            {
                finishedFadeOut = true; 
                return 0;
            }
        }

        if ( isFadingIn )
        {
            fadeVolume *= _GetFadeInValue();
        }
        return fadeVolume;
    }

    private float _GetFadeOutValue()
    {
        return 1.0f - _GetFadeValue( (float) ( time - _fadeOutStartTime ), _fadeOutTotalTime );
    }

    private float _GetFadeInValue()
    {
        return _GetFadeValue( (float) ( time - _fadeInStartTime ), _fadeInTotalTime );
    }

    private float _GetFadeValue( float t, float dt )
    {
        if ( dt <= 0 )
        {
            return t > 0 ? 1.0f : 0;
        }
        return Mathf.Clamp( t / dt, 0.0f, 1.0f );
    }
}
