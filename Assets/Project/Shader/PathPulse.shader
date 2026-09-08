Shader "Custom/URP/PathPulse"
{
    Properties
    {
        [MainColor]
        _BaseColor ("Base Color", Color) = (0.15, 0.65, 1.0, 1.0)

        _PulseColor ("Pulse Color", Color) = (1.0, 1.0, 1.0, 1.0)

        [Header(Pulse Settings)]

        _PulseSpeed ("Pulse Speed", Float) = 0.5

        [Range(0.001, 0.5)]
        _PulseWidth ("Pulse Width", Range(0.001, 0.5)) = 0.05

        [Range(0.001, 0.5)]
        _PulseSoftness ("Pulse Softness", Range(0.001, 0.5)) = 0.10

        [Range(0.0, 10.0)]
        _PulseIntensity ("Pulse Intensity", Range(0.0, 10.0)) = 3.0

        [Header(Base)]

        [Range(0.0, 2.0)]
        _BaseBrightness ("Base Brightness", Range(0.0, 2.0)) = 1.0

        [Header(Direction)]

        [Toggle]
        _ReverseDirection ("Reverse Direction", Float) = 0

        [Header(Movement Axis)]

        [Enum(X Axis, 0, Y Axis, 1, Z Axis, 2)]
        _MovementAxis ("Movement Axis", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "PathPulse"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ------------------------------------------------------------
            // Vertex Input
            // ------------------------------------------------------------

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };


            // ------------------------------------------------------------
            // Vertex Output
            // ------------------------------------------------------------

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                half pathProgress : TEXCOORD2;
            };


            // ------------------------------------------------------------
            // Material Properties
            // ------------------------------------------------------------

            CBUFFER_START(UnityPerMaterial)

                float4 _BaseColor;
                float4 _PulseColor;

                float _PulseSpeed;
                float _PulseWidth;
                float _PulseSoftness;
                float _PulseIntensity;

                float _BaseBrightness;

                float _ReverseDirection;
                float _MovementAxis;

            CBUFFER_END


            // ------------------------------------------------------------
            // Vertex Shader
            // ------------------------------------------------------------

            Varyings Vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);

                output.positionHCS = positionInputs.positionCS;

                output.positionOS = input.positionOS.xyz;

                output.uv = input.uv;

                /*
                 * Keep the Blender vertex color path-progress
                 * available as an alternative.
                 */
                output.pathProgress = saturate(input.color.r);

                return output;
            }


            // ------------------------------------------------------------
            // Get Movement Position
            // ------------------------------------------------------------

            float GetMovementPosition(Varyings input)
            {
                /*
                 * X Axis
                 * Uses UV X.
                 */
                if (_MovementAxis < 0.5)
                {
                    return frac(input.uv.x);
                }

                /*
                 * Y Axis
                 * Uses UV Y.
                 */
                if (_MovementAxis < 1.5)
                {
                    return frac(input.uv.y);
                }

                /*
                 * Z Axis
                 *
                 * Uses local-space Z position.
                 *
                 * This is useful when the path progression
                 * is physically along the object's Z axis.
                 */
                return frac(input.positionOS.z);
            }


            // ------------------------------------------------------------
            // Fragment Shader
            // ------------------------------------------------------------

            half4 Frag(Varyings input) : SV_Target
            {
                /*
                 * Get the current position along the selected axis.
                 */
                float pathPosition =
                    GetMovementPosition(input);


                /*
                 * Calculate moving pulse position.
                 */
                float pulsePosition =
                    frac(_Time.y * _PulseSpeed);


                /*
                 * Reverse direction.
                 */
                if (_ReverseDirection > 0.5)
                {
                    pulsePosition =
                        1.0 - pulsePosition;
                }


                /*
                 * Distance between current position
                 * and pulse position.
                 */
                float distanceToPulse =
                    abs(pathPosition - pulsePosition);


                /*
                 * Handle the 0 -> 1 boundary so the pulse
                 * wraps smoothly.
                 */
                distanceToPulse =
                    min(
                        distanceToPulse,
                        1.0 - distanceToPulse
                    );


                /*
                 * Create the soft pulse.
                 */
                float pulse =
                    1.0 -
                    smoothstep(
                        _PulseWidth,
                        _PulseWidth + _PulseSoftness,
                        distanceToPulse
                    );


                /*
                 * Base path color.
                 */
                float3 baseColor =
                    _BaseColor.rgb *
                    _BaseBrightness;


                /*
                 * Moving pulse color.
                 */
                float3 animatedColor =
                    _PulseColor.rgb *
                    pulse *
                    _PulseIntensity;


                /*
                 * Final color.
                 */
                float3 finalColor =
                    baseColor +
                    animatedColor;


                return half4(finalColor, 1.0);
            }

            ENDHLSL
        }
    }

    FallBack Off
}