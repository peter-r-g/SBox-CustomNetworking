using System;

namespace CustomNetworking.Shared.Networkables;

/// <summary>
/// Marks a networked property to be editable by a client.
/// </summary>
[AttributeUsage( AttributeTargets.Property )]
public class ClientAuthorityAttribute : Attribute
{
}
