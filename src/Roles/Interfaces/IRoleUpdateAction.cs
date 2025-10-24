namespace TheBetterRoles.Roles.Interfaces;

internal interface IRoleUpdateAction : IRoleAction
{
    /// <summary>
    /// Called once per frame to update the state of the role or perform actions.
    /// Override this method to implement any per-frame logic, such as checking conditions, updating timers, or managing abilities.
    /// </summary>
    void Update() { }

    /// <summary>
    /// Called 50 times a frame to update the state of the role or perform actions.
    /// Override this method to implement any per-frame logic, such as checking conditions, updating timers, or managing abilities.
    /// </summary>
    void FixedUpdate() { }

    /// <summary>
    /// Called after all Updates to update the state of the role or perform actions.
    /// Override this method to implement any per-frame logic, such as checking conditions, updating timers, or managing abilities.
    /// </summary>
    void LateUpdate() { }
}
