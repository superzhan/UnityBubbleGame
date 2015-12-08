/****************************************
	ParticleScaler.js v1.01
	Copyright 2013 Unluck Software	
 	www.chemicalbliss.com		
 	
 		• Scale Particles 
                A simple function that helps with scaling all selected particle systems 
        • Create Prefabs 
                Creates prefabs from selected gameObjects 
        • Update Particles 
                Updates all selected particle systems, useful for previewing multiple particle systems 	
 	
	Changelog
		v1.01
		Scales transform as well as particles
		Added function to create prefabs from selected gameobjects																										
*****************************************/
class ParticleScaler extends EditorWindow {

    var _scaleMultiplier: float = 1.0f;
	var _maxParticleScale: float = .5f;
	var _maxParticles: int = 50;
    @MenuItem("Unluck/Particle Scaler")

    static
    function ShowWindow() {
        EditorWindow.GetWindow(ParticleScaler);
    }

    function OnGUI() {
    	EditorGUILayout.LabelField("Particle Scaler v1.01");
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scale particles and selected gameObjects");
        if (GUILayout.Button("Scale Particles")) {
            for (var gameObj: GameObject in Selection.gameObjects) {
            	//gameObj.transform.localScale*=_scaleMultiplier;
            	var pss: ParticleSystem[];
           	 	pss = gameObj.GetComponentsInChildren.<ParticleSystem> ();
            	for (var ps: ParticleSystem in pss) {
            		ps.Stop();
					scalePs(gameObj, ps);
					ps.Play();
				}
			}
			
		}
        _scaleMultiplier = EditorGUILayout.Slider(_scaleMultiplier, 0.5, 2);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Create prefabs from selected gameObjects");
        if (GUILayout.Button("Create Prefabs")) {
        	DoCreateSimplePrefab();
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Update Particle Systems in Selection");
        if (GUILayout.Button("Update Particles")) {
        	updateParticles();
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Change Max Particle Scale in Selection");
        if (GUILayout.Button("Max Particle Scale")) {
        	maxScale();
        }
         _maxParticleScale = EditorGUILayout.Slider(_maxParticleScale, 0.01, 0.75);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Change Max Particles in Selection");
        if (GUILayout.Button("Max Particles")) {
        	maxParticles();
        }
         _maxParticles = EditorGUILayout.Slider(_maxParticles, 10, 1000);
    }
	
	function DoCreateSimplePrefab(){
		for (var gameObj: GameObject in Selection.gameObjects) {
			var prefab:Object  = PrefabUtility.CreateEmptyPrefab("Assets/"+gameObj.gameObject.name+".prefab");
			PrefabUtility.ReplacePrefab(gameObj.gameObject, prefab, ReplacePrefabOptions.ConnectToPrefab);
		}
	}
	
	function updateParticles(){
		 for (var gameObj: GameObject in Selection.gameObjects) {
            	var pss: ParticleSystem[];
           	 	pss = gameObj.GetComponentsInChildren.<ParticleSystem> ();
            	for (var ps: ParticleSystem in pss) {
            		ps.Stop();
					ps.Play();
				}
			}
	}
	
	function maxParticles(){
		for (var gameObj: GameObject in Selection.gameObjects) {
            	
            	var pss: ParticleSystem[];
           	 	pss = gameObj.GetComponentsInChildren.<ParticleSystem> ();
            	for (var ps: ParticleSystem in pss) {
            		 ps.Stop();
            		 
            		 //ps. = _maxParticles;
            		 var sObj: SerializedObject = new SerializedObject(ps);
            		
            		 sObj.FindProperty("InitialModule.maxNumParticles").intValue = _maxParticles;
            		 sObj.ApplyModifiedProperties();
            		 ps.Play();
				}
			}
	}
	
	function maxScale(){
		for (var gameObj: GameObject in Selection.gameObjects) {
            	
            	var pss: ParticleSystemRenderer[];
           	 	pss = gameObj.GetComponentsInChildren.<ParticleSystemRenderer> ();
            	for (var ps: ParticleSystemRenderer in pss) {
            		ps.GetComponent.<ParticleSystem>().Stop();
					ps.maxParticleSize = _maxParticleScale;
					ps.GetComponent.<ParticleSystem>().Play();
				}
			}
	}
	
    function scalePs(__parent: GameObject, __particles: ParticleSystem) {
        if (__parent != __particles.gameObject) {
            __particles.transform.localPosition *= _scaleMultiplier;
        }
        __particles.startSize *= _scaleMultiplier;
        __particles.gravityModifier *= _scaleMultiplier;
        __particles.startSpeed *= _scaleMultiplier;
        
        var sObj: SerializedObject = new SerializedObject(__particles);
		sObj.FindProperty("VelocityModule.x.scalar").floatValue *= _scaleMultiplier;
		sObj.FindProperty("VelocityModule.y.scalar").floatValue *= _scaleMultiplier;
		sObj.FindProperty("VelocityModule.z.scalar").floatValue *= _scaleMultiplier;
		scaleCurve(sObj.FindProperty("VelocityModule.x.minCurve").animationCurveValue);
		scaleCurve(sObj.FindProperty("VelocityModule.x.maxCurve").animationCurveValue);
		scaleCurve(sObj.FindProperty("VelocityModule.y.minCurve").animationCurveValue);
		scaleCurve(sObj.FindProperty("VelocityModule.y.maxCurve").animationCurveValue);
		scaleCurve(sObj.FindProperty("VelocityModule.z.minCurve").animationCurveValue);
		scaleCurve(sObj.FindProperty("VelocityModule.z.maxCurve").animationCurveValue);
		sObj.FindProperty("ClampVelocityModule.x.scalar").floatValue *= _scaleMultiplier;
		sObj.FindProperty("ClampVelocityModule.y.scalar").floatValue *= _scaleMultiplier;
		sObj.FindProperty("ClampVelocityModule.z.scalar").floatValue *= _scaleMultiplier;
		sObj.FindProperty("ClampVelocityModule.magnitude.scalar").floatValue *= _scaleMultiplier;
		scaleCurve(sObj.FindProperty("ClampVelocityModule.x.minCurve").animationCurveValue);
		scaleCurve(sObj.FindProperty("ClampVelocityModule.x.maxCurve").animationCurveValue);
		scaleCurve(sObj.FindProperty("ClampVelocityModule.y.minCurve").animationCurveValue);
		scaleCurve(sObj.FindProperty("ClampVelocityModule.y.maxCurve").animationCurveValue);
		scaleCurve(sObj.FindProperty("ClampVelocityModule.z.minCurve").animationCurveValue);
		scaleCurve(sObj.FindProperty("ClampVelocityModule.z.maxCurve").animationCurveValue);
		scaleCurve(sObj.FindProperty("ClampVelocityModule.magnitude.minCurve").animationCurveValue);
		scaleCurve(sObj.FindProperty("ClampVelocityModule.magnitude.maxCurve").animationCurveValue);
		sObj.FindProperty("ForceModule.x.scalar").floatValue *= _scaleMultiplier;
		sObj.FindProperty("ForceModule.y.scalar").floatValue *= _scaleMultiplier;
		sObj.FindProperty("ForceModule.z.scalar").floatValue *= _scaleMultiplier;
		scaleCurve(sObj.FindProperty("ForceModule.x.minCurve").animationCurveValue);
		scaleCurve(sObj.FindProperty("ForceModule.x.maxCurve").animationCurveValue);
		scaleCurve(sObj.FindProperty("ForceModule.y.minCurve").animationCurveValue);
		scaleCurve(sObj.FindProperty("ForceModule.y.maxCurve").animationCurveValue);
		scaleCurve(sObj.FindProperty("ForceModule.z.minCurve").animationCurveValue);
		scaleCurve(sObj.FindProperty("ForceModule.z.maxCurve").animationCurveValue);
		sObj.ApplyModifiedProperties();
    }

    function scaleCurve(curve: AnimationCurve) {
        for (var i: int = 0; i < curve.keys.Length; i++) {
            curve.keys[i].value *= _scaleMultiplier;
        }
    }
}