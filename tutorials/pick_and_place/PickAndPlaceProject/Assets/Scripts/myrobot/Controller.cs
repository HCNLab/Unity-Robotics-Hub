using System;
using Unity.Robotics;
using UnityEngine;
using System.Collections; // kh

namespace MyProject.RobotControl
{
    public enum RotationDirection { None = 0, Positive = 1, Negative = -1 };
    public enum ControlType { PositionControl };

    public class Controller : MonoBehaviour
    {
        private ArticulationBody[] articulationChain;
        // Stores original colors of the part being highlighted
        private Color[] prevColor;
        private int previousIndex;

        private bool isAnimating = false; // kh
        private bool spaceToggle = false; // kh




        [InspectorReadOnly(hideInEditMode: true)]
        public string selectedJoint;
        [HideInInspector]
        public int selectedIndex;

        public ControlType control = ControlType.PositionControl;
        public float stiffness;
        public float damping;
        public float forceLimit;
        public float speed = 5f; // Units: degree/s
        public float torque = 100f; // Units: Nm or N
        public float acceleration = 5f;// Units: m/s^2 / degree/s^2

        [Tooltip("Color to highlight the currently selected join")]
        public Color highLightColor = new Color(1.0f, 0, 0, 1.0f);

        void Start()
        {
            previousIndex = selectedIndex = 1;
            this.gameObject.AddComponent<FKRobot>();
            articulationChain = this.GetComponentsInChildren<ArticulationBody>();
            int defDyanmicVal = 10;
            foreach (ArticulationBody joint in articulationChain)
            {
                joint.gameObject.AddComponent<JointControl>();
                joint.jointFriction = defDyanmicVal;
                joint.angularDamping = defDyanmicVal;
                ArticulationDrive currentDrive = joint.xDrive;
                currentDrive.forceLimit = forceLimit;
                joint.xDrive = currentDrive;
            }
            DisplaySelectedJoint(selectedIndex);
            StoreJointColors(selectedIndex);
        }

        void SetSelectedJointIndex(int index)
        {
            if (articulationChain.Length > 0)
            {
                selectedIndex = (index + articulationChain.Length) % articulationChain.Length;
            }
        }

        void Update()
        {
            bool SelectionInput1 = Input.GetKeyDown("right");
            bool SelectionInput2 = Input.GetKeyDown("left");
            bool spacePressed = Input.GetKeyDown(KeyCode.Space); /// kh

            SetSelectedJointIndex(selectedIndex); // to make sure it is in the valid range
            UpdateDirection(selectedIndex);

            if (SelectionInput2)
            {
                SetSelectedJointIndex(selectedIndex - 1);
                Highlight(selectedIndex);
            }
            else if (SelectionInput1)
            {
                SetSelectedJointIndex(selectedIndex + 1);
                Highlight(selectedIndex);
            }

            // kh
            if (spacePressed && !isAnimating)
            {
                spaceToggle = !spaceToggle;

                if (spaceToggle)
                {
                    StartCoroutine(RotateAndPick());
                }
                else
                {
                    StartCoroutine(RotateBack());
                }
            }

            UpdateDirection(selectedIndex);
        }

        /// <summary>
        /// Highlights the color of the robot by changing the color of the part to a color set by the user in the inspector window
        /// </summary>
        /// <param name="selectedIndex">Index of the link selected in the Articulation Chain</param>
        private void Highlight(int selectedIndex)
        {
            if (selectedIndex == previousIndex || selectedIndex < 0 || selectedIndex >= articulationChain.Length)
            {
                return;
            }

            // reset colors for the previously selected joint
            ResetJointColors(previousIndex);

            // store colors for the current selected joint
            StoreJointColors(selectedIndex);

            DisplaySelectedJoint(selectedIndex);
            Renderer[] rendererList = articulationChain[selectedIndex].transform.GetChild(0).GetComponentsInChildren<Renderer>();

            // set the color of the selected join meshes to the highlight color
            foreach (var mesh in rendererList)
            {
                MaterialExtensions.SetMaterialColor(mesh.material, highLightColor);
            }
        }

        void DisplaySelectedJoint(int selectedIndex)
        {
            if (selectedIndex < 0 || selectedIndex >= articulationChain.Length)
            {
                return;
            }
            selectedJoint = articulationChain[selectedIndex].name + " (" + selectedIndex + ")";
        }

        /// <summary>
        /// Sets the direction of movement of the joint on every update
        /// </summary>
        /// <param name="jointIndex">Index of the link selected in the Articulation Chain</param>
        private void UpdateDirection(int jointIndex)
        {
            if (jointIndex < 0 || jointIndex >= articulationChain.Length)
            {
                return;
            }

            float moveDirection = Input.GetAxis("Vertical");
            JointControl current = articulationChain[jointIndex].GetComponent<JointControl>();
            if (previousIndex != jointIndex)
            {
                JointControl previous = articulationChain[previousIndex].GetComponent<JointControl>();
                previous.direction = RotationDirection.None;
                previousIndex = jointIndex;
            }

            if (current.controltype != control)
            {
                UpdateControlType(current);
            }

            if (moveDirection > 0)
            {
                current.direction = RotationDirection.Positive;
            }
            else if (moveDirection < 0)
            {
                current.direction = RotationDirection.Negative;
            }
            else
            {
                current.direction = RotationDirection.None;
            }
        }

        /// <summary>
        /// Stores original color of the part being highlighted
        /// </summary>
        /// <param name="index">Index of the part in the Articulation chain</param>
        private void StoreJointColors(int index)
        {
            Renderer[] materialLists = articulationChain[index].transform.GetChild(0).GetComponentsInChildren<Renderer>();
            prevColor = new Color[materialLists.Length];
            for (int counter = 0; counter < materialLists.Length; counter++)
            {
                prevColor[counter] = MaterialExtensions.GetMaterialColor(materialLists[counter]);
            }
        }

        /// <summary>
        /// Resets original color of the part being highlighted
        /// </summary>
        /// <param name="index">Index of the part in the Articulation chain</param>
        private void ResetJointColors(int index)
        {
            Renderer[] previousRendererList = articulationChain[index].transform.GetChild(0).GetComponentsInChildren<Renderer>();
            for (int counter = 0; counter < previousRendererList.Length; counter++)
            {
                MaterialExtensions.SetMaterialColor(previousRendererList[counter].material, prevColor[counter]);
            }
        }

        public void UpdateControlType(JointControl joint)
        {
            joint.controltype = control;
            if (control == ControlType.PositionControl)
            {
                ArticulationDrive drive = joint.joint.xDrive;
                drive.stiffness = stiffness;
                drive.damping = damping;
                joint.joint.xDrive = drive;
            }
        }

        public void OnGUI()
        {
            GUIStyle centeredStyle = GUI.skin.GetStyle("Label");
            centeredStyle.alignment = TextAnchor.UpperCenter;
            GUI.Label(new Rect(Screen.width / 2 - 200, 10, 400, 20), "Press left/right arrow keys to select a robot joint.", centeredStyle);
            GUI.Label(new Rect(Screen.width / 2 - 200, 30, 400, 20), "Press up/down arrow keys to move " + selectedJoint + ".", centeredStyle);
        }

        IEnumerator RotateAndPick()
        {
            isAnimating = true;

            // 1. 제1관절 회전 (시계 방향 180도 → target = -180)
            yield return StartCoroutine(RotateJoint(1, -180f, 1f));  // 0번째 관절

            // 2. 팔 들어올리기
            float[] liftPose = new float[] { 0f, -180f, 30f, -45f, 0f, 0f, 0f }; // 첫 번째는 그대로 유지
            yield return StartCoroutine(MoveToPose(liftPose, 1.5f));

            // 3. 집게 닫았다 펴기
            SetGripper(-10f, 10f);
            yield return new WaitForSeconds(0.5f);

            SetGripper(0f, 0f);
            yield return new WaitForSeconds(0.5f);

            isAnimating = false;
        }

        IEnumerator RotateBack()
        {
            isAnimating = true;

            // 1. 팔 내려놓기 Pose (모든 target을 0으로)
            float[] restPose = new float[articulationChain.Length];
            for (int i = 0; i < restPose.Length; i++) restPose[i] = 0f;
            yield return StartCoroutine(MoveToPose(restPose, 1.5f));

            // 2. 몸통 원위치 (target = 0)
            yield return StartCoroutine(RotateJoint(1, 0f, 1f));

            isAnimating = false;
        }

        IEnumerator RotateJoint(int jointIndex, float targetAngle, float duration)
        {
            var joint = articulationChain[jointIndex];
            float startAngle = joint.xDrive.target;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                float interpolated = Mathf.Lerp(startAngle, targetAngle, t);

                var drive = joint.xDrive;
                drive.target = interpolated;
                joint.xDrive = drive;

                yield return null;
            }
        }




        IEnumerator PlayPickAndLiftSequence()
        {
            isAnimating = true;

            // 1. 팔 들어올리기 동작 예시
            float[] liftPose = new float[] { 0f, 30f, -45f, 0f, 0f, 0f }; // 관절 순서에 맞춰 수정 필요
            yield return StartCoroutine(MoveToPose(liftPose, 1.5f));

            // 2. 집게 닫기 (예: 양쪽 -10도 / +10도 등)
            SetGripper(-10f, 10f);
            yield return new WaitForSeconds(0.5f);

            // 3. 집게 펴기
            SetGripper(0f, 0f);
            yield return new WaitForSeconds(0.5f);

            isAnimating = false;
        }

        IEnumerator MoveToPose(float[] angles, float duration)
        {
            float t = 0f;
            float[] startAngles = new float[angles.Length];

            for (int i = 0; i < angles.Length && i < articulationChain.Length; i++)
                startAngles[i] = articulationChain[i].xDrive.target;

            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                for (int i = 0; i < angles.Length && i < articulationChain.Length; i++)
                {
                    var drive = articulationChain[i].xDrive;
                    drive.target = Mathf.Lerp(startAngles[i], angles[i], t);
                    articulationChain[i].xDrive = drive;
                }
                yield return null;
            }
        }

        void SetGripper(float left, float right)
        {
            int count = articulationChain.Length;
            if (count < 2) return;

            var leftDrive = articulationChain[count - 2].xDrive;
            leftDrive.target = left;
            articulationChain[count - 2].xDrive = leftDrive;

            var rightDrive = articulationChain[count - 1].xDrive;
            rightDrive.target = right;
            articulationChain[count - 1].xDrive = rightDrive;
        }



    }
}
