using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SensorToolkit;
using UnityEditor;
using WizardsCode.AI;

namespace WizardsCode.AIEditor
{
    [CustomEditor(typeof(CameraSteeringRig))]
    public class CameraSteeringRigEditor : Editor
    {
        SerializedProperty ignoreList;
        SerializedProperty avoidanceSensitivity;
        SerializedProperty maxAvoidanceLength;
        SerializedProperty yAxis;
        SerializedProperty rotateTowardsTarget;
        SerializedProperty rb;
        SerializedProperty turnForce;
        SerializedProperty moveForce;
        SerializedProperty strafeForce;
        SerializedProperty turnSpeed;
        SerializedProperty moveSpeed;
        SerializedProperty strafeSpeed;
        SerializedProperty stoppingDistance;
        SerializedProperty destinationTransform;
        SerializedProperty faceTowardsTransform;
        SerializedProperty maintainHeight;
        SerializedProperty minHeight;
        SerializedProperty maxHeight;
        SerializedProperty optimalHeight;
        SerializedProperty randomizeSpeedOnStart;
        SerializedProperty animationController;
        SerializedProperty legacyAnimationController;

        CameraSteeringRig steeringRig;

        void OnEnable()
        {
            if (serializedObject == null) return;

            steeringRig = serializedObject.targetObject as CameraSteeringRig;
            ignoreList = serializedObject.FindProperty("IgnoreList");
            avoidanceSensitivity = serializedObject.FindProperty("AvoidanceSensitivity");
            maxAvoidanceLength = serializedObject.FindProperty("MaxAvoidanceLength");
            yAxis = serializedObject.FindProperty("YAxis");
            rotateTowardsTarget = serializedObject.FindProperty("RotateTowardsTarget");
            rb = serializedObject.FindProperty("RB");
            turnForce = serializedObject.FindProperty("TurnForce");
            moveForce = serializedObject.FindProperty("MoveForce");
            strafeForce = serializedObject.FindProperty("StrafeForce");
            turnSpeed = serializedObject.FindProperty("TurnSpeed");
            moveSpeed = serializedObject.FindProperty("MoveSpeed");
            strafeSpeed = serializedObject.FindProperty("StrafeSpeed");
            stoppingDistance = serializedObject.FindProperty("StoppingDistance");
            destinationTransform = serializedObject.FindProperty("DestinationTransform");
            faceTowardsTransform = serializedObject.FindProperty("FaceTowardsTransform");
            maintainHeight = serializedObject.FindProperty("m_MaintainHeight");
            minHeight = serializedObject.FindProperty("minHeight");
            maxHeight = serializedObject.FindProperty("maxHeight");
            optimalHeight = serializedObject.FindProperty("optimalHeight");
            randomizeSpeedOnStart = serializedObject.FindProperty("m_RandomizeSpeedOnStart");
            animationController = serializedObject.FindProperty("m_Animator");
            legacyAnimationController = serializedObject.FindProperty("m_LegacyAnimation");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(ignoreList, true);
            EditorGUILayout.PropertyField(avoidanceSensitivity);
            EditorGUILayout.PropertyField(maxAvoidanceLength);
            EditorGUILayout.PropertyField(yAxis);
            EditorGUILayout.PropertyField(rotateTowardsTarget);
            EditorGUILayout.PropertyField(rb);
            if (rb.objectReferenceValue != null)
            {
                if ((rb.objectReferenceValue as Rigidbody).isKinematic)
                {
                    EditorGUILayout.PropertyField(turnSpeed);
                    EditorGUILayout.PropertyField(moveSpeed);
                    EditorGUILayout.PropertyField(strafeSpeed);
                }
                else
                {
                    EditorGUILayout.PropertyField(turnForce);
                    EditorGUILayout.PropertyField(moveForce);
                    EditorGUILayout.PropertyField(strafeForce);
                }
                EditorGUILayout.PropertyField(stoppingDistance);
                EditorGUILayout.PropertyField(destinationTransform);
                EditorGUILayout.PropertyField(faceTowardsTransform);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Speed Management", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(randomizeSpeedOnStart);
                if (randomizeSpeedOnStart.boolValue)
                {
                    EditorGUILayout.PropertyField(animationController);
                    EditorGUILayout.PropertyField(legacyAnimationController);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Height Management", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(maintainHeight);
                if (maintainHeight.boolValue) {
                    EditorGUILayout.PropertyField(minHeight);
                    EditorGUILayout.PropertyField(maxHeight);
                    EditorGUILayout.PropertyField(optimalHeight);
                }
            }

            displayErrors();

            serializedObject.ApplyModifiedProperties();
        }

        void displayErrors()
        {
            EditorGUILayout.Space();
            var raySensors = steeringRig.GetComponentsInChildren<RaySensor>();

            if (raySensors.Length == 0)
            {
                EditorGUILayout.HelpBox("Steering Rig looks for child Ray Sensors to calculate avoidance vectors, you should add some.", MessageType.Warning);
            }
            else
            {
                for (int i = 0; i < raySensors.Length; i++)
                {
                    if (raySensors[i].IgnoreList != null && raySensors[i].IgnoreList.Count > 0 && raySensors[i].IgnoreList != steeringRig.IgnoreList)
                    {
                        EditorGUILayout.HelpBox("One or more of the steering ray sensors has objects assigned to its IgnoreList parameter. "
                            + "These will be overwritten by the steering rigs IgnoreList.", MessageType.Warning);
                        break;
                    }
                }
            }
        }
    }
}
