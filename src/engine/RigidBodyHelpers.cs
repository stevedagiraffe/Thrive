using Godot;

/// <summary>
///   Common helper operations for RigidBodies
/// </summary>
public static class RigidBodyHelpers
{
    /// <summary>
    /// Basic rotation physics for a RigidBody based on a target point
    /// optimized for performance, control, and gameplay rather than simulation.
    /// </summary>
    public static void LookFollow(this RigidBody body, PhysicsDirectBodyState state,
        Transform transform, Vector3 targetPoint)
    {
        // various directions
        var up = new Vector3(0, 1, 0);
        var direction = transform.basis.Quat().Normalized();
        var targetDirection = transform.LookingAt(targetPoint, up).basis.Quat().Normalized();

        // quaternion representing desired rotation and axis angle pair
        var rotDifference = (targetDirection * direction.Inverse()).Normalized();
        var rotAxis = rotDifference.GetEuler().Normalized();
        var rotAngle = 2 * Mathf.Acos(rotDifference.w);

        // this is way cheaper than the PID controller and looks exactly the same
        // if you don't control your speed you'll over-steer
        if (rotAngle <= 0.02 && state.AngularVelocity.Abs() <
            Mathf.Pow(body.Mass, 2) * 3 * (rotAxis * 0.3f).Abs())
        {
            state.AngularVelocity = Vector3.Zero;
            return;
        }

        body.AddTorque(rotAxis * Mathf.Pow(body.Mass, 2) * 3);

        if (rotAngle <= Mathf.Pi / 2.0f && state.AngularVelocity.Abs() >
            Mathf.Pow(body.Mass, 2) * 3 * (rotAxis * 0.3f).Abs())
        {
            body.AddTorque(state.AngularVelocity * -0.5f);
        }
    }

    /// <summary>
    ///   Just slerps by a fixed amount towards the target point.
    ///   Weight of the slerp determines turn speed. Collisions dodgy.
    /// </summary>
    public static Transform LookSlerp(this RigidBody body, PhysicsDirectBodyState state, Vector3 targetPoint)
    {
        Transform target = state.Transform.LookingAt(targetPoint, new Vector3(0, 1, 0));

        // Need to manually normalize everything, otherwise the slerp fails
        Quat direction = state.Transform.basis.Quat().Normalized();
        Quat targetDirection = direction.Slerp(target.basis.Quat().Normalized(), 0.2f);

        return new Transform(new Basis(targetDirection), state.Transform.origin);
    }
}
