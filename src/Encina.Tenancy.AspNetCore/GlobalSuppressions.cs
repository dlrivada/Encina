// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// Temporarily suppress RS0016/RS0017 until PublicAPI.txt is properly generated
// TODO: Remove these suppressions and regenerate PublicAPI.txt in Visual Studio
[assembly: SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "PublicAPI.txt needs regeneration")]
[assembly: SuppressMessage("ApiDesign", "RS0017:Remove deleted types and members from the declared API", Justification = "PublicAPI.txt needs regeneration")]
