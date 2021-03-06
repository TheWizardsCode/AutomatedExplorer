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
        SerializedProperty maxVerticalVelocity;
        SerializedProperty maxForwardVelocity;
        SerializedProperty animationController;
        SerializedProperty randomizeX;
        SerializedProperty randomizeY;
        SerializedProperty randomizeZ;
        SerializedProperty randomizationFrequency;
        SerializedProperty randomizationFactor;

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
            animationController = serializedObject.FindProperty("m_Animator");
            maxVerticalVelocity = serializedObject.FindProperty("m_MaxVerticalVelocity");
            maxForwardVelocity = serializedObject.FindProperty("m_MaxForwardVelocity");
            randomizeX = serializedObject.FindProperty("m_RandomizeX");
            randomizeY = serializedObject.FindProperty("m_RandomizeY");
            randomizeZ = serializedObject.FindProperty("m_RandomizeZ");
            randomizationFrequency = serializedObject.FindProperty("m_RandomizationFrequency");
            randomizationFactor = serializedObject.FindProperty("m_RandomizationFactor");
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
                EditorGUILayout.LabelField("Flight Randomization", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(randomizeX);
                EditorGUILayout.PropertyField(randomizeY);
                EditorGUILayout.PropertyField(randomizeZ);
                if (randomizeX.boolValue || randomizeY.boolValue || randomizeZ.boolValue)
                {
                    EditorGUILayout.PropertyField(randomizationFrequency);
                    EditorGUILayout.PropertyField(randomizationFactor);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(animationController);
                if (animationController.objectReferenceValue != null)
                {
                    EditorGUILayout.PropertyField(maxVerticalVelocity);
                    EditorGUILayout.PropertyField(maxForwardVelocity);
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
