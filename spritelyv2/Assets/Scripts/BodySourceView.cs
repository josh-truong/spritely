using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kinect = Windows.Kinect;
using System.Linq;

public class BodySourceView : MonoBehaviour 
{
    public GameObject Prefab;
    public GameObject BodySourceManager;
    
    private Dictionary<ulong, GameObject> _Bodies = new Dictionary<ulong, GameObject>();
    private BodySourceManager _BodyManager;
    
    private Dictionary<Kinect.JointType, Kinect.JointType> _K2KBoneMap = new Dictionary<Kinect.JointType, Kinect.JointType>()
    {
        { Kinect.JointType.FootLeft, Kinect.JointType.AnkleLeft },
        { Kinect.JointType.AnkleLeft, Kinect.JointType.KneeLeft },
        { Kinect.JointType.KneeLeft, Kinect.JointType.HipLeft },
        { Kinect.JointType.HipLeft, Kinect.JointType.SpineBase },
        
        { Kinect.JointType.FootRight, Kinect.JointType.AnkleRight },
        { Kinect.JointType.AnkleRight, Kinect.JointType.KneeRight },
        { Kinect.JointType.KneeRight, Kinect.JointType.HipRight },
        { Kinect.JointType.HipRight, Kinect.JointType.SpineBase },
        
        { Kinect.JointType.HandTipLeft, Kinect.JointType.HandLeft },
        { Kinect.JointType.ThumbLeft, Kinect.JointType.HandLeft },
        { Kinect.JointType.HandLeft, Kinect.JointType.ElbowLeft },
        { Kinect.JointType.ElbowLeft, Kinect.JointType.ShoulderLeft },
        { Kinect.JointType.ShoulderLeft, Kinect.JointType.SpineShoulder },
        
        { Kinect.JointType.HandTipRight, Kinect.JointType.HandRight },
        { Kinect.JointType.ThumbRight, Kinect.JointType.HandRight },
        { Kinect.JointType.HandRight, Kinect.JointType.ElbowRight },
        { Kinect.JointType.ElbowRight, Kinect.JointType.ShoulderRight },
        { Kinect.JointType.ShoulderRight, Kinect.JointType.SpineShoulder },
        
        { Kinect.JointType.SpineBase, Kinect.JointType.SpineMid },
        { Kinect.JointType.SpineMid, Kinect.JointType.SpineShoulder },
        { Kinect.JointType.SpineShoulder, Kinect.JointType.Neck },
        { Kinect.JointType.Neck, Kinect.JointType.Head },
    };

    private Dictionary<Kinect.JointType, string> _K2MBoneMap = new Dictionary<Kinect.JointType, string>()
    {
        { Kinect.JointType.FootLeft, "B-toe_L" },
        { Kinect.JointType.AnkleLeft, "B-foot_L" },
        { Kinect.JointType.KneeLeft, "B-shin_L" },
        { Kinect.JointType.HipLeft, "B-thigh_L" },
            
        { Kinect.JointType.FootRight, "B-toe_R" },
        { Kinect.JointType.AnkleRight, "B-foot_R" },
        { Kinect.JointType.KneeRight, "B-shin_R" },
        { Kinect.JointType.HipRight, "B-thigh_R" },
            
        { Kinect.JointType.HandTipLeft, "B-f_middle_03_L" },
        { Kinect.JointType.ThumbLeft, "B-thumb_03_L" },
        { Kinect.JointType.HandLeft, "B-hand_L" },
        { Kinect.JointType.ElbowLeft, "B-forearm_L" },
        { Kinect.JointType.ShoulderLeft, "B-shoulder_L" },
            
        { Kinect.JointType.HandTipRight, "B-f_middle_03_R" },
        { Kinect.JointType.ThumbRight, "B-thumb_03_R" },
        { Kinect.JointType.HandRight, "B-hand_R" },
        { Kinect.JointType.ElbowRight, "B-forearm_R" },
        { Kinect.JointType.ShoulderRight, "B-shoulder_R" },
            
        { Kinect.JointType.SpineBase, "B-hips" },
        { Kinect.JointType.SpineMid, "B-spine" },
        { Kinect.JointType.SpineShoulder, "B-upperChest" },
        { Kinect.JointType.Neck, "B-neck" },
        { Kinect.JointType.Head, "B-head" }
    };

    
    void Update () 
    {
        if (BodySourceManager == null) { return; }
        
        _BodyManager = BodySourceManager.GetComponent<BodySourceManager>();
        if (_BodyManager == null) { return; }
        
        Kinect.Body[] data = _BodyManager.GetData();
        if (data == null) { return; }
        
        List<ulong> trackedIds = new List<ulong>();
        foreach(var body in data)
        {
            if (body == null) { continue; }
            if(body.IsTracked) { trackedIds.Add (body.TrackingId); }
        }
        
        List<ulong> knownIds = new List<ulong>(_Bodies.Keys);
        
        // First delete untracked bodies
        foreach(ulong trackingId in knownIds)
        {
            if(!trackedIds.Contains(trackingId))
            {
                Destroy(_Bodies[trackingId]);
                _Bodies.Remove(trackingId);
            }
        }

        foreach(var body in data)
        {
            if (body == null) { continue; }
            
            if(body.IsTracked)
            {
                if(!_Bodies.ContainsKey(body.TrackingId))
                {
                    _Bodies[body.TrackingId] = CreateBodyObject(body.TrackingId);
                }
                RefreshBodyObject(body, _Bodies[body.TrackingId]);
            }
        }
    }
    
    // Initiate a body object
    private GameObject CreateBodyObject(ulong id)
    {
        GameObject body = Instantiate(Prefab, new Vector3(0, 0, 0), Quaternion.identity);
        body.name = "Body:" + id;
        
        return body;
    }
    
    // Update object
    private void RefreshBodyObject(Kinect.Body body, GameObject obj)
    {
        List<string> modelJointsList = _K2MBoneMap.Values.ToList();
        // Get an array of all the child transforms in the prefab using GetComponentsInChildren
        List<Transform> modelChildrens = obj.transform.GetComponentsInChildren<Transform>().ToList();
        Dictionary<string, GameObject> modelDict = new Dictionary<string, GameObject>();
        // Loop through each child transform and create a dictionary associated with the models joints
        modelChildrens.ForEach(child => {
            GameObject obj = child.gameObject;
            if (modelJointsList.Contains(obj.name)) { modelDict.Add(obj.name, obj); }
        });

        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            Kinect.JointType KParent = jt;
            Kinect.JointType? KChild = (_K2KBoneMap.ContainsKey(jt)) ? _K2KBoneMap[jt] : null;

            if (KChild.HasValue)
            {
                // Get Kinect Joints
                Kinect.Joint KSourceJoint = body.Joints[jt];
                Kinect.Joint KTargetJoint = body.Joints[KChild.Value];

                // Get GameObject Model joints
                GameObject MSourceJoint = modelDict[_K2MBoneMap[KParent]];
                GameObject MTargetJoint = modelDict[_K2MBoneMap[KChild.Value]];

                // Update GameObject joints using Kinect joints
                MSourceJoint.transform.position = GetVector3FromJoint(KSourceJoint);
                MTargetJoint.transform.position = GetVector3FromJoint(KTargetJoint);
            }
        }
    }

    private static Vector3 GetVector3FromJoint(Kinect.Joint joint)
    {
        return new Vector3(joint.Position.X * 10, joint.Position.Y * 10, joint.Position.Z * 10);
    }
}
