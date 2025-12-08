using System.Linq;
using DV;
using DV.Customization;
using DV.Damage;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyUniversalJato;

public static class TrainCarHelper
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;

    public static Vector3? GetApproxStandardJatoPosition(TrainCar trainCar, bool isRear = false)
    {
        var coupler = isRear ? trainCar.rearCoupler : trainCar.frontCoupler;
        var collider = coupler.GetComponent<BoxCollider>();

        Bounds b = collider.bounds;

        Vector3 c = b.center;
        Vector3 e = b.extents;

        Vector3 worldBottomRight = new Vector3(c.x + e.x, c.y - e.y, c.z - e.z);
        Vector3 bottomRight = trainCar.transform.InverseTransformPoint(worldBottomRight);

        return bottomRight;
    }

    public static Vector3? GetApproxStandardRearJatoPosition(TrainCar trainCar)
    {
        return GetApproxStandardJatoPosition(trainCar, isRear: true);
    }

    public static Vector3? GetApproxStandardFrontJatoPosition(TrainCar trainCar)
    {
        return GetApproxStandardJatoPosition(trainCar, isRear: false);
    }

    public static void RerailTrain(TrainCar trainCar, bool isReverse = false)
    {
        var (closestTrack, point) = RailTrack.GetClosest(trainCar.transform.position);

        if (point == null)
            return;

        var rerailPos = (Vector3)point.Value.position + WorldMover.currentMove;

        var forward = point.Value.forward;

        if (isReverse)
            forward = -forward;

        void OnRerailed()
        {
            trainCar.brakeSystem.SetHandbrakePosition(0); //, forced: true
            trainCar.OnRerailed -= OnRerailed;
        }

        trainCar.OnRerailed += OnRerailed;

        if (trainCar.derailed)
            trainCar.Rerail(closestTrack, rerailPos, forward);
        else
            trainCar.SetTrack(closestTrack, rerailPos, forward);
    }


    public static void EnableNoDerail()
    {
        var oldVal = Globals.G.GameParams.DerailStressThreshold;
        Logger.Log($"Enable no-derail ({oldVal}=>infinity)");
        Globals.G.GameParams.DerailStressThreshold = float.PositiveInfinity;
    }

    public static void DisableNoDerail()
    {
        var oldVal = Globals.G.GameParams.DerailStressThreshold;
        Logger.Log($"Disable no-derail ({oldVal}=>{Globals.G.GameParams.defaultStressThreshold})");
        Globals.G.GameParams.DerailStressThreshold = Globals.G.GameParams.defaultStressThreshold;
    }

    public static TrainCarCustomization lastTrainCarCustomization;

    public static float GetForwardSpeed(TrainCar car)
    {
        // TODO: cache
        var customComp = car.GetComponent<TrainCarCustomization>();
        return customComp.ReadPort(STDSimPort.WheelSpeedKMH);
    }

    public static void RepairTrain(TrainCar car)
    {
        // TODO: 
        // car.CarDamage.RepairCarEffectivePercentage(100f);

        var damageController = car.GetComponent<DamageController>();
        damageController.RepairAll();
    }
}