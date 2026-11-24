// Made with Amplify Shader Editor v1.9.9.12
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Vefects/SH_Vefects_Unlit_Flipbook_BIRP_Pro"
{
	Properties
	{
		[Space(13)][Header(Main Texture)][Space(13)] _MainTexture( "Main Texture", 2D ) = "white" {}
		_UVS( "UV S", Vector ) = ( 1, 1, 0, 0 )
		_UVP( "UV P", Vector ) = ( 0, 0, 0, 0 )
		[HDR] _R( "R", Color ) = ( 1, 0.9719134, 0.5896226, 0 )
		[HDR] _G( "G", Color ) = ( 1, 0.7230805, 0.25, 0 )
		[HDR] _B( "B", Color ) = ( 0.5943396, 0.259371, 0.09812209, 0 )
		[HDR] _Outline( "Outline", Color ) = ( 0.2169811, 0.03320287, 0.02354041, 0 )
		[Space(33)][Header(Global)][Space(13)] _Intensity( "Intensity", Float ) = 1
		[Toggle( _ISADDITIVE_ON )] _IsAdditive( "Is Additive", Float ) = 0
		_ErosionSmoothness( "Erosion Smoothness", Float ) = 0.1
		_FlatColor( "Flat Color", Range( 0, 1 ) ) = 0
		[Space(13)][Header(DisolveMapping)][Space(13)] _disolveMap( "disolveMap", 2D ) = "white" {}
		[Space(13)][Header(Distortion)][Space(13)] _DistortionTexture( "Distortion Texture", 2D ) = "white" {}
		_UVDS1( "UV D S", Vector ) = ( 1, 1, 0, 0 )
		_UVDP1( "UV D P", Vector ) = ( 0.1, -0.2, 0, 0 )
		_DistortionLerp( "Distortion Lerp", Range( 0, 1 ) ) = 0
		[Header(SecondDistortion)][Space(13)] _DistortionSecond( "DistortionSecond", 2D ) = "white" {}
		_SecondDistortionLerp( "SecondDistortionLerp", Range( 0.5, 1 ) ) = 0.5
		_UVDS( "UV D S", Vector ) = ( 1, 1, 0, 0 )
		_UVDP( "UV D P", Vector ) = ( 0.1, -0.2, 0, 0 )
		[Space(33)][Header(Pixelate)][Space(13)][Toggle( _PIXELATE_ON )] _Pixelate( "Pixelate", Float ) = 0
		_PixelsMultiplier( "Pixels Multiplier", Float ) = 1
		_PixelsX( "Pixels X", Float ) = 32
		_PixelsY( "Pixels Y", Float ) = 32
		[Space(33)][Header(Depth Fade)][Space(13)] _DepthFade( "Depth Fade", Float ) = 0
		[Space(33)][Header(Camera Depth Fade)][Space(13)] _CameraDepthFadeLength( "Camera Depth Fade Length", Float ) = 1
		_CameraDepthFadeOffset( "Camera Depth Fade Offset", Float ) = 0
		[Space(13)][Header(AR)][Space(13)] _Cull( "Cull", Float ) = 2
		_Src( "Src", Float ) = 5
		_Dst( "Dst", Float ) = 10
		_ZWrite( "ZWrite", Float ) = 0
		_ZTest( "ZTest", Float ) = 2

	}

	SubShader
	{
		

		

		Tags { "RenderType"="Transparent" "Queue"="Transparent" }

	LOD 0

		ZWrite [_ZWrite]
		Cull [_Cull]
		AlphaToMask Off
		ColorMask RGBA
		Blend One Zero, One Zero
		BlendOp Add, Add

		

		Blend [_Src] [_Dst], One Zero
		BlendOp Add, Add
		

		CGINCLUDE
			#pragma target 3.5
			// ensure rendering platforms toggle list is visible

			float4 ComputeClipSpacePosition( float2 screenPosNorm, float deviceDepth )
			{
				float4 positionCS = float4( screenPosNorm * 2.0 - 1.0, deviceDepth, 1.0 );
			#if UNITY_UV_STARTS_AT_TOP
				positionCS.y = -positionCS.y;
			#endif
				return positionCS;
			}
		ENDCG

		
		Pass
		{
			
			Name "Unlit"
			Tags { "LightMode"="ForwardBase" }

			Cull [_Cull]
			ZWrite [_ZWrite]
			ZTest [_ZTest]
			Offset 0 , 0
			ColorMask RGBA
			Blend [_Src] [_Dst], One OneMinusSrcAlpha
			BlendOp Add, Add

			

			CGPROGRAM
				#define ASE_SURFACE_TRANSPARENT
				#define ASE_VERSION 19912

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_instancing
				#include "UnityCG.cginc"

				#include "UnityShaderVariables.cginc"
				#define ASE_NEEDS_TEXTURE_COORDINATES0
				#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
				#define ASE_NEEDS_FRAG_COLOR
				#define ASE_NEEDS_FRAG_SCREEN_POSITION_NORMALIZED
				#define ASE_NEEDS_VERT_POSITION
				#pragma shader_feature_local _ISADDITIVE_ON
				#pragma shader_feature_local _PIXELATE_ON


				#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
					#define ASE_SV_DEPTH SV_DepthLessEqual
					#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
				#else
					#define ASE_SV_DEPTH SV_Depth
					#define ASE_SV_POSITION_QUALIFIERS
				#endif

				struct appdata
				{
					float4 vertex : POSITION;
					float3 normal : NORMAL;
					float4 tangent : TANGENT;
					float4 ase_color : COLOR;
					float4 ase_texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f
				{
					ASE_SV_POSITION_QUALIFIERS float4 pos : SV_POSITION;
					float4 ase_color : COLOR;
					float4 ase_texcoord : TEXCOORD0;
					float4 ase_texcoord1 : TEXCOORD1;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};

				uniform float _Src;
				uniform float _Dst;
				uniform float _ZTest;
				uniform float _ZWrite;
				uniform float _Cull;
				uniform float4 _Outline;
				uniform float4 _B;
				uniform sampler2D _MainTexture;
				uniform float2 _UVP;
				uniform float2 _UVS;
				uniform sampler2D _DistortionTexture;
				uniform float2 _UVDP;
				uniform float2 _UVDS;
				uniform float _DistortionLerp;
				uniform float _PixelsX;
				uniform float _PixelsMultiplier;
				uniform float _PixelsY;
				uniform float4 _G;
				uniform float4 _R;
				uniform float _FlatColor;
				uniform float _Intensity;
				uniform sampler2D _DistortionSecond;
				uniform float2 _UVDP1;
				uniform float2 _UVDS1;
				uniform float _SecondDistortionLerp;
				uniform float _ErosionSmoothness;
				uniform sampler2D _disolveMap;
				uniform float4 _disolveMap_ST;
				UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
				uniform float4 _CameraDepthTexture_TexelSize;
				uniform float _DepthFade;
				uniform float _CameraDepthFadeLength;
				uniform float _CameraDepthFadeOffset;


				
				v2f vert( appdata v  )
				{
					UNITY_SETUP_INSTANCE_ID(v);
					v2f o;
					UNITY_INITIALIZE_OUTPUT(v2f,o);
					UNITY_TRANSFER_INSTANCE_ID(v,o);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

					float3 objectToViewPos = UnityObjectToViewPos( v.vertex.xyz );
					float eyeDepth = -objectToViewPos.z;
					o.ase_texcoord1.x = eyeDepth;
					
					o.ase_color = v.ase_color;
					o.ase_texcoord = v.ase_texcoord;
					
					//setting value to unused interpolator channels and avoid initialization warnings
					o.ase_texcoord1.yzw = 0;

					#ifdef ASE_ABSOLUTE_VERTEX_POS
						float3 defaultVertexValue = v.vertex.xyz;
					#else
						float3 defaultVertexValue = float3(0, 0, 0);
					#endif
					float3 vertexValue = defaultVertexValue;
					#ifdef ASE_ABSOLUTE_VERTEX_POS
						v.vertex.xyz = vertexValue;
					#else
						v.vertex.xyz += vertexValue;
					#endif
					v.vertex.w = 1;
					v.normal = v.normal;
					v.tangent = v.tangent;

					o.pos = UnityObjectToClipPos( v.vertex );

					#if defined( ASE_SHADOWS )
						UNITY_TRANSFER_SHADOW( o, v.texcoord );
					#endif
					return o;
				}

				half4 frag( v2f IN 
							#if defined( ASE_WRITE_DEPTH )
								, out float outputDepth : SV_Depth
							#endif
				) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID( IN );
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

					float4 ScreenPosNorm = float4( IN.pos.xy * ( _ScreenParams.zw - 1.0 ), IN.pos.zw );
					float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, IN.pos.z ) * IN.pos.w;
					float4 ScreenPos = ComputeScreenPos( ClipPos );

					float2 texCoord19 = IN.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float2 panner27 = ( 1.0 * _Time.y * _UVP + ( texCoord19 * _UVS ));
					float2 texCoord11 = IN.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float2 panner15 = ( 1.0 * _Time.y * _UVDP + ( texCoord11 * _UVDS ));
					float2 lerpResult26 = lerp( float2( 0,0 ) , ( ( (tex2D( _DistortionTexture, panner15 )).rg + -0.5 ) * 2.0 ) , _DistortionLerp);
					float2 DistortionRegister34 = ( panner27 + lerpResult26 );
					half2 pixelateduv115 = floor( DistortionRegister34 * float2( ( _PixelsX * _PixelsMultiplier ), ( _PixelsY * _PixelsMultiplier ) ) + float2( 0,0 ) ) / float2( ( _PixelsX * _PixelsMultiplier ), ( _PixelsY * _PixelsMultiplier ) );
					#ifdef _PIXELATE_ON
					float2 staticSwitch118 = pixelateduv115;
					#else
					float2 staticSwitch118 = DistortionRegister34;
					#endif
					float2 MT_UVs146 = staticSwitch118;
					float4 tex2DNode45 = tex2D( _MainTexture, MT_UVs146 );
					float4 lerpResult97 = lerp( _Outline , _B , tex2DNode45.b);
					float4 lerpResult112 = lerp( lerpResult97 , _G , tex2DNode45.g);
					float4 lerpResult111 = lerp( lerpResult112 , _R , tex2DNode45.r);
					float4 lerpResult88 = lerp( ( IN.ase_color * lerpResult111 ) , IN.ase_color , _FlatColor);
					float2 texCoord95 = IN.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float2 panner71 = ( 1.0 * _Time.y * _UVDP1 + ( texCoord95 * _UVDS1 ));
					float4 SecondDistortion108 = ( tex2D( _DistortionSecond, panner71 ) + _SecondDistortionLerp );
					float4 temp_output_90_0 = ( ( lerpResult88 * _Intensity ) * SecondDistortion108 );
					float MT_Alpha48 = tex2DNode45.a;
					float smoothstepResult55 = smoothstep( IN.ase_texcoord.z , ( IN.ase_texcoord.z + _ErosionSmoothness ) , MT_Alpha48);
					float VC_Alpha52 = IN.ase_color.a;
					float Opacity_VTC_W33 = IN.ase_texcoord.z;
					float Opacity_VTC_T30 = IN.ase_texcoord.w;
					float temp_output_44_0 =  (( Opacity_VTC_T30 - 1.0 ) + ( Opacity_VTC_W33 - 0.0 ) * ( 1.0 - ( Opacity_VTC_T30 - 1.0 ) ) / ( 1.0 - 0.0 ) );
					float2 uv_disolveMap = IN.ase_texcoord.xy * _disolveMap_ST.xy + _disolveMap_ST.zw;
					float smoothstepResult54 = smoothstep( temp_output_44_0 , ( temp_output_44_0 + Opacity_VTC_T30 ) , tex2D( _disolveMap, uv_disolveMap ).r);
					float disolveMapping56 = saturate( smoothstepResult54 );
					float screenDepth133 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ScreenPosNorm.xy ));
					float distanceDepth133 = saturate( ( screenDepth133 - LinearEyeDepth( ScreenPosNorm.z ) ) / ( _DepthFade ) );
					float DF135 = saturate( distanceDepth133 );
					float eyeDepth = IN.ase_texcoord1.x;
					float cameraDepthFade139 = (( eyeDepth -_ProjectionParams.y - _CameraDepthFadeOffset ) / _CameraDepthFadeLength);
					float Cam_DF141 = saturate( cameraDepthFade139 );
					float OP61 = saturate( ( saturate( ( saturate( ( saturate( ( saturate( smoothstepResult55 ) * VC_Alpha52 ) ) * disolveMapping56 ) ) * DF135 ) ) * Cam_DF141 ) );
					#ifdef _ISADDITIVE_ON
					float4 staticSwitch153 = ( temp_output_90_0 * OP61 );
					#else
					float4 staticSwitch153 = temp_output_90_0;
					#endif
					float4 COL149 = staticSwitch153;
					

					float3 Color = COL149.rgb;
					float Alpha = OP61;
					half AlphaClipThreshold = 0.5;
					half AlphaClipThresholdShadow = 0.5;

					#if defined( ASE_WRITE_DEPTH )
						outputDepth = IN.pos.z;
					#endif

					#ifdef _ALPHATEST_ON
						clip( Alpha - AlphaClipThreshold );
					#endif

				#if defined( ASE_SURFACE_TRANSPARENT ) || defined( ASE_OPAQUE_KEEP_ALPHA )
					return half4( Color, Alpha );
				#else
					return half4( Color, 1.0 );
				#endif
				}
			ENDCG
		}

	
	}
	CustomEditor "AmplifyShaderEditor.MaterialInspector"
	
	Fallback Off
}
/*ASEBEGIN
Version=19912
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":10,"pos":[-5376,-1728],"params":["Inherit","False","1992","995","Distortion","18","34","31","27","26","25","24","23","22","20","19","18","17","16","15","14","13","12","11","Distortion","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor","id":11,"pos":[-5248,-1152],"params":["Inherit","False","0","-1","2","3","2","SAMPLER2D","","False","0","FLOAT2","1,1","False","1","FLOAT2","0,0","False","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":12,"pos":[-4992,-1024],"params":["Inherit","False","Property","_UVDS","UV D S","18","0","Create","True","0","0","0","False","0","False","Object","-1","","1,1","1,1","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":13,"pos":[-4992,-1152],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":14,"pos":[-4736,-1024],"params":["Inherit","False","Property","_UVDP","UV D P","19","0","Create","True","0","0","0","False","0","False","Object","-1","","0.1,-0.2","0.1,-0.2","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.PannerNode, AmplifyShaderEditor","id":15,"pos":[-4736,-1152],"params":["Inherit","False","3","0","FLOAT2","0,0","False","2","FLOAT2","0,0","False","1","FLOAT","1","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":21,"pos":[-5408,-320],"params":["Inherit","False","1951.145","963.0234","Opacity","21","61","125","59","60","127","126","57","58","53","55","46","51","33","30","28","128","129","130","131","136","142","Opacity","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":16,"pos":[-4480,-1152],"params":["Inherit","True","Property","_DistortionTexture","Distortion Texture","12","0","Create","True","0","0","0","False","3","Space(13)","Header(Distortion)","Space(13)","False","","-1","98c3d568d9032a34eb5b038e20fea05d","98c3d568d9032a34eb5b038e20fea05d","True","0","False","white","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":29,"pos":[-5392,768],"params":["Inherit","False","1934.222","768.4693","Dissolve Map","14","56","54","50","49","44","43","42","41","40","39","37","35","32","145","Dissolve Map","0.1037736,0.1037736,0.1037736,1","0","0"]}
{"type":"AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor","id":17,"pos":[-4096,-1024],"params":["Inherit","False","True","True","False","False","1","0","COLOR","0,0,0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor","id":19,"pos":[-5248,-1664],"params":["Inherit","False","0","-1","2","3","2","SAMPLER2D","","False","0","FLOAT2","1,1","False","1","FLOAT2","0,0","False","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":18,"pos":[-4992,-1536],"params":["Inherit","False","Property","_UVS","UV S","1","0","Create","True","0","0","0","False","0","False","Object","-1","","1,1","1,1","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":30,"pos":[-5376,80],"params":["Inherit","False","Opacity_VTC_T","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":36,"pos":[-3312,-1728],"params":["Inherit","False","2801.733","1345.571","Color","23","45","147","48","106","107","149","90","84","99","88","105","47","111","112","101","97","96","93","92","52","153","154","155","Color","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":24,"pos":[-3840,-1024],"params":["Inherit","False","ConstantBiasScale","-1","","1","63208df05c83e8e49a48ffbdce2e43a0","0","3","3","FLOAT2","0,0","False","1","FLOAT","-0.5","False","2","FLOAT","2","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":23,"pos":[-4096,-1280],"params":["Inherit","False","Constant","_Vector0","Vector 0","8","0","Create","True","0","0","0","False","0","False","Object","-1","","0,0","0,0","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":25,"pos":[-4992,-1664],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":20,"pos":[-4736,-1536],"params":["Inherit","False","Property","_UVP","UV P","2","0","Create","True","0","0","0","False","0","False","Object","-1","","0,0","0,0","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":22,"pos":[-4096,-1152],"params":["Inherit","False","Property","_DistortionLerp","Distortion Lerp","15","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":32,"pos":[-5248,1408],"params":["Inherit","False","Constant","_Float2","Float 2","20","0","Create","True","0","0","0","False","0","False","Object","-1","","1","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":35,"pos":[-5248,1280],"params":["Inherit","False","30","Opacity_VTC_T","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":33,"pos":[-5376,0],"params":["Inherit","False","Opacity_VTC_W","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":148,"pos":[-3248,328],"params":["Inherit","False","1316","547","Pixelate","9","117","116","121","119","120","118","38","115","146","Pixelate","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":26,"pos":[-3712,-1280],"params":["Inherit","False","3","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.PannerNode, AmplifyShaderEditor","id":27,"pos":[-4736,-1664],"params":["Inherit","False","3","0","FLOAT2","0,0","False","2","FLOAT2","0,0","False","1","FLOAT","1","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":42,"pos":[-4992,1408],"params":["Inherit","False","Constant","_Float3","Float 3","20","0","Create","True","0","0","0","False","0","False","Object","-1","","1","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor","id":39,"pos":[-4992,1280],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":37,"pos":[-4992,1024],"params":["Inherit","False","Constant","_Float0","Float 0","20","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":40,"pos":[-4992,1152],"params":["Inherit","False","Constant","_Float1","Float 1","20","0","Create","True","0","0","0","False","0","False","Object","-1","","1","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":41,"pos":[-5248,896],"params":["Inherit","False","33","Opacity_VTC_W","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":43,"pos":[-4480,1280],"params":["Inherit","False","30","Opacity_VTC_T","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":48,"pos":[-2560,-512],"params":["Inherit","False","MT Alpha","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":46,"pos":[-5376,256],"params":["Inherit","False","Property","_ErosionSmoothness","Erosion Smoothness","9","0","Create","True","0","0","0","False","0","False","Object","-1","","0.1","1.57","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":117,"pos":[-3200,640],"params":["Inherit","False","Property","_PixelsY","Pixels Y","23","0","Create","True","0","0","0","False","0","False","Object","-1","","32","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":116,"pos":[-3200,512],"params":["Inherit","False","Property","_PixelsX","Pixels X","22","0","Create","True","0","0","0","False","0","False","Object","-1","","32","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":121,"pos":[-3200,768],"params":["Inherit","False","Property","_PixelsMultiplier","Pixels Multiplier","21","0","Create","True","0","0","0","False","0","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":31,"pos":[-3872,-1664],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor","id":28,"pos":[-5376,-256],"params":["Inherit","False","0","4","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":51,"pos":[-5120,0],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":53,"pos":[-4864,-256],"params":["Inherit","False","48","MT Alpha","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TFHCRemapNode, AmplifyShaderEditor","id":44,"pos":[-4736,896],"params":["Inherit","False","5","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","1","False","3","FLOAT","0","False","4","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":49,"pos":[-4480,1152],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":50,"pos":[-4224,1152],"params":["Inherit","True","Property","_disolveMap","disolveMap","11","0","Create","True","0","0","0","False","3","Space(13)","Header(DisolveMapping)","Space(13)","False","","-1","None","None","True","0","False","white","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":143,"pos":[-5424,1736],"params":["Inherit","False","1188","187","Depth Fade","4","132","133","135","134","Depth Fade","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":119,"pos":[-2944,512],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":120,"pos":[-2944,640],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":34,"pos":[-3712,-1664],"params":["Inherit","False","DistortionRegister","-1","True","1","0","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor","id":55,"pos":[-4864,-128],"params":["Inherit","False","3","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor","id":54,"pos":[-4224,896],"params":["Inherit","False","3","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":52,"pos":[-2176,-1024],"params":["Inherit","False","VC Alpha","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":38,"pos":[-3200,384],"params":["Inherit","False","34","DistortionRegister","1","0","OBJECT","","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.TFHCPixelate, AmplifyShaderEditor","id":115,"pos":[-2688,512],"params":["Inherit","False","4","0","FLOAT2","0,0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":57,"pos":[-4864,256],"params":["Inherit","False","52","VC Alpha","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":126,"pos":[-4608,-128],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":132,"pos":[-5376,1792],"params":["Inherit","False","Property","_DepthFade","Depth Fade","24","0","Create","True","0","0","0","False","3","Space(33)","Header(Depth Fade)","Space(13)","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":145,"pos":[-3968,896],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":144,"pos":[-5424,2120],"params":["Inherit","False","1188","291","Cam Depth Fade","5","139","138","137","140","141","Cam Depth Fade","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":118,"pos":[-2432,384],"params":["Inherit","False","Property","_Pixelate","Pixelate","20","0","Create","True","0","0","0","False","3","Space(33)","Header(Pixelate)","Space(13)","False","","0","0","0","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","FLOAT2","0,0","False","0","FLOAT2","0,0","False","2","FLOAT2","0,0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT2","0,0","False","6","FLOAT2","0,0","False","7","FLOAT2","0,0","False","8","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":58,"pos":[-4864,128],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.DepthFade, AmplifyShaderEditor","id":133,"pos":[-4992,1792],"params":["Inherit","False","True","True","False","2","1","FLOAT3","0,0,0","False","0","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":56,"pos":[-3712,896],"params":["Inherit","False","disolveMapping","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":146,"pos":[-2176,384],"params":["Inherit","False","MT UVs","-1","True","1","0","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":127,"pos":[-4608,128],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":59,"pos":[-4352,256],"params":["Inherit","False","56","disolveMapping","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":134,"pos":[-4736,1792],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":140,"pos":[-5376,2304],"params":["Inherit","False","Property","_CameraDepthFadeOffset","Camera Depth Fade Offset","26","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":137,"pos":[-5376,2176],"params":["Inherit","False","Property","_CameraDepthFadeLength","Camera Depth Fade Length","25","0","Create","True","0","0","0","False","3","Space(33)","Header(Camera Depth Fade)","Space(13)","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":65,"pos":[-5360,-704],"params":["Inherit","False","1665.348","371.0714","Distortion Secondary","9","108","104","103","102","98","95","94","85","71","Distortion Secondary","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":147,"pos":[-3200,-640],"params":["Inherit","False","146","MT UVs","1","0","OBJECT","","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":60,"pos":[-4352,128],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":135,"pos":[-4480,1792],"params":["Inherit","False","DF","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CameraDepthFade, AmplifyShaderEditor","id":139,"pos":[-4992,2176],"params":["Inherit","False","3","2","FLOAT3","0,0,0","False","0","FLOAT","1","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":92,"pos":[-3200,-1152],"params":["Inherit","False","Property","_B","B","5","1","[HDR]","Create","True","0","0","0","False","0","False","Object","-1","","0.5943396,0.259371,0.09812209,0","0.2641509,0.2616589,0.2554289,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":93,"pos":[-3200,-896],"params":["Inherit","False","Property","_Outline","Outline","6","1","[HDR]","Create","True","0","0","0","False","0","False","Object","-1","","0.2169811,0.03320287,0.02354041,0","0,0,0,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":94,"pos":[-5056,-496],"params":["Inherit","False","Property","_UVDS1","UV D S","13","0","Create","True","0","0","0","False","0","False","Object","-1","","1,1","1,1","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor","id":95,"pos":[-5312,-624],"params":["Inherit","False","0","-1","2","3","2","SAMPLER2D","","False","0","FLOAT2","1,1","False","1","FLOAT2","0,0","False","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":45,"pos":[-2944,-640],"params":["Inherit","True","Property","_MainTexture","Main Texture","0","0","Create","True","0","0","0","False","3","Space(13)","Header(Main Texture)","Space(13)","False","","-1","5e9cda599296bd74a9a45a7b3a63c0a9","5e9cda599296bd74a9a45a7b3a63c0a9","True","0","False","white","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":125,"pos":[-4096,128],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":136,"pos":[-4864,512],"params":["Inherit","False","135","DF","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":138,"pos":[-4736,2176],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":85,"pos":[-4800,-496],"params":["Inherit","False","Property","_UVDP1","UV D P","14","0","Create","True","0","0","0","False","0","False","Object","-1","","0.1,-0.2","0.1,-0.2","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":96,"pos":[-3200,-1408],"params":["Inherit","False","Property","_G","G","4","1","[HDR]","Create","True","0","0","0","False","0","False","Object","-1","","1,0.7230805,0.25,0","1,0.3523919,0,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":98,"pos":[-5056,-624],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":97,"pos":[-2816,-1152],"params":["Inherit","True","3","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":128,"pos":[-4864,384],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":141,"pos":[-4480,2176],"params":["Inherit","False","Cam DF","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.PannerNode, AmplifyShaderEditor","id":71,"pos":[-4800,-624],"params":["Inherit","False","3","0","FLOAT2","0,0","False","2","FLOAT2","0,0","False","1","FLOAT","1","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":101,"pos":[-3200,-1664],"params":["Inherit","False","Property","_R","R","3","1","[HDR]","Create","True","0","0","0","False","0","False","Object","-1","","1,0.9719134,0.5896226,0","0.3679245,0.3679245,0.3679245,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":112,"pos":[-2560,-1408],"params":["Inherit","True","3","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":129,"pos":[-4608,384],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":142,"pos":[-4352,512],"params":["Inherit","False","141","Cam DF","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":103,"pos":[-4432,-448],"params":["Inherit","False","Property","_SecondDistortionLerp","SecondDistortionLerp","17","0","Create","True","0","0","0","False","0","False","Object","-1","","0.5","0","0.5","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":111,"pos":[-2304,-1664],"params":["Inherit","True","3","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.VertexColorNode, AmplifyShaderEditor","id":47,"pos":[-2176,-1280],"params":["Inherit","False","0","5","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":130,"pos":[-4352,384],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":102,"pos":[-4448,-656],"params":["Inherit","True","Property","_DistortionSecond","DistortionSecond","16","1","[Header]","Create","True","1","SecondDistortion","0","0","False","1","Space(13)","False","","-1","None","None","True","0","False","white","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":104,"pos":[-4128,-512],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":105,"pos":[-1920,-1664],"params":["Inherit","True","2","2","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":106,"pos":[-1664,-1408],"params":["Inherit","False","Property","_FlatColor","Flat Color","10","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":131,"pos":[-4096,384],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":108,"pos":[-3920,-512],"params":["Inherit","False","SecondDistortion","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":88,"pos":[-1664,-1664],"params":["Inherit","True","3","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":107,"pos":[-1280,-1536],"params":["Inherit","False","Property","_Intensity","Intensity","7","0","Create","True","1","TextureProps","0","0","False","3","Space(33)","Header(Global)","Space(13)","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":61,"pos":[-3712,384],"params":["Inherit","False","OP","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":99,"pos":[-1280,-1664],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":84,"pos":[-1024,-1536],"params":["Inherit","False","108","SecondDistortion","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":155,"pos":[-1408,-896],"params":["Inherit","False","61","OP","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":90,"pos":[-1024,-1664],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":154,"pos":[-1408,-1024],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":153,"pos":[-1152,-1152],"params":["Inherit","False","Property","_IsAdditive","Is Additive","8","0","Create","True","0","0","0","False","0","False","","0","0","0","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","COLOR","0,0,0,0","False","0","COLOR","0,0,0,0","False","2","COLOR","0,0,0,0","False","3","COLOR","0,0,0,0","False","4","COLOR","0,0,0,0","False","5","COLOR","0,0,0,0","False","6","COLOR","0,0,0,0","False","7","COLOR","0,0,0,0","False","8","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":63,"pos":[336,-48],"params":["Inherit","False","1243","166","AR","5","110","80","78","82","83","","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":149,"pos":[-768,-1152],"params":["Inherit","False","COL","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":78,"pos":[640,0],"params":["Inherit","False","Property","_Src","Src","28","0","Create","True","0","0","0","True","0","False","Object","-1","","5","5","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":110,"pos":[896,0],"params":["Inherit","False","Property","_Dst","Dst","29","0","Create","True","0","0","0","True","0","False","Object","-1","","10","10","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":82,"pos":[1408,0],"params":["Inherit","False","Property","_ZTest","ZTest","31","0","Create","True","0","0","0","True","0","False","Object","-1","","2","2","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":83,"pos":[1152,0],"params":["Inherit","False","Property","_ZWrite","ZWrite","30","0","Create","True","0","0","0","True","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":80,"pos":[384,0],"params":["Inherit","False","Property","_Cull","Cull","27","0","Create","True","0","0","0","True","3","Space(13)","Header(AR)","Space(13)","False","Object","-1","","2","2","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":73,"pos":[-384,128],"params":["Inherit","False","61","OP","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":150,"pos":[-384,0],"params":["Inherit","False","149","COL","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":157,"pos":[0,0],"params":["Float","False","False","-1","3","AmplifyShaderEditor.MaterialInspector","0","1","New Amplify Shader","0770190933193b94aaa3065e307002fa","True","ExtraPrePass","0","0","ExtraPrePass","6","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","1","RenderType=Opaque=RenderType","True","3","True","14","all","0","False","True","1","1","False","","0","False","","0","1","False","","0","False","","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","1","LightMode=ForwardBase","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":158,"pos":[0,0],"params":["Float","False","True","-1","3","AmplifyShaderEditor.MaterialInspector","0","7","Vefects/SH_Vefects_Unlit_Flipbook_BIRP_Pro","0770190933193b94aaa3065e307002fa","True","Unlit","0","1","Unlit","8","True","True","1","1","True","_Src","0","True","_Dst","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","True","True","0","True","_Cull","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","True","True","1","True","_ZWrite","False","False","False","True","2","RenderType=Transparent=RenderType","Queue=Transparent=Queue=0","True","3","True","14","all","0","True","True","1","5","True","_Src","10","True","_Dst","1","1","False","","10","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","False","True","True","0","True","_Cull","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","True","True","2","True","_ZWrite","True","3","True","_ZTest","True","True","0","False","","0","False","","False","True","1","LightMode=ForwardBase","False","False","0","","0","0","Standard","10","Surface","1","639264335900987133","  Keep Alpha","0","0","  Blend","0","0","Alpha Clipping","0","0","  Use Shadow Threshold","0","0","Cast Shadows","0","639264336297014064","Write Depth","0","0","  Conservative","0","0","Extra Pre Pass","0","0","Vertex Position","1","0","0","3","False","True","False","False","","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":159,"pos":[0,0],"params":["Float","False","False","-1","3","AmplifyShaderEditor.MaterialInspector","0","1","New Amplify Shader","0770190933193b94aaa3065e307002fa","True","ShadowCaster","0","2","ShadowCaster","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","1","RenderType=Opaque=RenderType","True","3","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","False","","True","3","False","","False","False","True","1","LightMode=ShadowCaster","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":113,"pos":[384,-256],"params":["Inherit","False","304","100","Ge Lush was here! <3","0","Ge Lush was here! <3","0,0,0,1","0","0"]}
{"wire":[13,0,11,0]}
{"wire":[13,1,12,0]}
{"wire":[15,0,13,0]}
{"wire":[15,2,14,0]}
{"wire":[16,1,15,0]}
{"wire":[17,0,16,0]}
{"wire":[30,0,28,4]}
{"wire":[24,3,17,0]}
{"wire":[25,0,19,0]}
{"wire":[25,1,18,0]}
{"wire":[33,0,28,3]}
{"wire":[26,0,23,0]}
{"wire":[26,1,24,0]}
{"wire":[26,2,22,0]}
{"wire":[27,0,25,0]}
{"wire":[27,2,20,0]}
{"wire":[39,0,35,0]}
{"wire":[39,1,32,0]}
{"wire":[48,0,45,4]}
{"wire":[31,0,27,0]}
{"wire":[31,1,26,0]}
{"wire":[51,0,28,3]}
{"wire":[51,1,46,0]}
{"wire":[44,0,41,0]}
{"wire":[44,1,37,0]}
{"wire":[44,2,40,0]}
{"wire":[44,3,39,0]}
{"wire":[44,4,42,0]}
{"wire":[49,0,44,0]}
{"wire":[49,1,43,0]}
{"wire":[119,0,116,0]}
{"wire":[119,1,121,0]}
{"wire":[120,0,117,0]}
{"wire":[120,1,121,0]}
{"wire":[34,0,31,0]}
{"wire":[55,0,53,0]}
{"wire":[55,1,28,3]}
{"wire":[55,2,51,0]}
{"wire":[54,0,50,1]}
{"wire":[54,1,44,0]}
{"wire":[54,2,49,0]}
{"wire":[52,0,47,4]}
{"wire":[115,0,38,0]}
{"wire":[115,1,119,0]}
{"wire":[115,2,120,0]}
{"wire":[126,0,55,0]}
{"wire":[145,0,54,0]}
{"wire":[118,1,38,0]}
{"wire":[118,0,115,0]}
{"wire":[58,0,126,0]}
{"wire":[58,1,57,0]}
{"wire":[133,0,132,0]}
{"wire":[56,0,145,0]}
{"wire":[146,0,118,0]}
{"wire":[127,0,58,0]}
{"wire":[134,0,133,0]}
{"wire":[60,0,127,0]}
{"wire":[60,1,59,0]}
{"wire":[135,0,134,0]}
{"wire":[139,0,137,0]}
{"wire":[139,1,140,0]}
{"wire":[45,1,147,0]}
{"wire":[125,0,60,0]}
{"wire":[138,0,139,0]}
{"wire":[98,0,95,0]}
{"wire":[98,1,94,0]}
{"wire":[97,0,93,0]}
{"wire":[97,1,92,0]}
{"wire":[97,2,45,3]}
{"wire":[128,0,125,0]}
{"wire":[128,1,136,0]}
{"wire":[141,0,138,0]}
{"wire":[71,0,98,0]}
{"wire":[71,2,85,0]}
{"wire":[112,0,97,0]}
{"wire":[112,1,96,0]}
{"wire":[112,2,45,2]}
{"wire":[129,0,128,0]}
{"wire":[111,0,112,0]}
{"wire":[111,1,101,0]}
{"wire":[111,2,45,1]}
{"wire":[130,0,129,0]}
{"wire":[130,1,142,0]}
{"wire":[102,1,71,0]}
{"wire":[104,0,102,0]}
{"wire":[104,1,103,0]}
{"wire":[105,0,47,0]}
{"wire":[105,1,111,0]}
{"wire":[131,0,130,0]}
{"wire":[108,0,104,0]}
{"wire":[88,0,105,0]}
{"wire":[88,1,47,0]}
{"wire":[88,2,106,0]}
{"wire":[61,0,131,0]}
{"wire":[99,0,88,0]}
{"wire":[99,1,107,0]}
{"wire":[90,0,99,0]}
{"wire":[90,1,84,0]}
{"wire":[154,0,90,0]}
{"wire":[154,1,155,0]}
{"wire":[153,1,90,0]}
{"wire":[153,0,154,0]}
{"wire":[149,0,153,0]}
{"wire":[158,0,150,0]}
{"wire":[158,7,73,0]}
ASEEND*/
//CHKSM=25292213464210BA6DA0B42EEDD34DBCCCA083E2