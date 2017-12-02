using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Audio;
using UnityEditor.Audio;
using UnityEditor;

namespace Pulpobot
{
    public class AudioMixerGroupFabric : EditorWindow
    {
        public AudioMixer mixer;
        public AudioMixerGroup parentGroup;
        bool openMixerNextFrame = false;

        // Add menu named "My Window" to the Window menu
        [MenuItem("Pulpobot/AudioMixerGroup Fabric")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            AudioMixerGroupFabric window = (AudioMixerGroupFabric)EditorWindow.GetWindow(typeof(AudioMixerGroupFabric));
            window.titleContent.text = "AudioMixerGroup Fabric";
            window.Show();
        }

        void OnGUI()
        {
            if (openMixerNextFrame)
            {
                System.Type window = PulpobotUtils.GetEditorWindowType("AudioMixerWindow");
                Type auxioMixerWindowType = window.UnderlyingSystemType;
                EditorWindow audioMixerWindow = GetWindow(auxioMixerWindowType, false, "Audio Mixer Window");
                audioMixerWindow.Show(true);
                auxioMixerWindowType = audioMixerWindow.GetType();
                openMixerNextFrame = false;
            }

            EditorGUILayout.HelpBox("This tool creates an AudioGroup per AaudioSource selected in the scene", MessageType.Info);
            GUILayout.Space(5);

            GUILayout.Label("Choose an AudioMixer from the project");
            EditorGUI.BeginChangeCheck();
            mixer = EditorGUILayout.ObjectField("Mixer ", mixer, typeof(AudioMixer), true) as AudioMixer;
            GUILayout.Space(10);
            if (EditorGUI.EndChangeCheck())
            {
                if (mixer != null)
                {
                    parentGroup = mixer.FindMatchingGroups("Master")[0];
                }
            }

            EditorGUI.BeginDisabledGroup(mixer == null || Selection.activeTransform == null);

            EditorGUI.BeginChangeCheck();
            parentGroup = EditorGUILayout.ObjectField("Parent Mixer Group", parentGroup, typeof(AudioMixerGroup), true) as AudioMixerGroup;
            if (EditorGUI.EndChangeCheck())
            {
                if (parentGroup == null)
                {
                    parentGroup = mixer.FindMatchingGroups("Master")[0];
                }
                else
                {
                    mixer = parentGroup.audioMixer;
                }
            }
            EditorGUI.EndDisabledGroup();

            if (Selection.activeTransform == null)
            {
                EditorGUILayout.HelpBox("Select a gameobject in the scene", MessageType.Warning);
            }

            EditorGUI.BeginDisabledGroup(mixer == null || Selection.activeTransform == null);

            if (GUILayout.Button("Create Groups"))
            {
                if (Selection.activeTransform != null)
                {
                    List<AudioSource> sources = new List<AudioSource>();
                    PulpobotUtils.GetAllChildsOfTypeRecursively<AudioSource>(Selection.activeTransform, ref sources);
                    CreateGroups(sources, mixer, parentGroup);
                    openMixerNextFrame = true;
                }
            }

            EditorGUI.EndDisabledGroup();
        }

        public static void CreateGroups(List<AudioSource> sources, AudioMixer mixer, AudioMixerGroup parentGroup = null)
        {
            System.Type window = PulpobotUtils.GetEditorWindowType("AudioMixerWindow");

            if (window != null)
            {
                //Open an AudioMixer Window, so we have an instance from where to obtain the mixer controllers
                Type auxioMixerWindowType = window.UnderlyingSystemType;
                EditorWindow audioMixerWindow = GetWindow(auxioMixerWindowType, false, "Audio Mixer Window");
                audioMixerWindow.Show(true);
                auxioMixerWindowType = audioMixerWindow.GetType();

                //Force the window to init
                MethodInfo initMethodInfo = auxioMixerWindowType.GetMethod("Init", BindingFlags.NonPublic | BindingFlags.Instance);
                initMethodInfo.Invoke(audioMixerWindow, null);

                //Obtain the AudioMixerGroupTreeView that contains the editor function to manage groups
                FieldInfo groupTreeField = auxioMixerWindowType.GetField("m_GroupTree" ,BindingFlags.Instance | BindingFlags.NonPublic);
                System.Object groupTree = groupTreeField.GetValue(audioMixerWindow);
                Type groupTreeType = groupTree.GetType(); //AudioMixerGroupTreeView

                //SNIPPET: class AudioMixerController : AudioMixer
                FieldInfo controllerFieldInfo = groupTreeType.GetField("m_Controller", BindingFlags.NonPublic | BindingFlags.Instance);
                controllerFieldInfo.SetValue(groupTree, mixer);
                System.Object controller = controllerFieldInfo.GetValue(groupTree);

                //Find the master group if needed
                System.Object masterGroup = parentGroup;
                if (masterGroup == null)
                {
                    PropertyInfo masterGroupProperty = controller.GetType().GetProperty("masterGroup", BindingFlags.Public | BindingFlags.Instance);
                    masterGroup = masterGroupProperty.GetValue(controller, null);
                }

                //Create a new group per audio source
                for (int i = 0; i < sources.Count; i++)
                {
                    MethodInfo groupFunction = controller.GetType().GetMethod("CreateNewGroup", BindingFlags.Public | BindingFlags.Instance);
                    System.Object newGroup = groupFunction.Invoke(controller, new object[] { sources[i].name, true });

                    //Add the new created group to the parent group
                    groupFunction = controller.GetType().GetMethod("AddChildToParent", BindingFlags.Public | BindingFlags.Instance);
                    groupFunction.Invoke(controller, new object[] { newGroup, masterGroup });

                    //Add the group to the current view to it shows up correctly
                    groupFunction = controller.GetType().GetMethod("AddGroupToCurrentView", BindingFlags.Public | BindingFlags.Instance);
                    groupFunction.Invoke(controller, new object[] { newGroup });

                    sources[i].outputAudioMixerGroup = newGroup as AudioMixerGroup;
                }

                //Set the new controller to the window
                FieldInfo windowControllerField = auxioMixerWindowType.GetField("m_Controller", BindingFlags.Instance | BindingFlags.NonPublic);
                windowControllerField.SetValue(audioMixerWindow, controller);

                //Force Repaint the mixer window
                windowControllerField = auxioMixerWindowType.GetField("m_Initialized", BindingFlags.Instance | BindingFlags.NonPublic);
                windowControllerField.SetValue(audioMixerWindow, false);

                MethodInfo repaintFunction = auxioMixerWindowType.GetMethod("Init", BindingFlags.NonPublic | BindingFlags.Instance);
                repaintFunction.Invoke(audioMixerWindow, null);
            }
        }
    }

}
