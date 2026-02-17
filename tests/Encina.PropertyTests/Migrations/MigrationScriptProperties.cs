using Encina.Sharding.Migrations;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Migrations;

/// <summary>
/// Property-based tests for <see cref="MigrationScript"/> record semantics and validation invariants.
/// Verifies record equality, value preservation, and that valid non-whitespace inputs
/// produce valid instances.
/// </summary>
[Trait("Category", "Property")]
public sealed class MigrationScriptProperties
{
    #region Record Equality

    /// <summary>
    /// Two MigrationScript instances with identical values are equal (record semantics).
    /// </summary>
    [Property(MaxTest = 100)]
    public Property TwoScriptsWithSameValues_AreEqual()
    {
        return Prop.ForAll(Arb.From(BuildScriptGen()), script =>
        {
            var clone = new MigrationScript(
                script.Id, script.UpSql, script.DownSql,
                script.Description, script.Checksum);

            return script == clone;
        });
    }

    /// <summary>
    /// Two MigrationScript instances with identical values have the same hash code.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property TwoScriptsWithSameValues_HaveSameHashCode()
    {
        return Prop.ForAll(Arb.From(BuildScriptGen()), script =>
        {
            var clone = new MigrationScript(
                script.Id, script.UpSql, script.DownSql,
                script.Description, script.Checksum);

            return script.GetHashCode() == clone.GetHashCode();
        });
    }

    /// <summary>
    /// Two MigrationScript instances with different IDs are not equal.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ScriptsWithDifferentIds_AreNotEqual()
    {
        var gen = GenScriptId().SelectMany(id1 =>
            GenScriptId().Where(id2 => id2 != id1).SelectMany(id2 =>
                GenSql().SelectMany(upSql =>
                    GenSql().SelectMany(downSql =>
                        GenDescription().SelectMany(desc =>
                            GenChecksum().Select(checksum => (
                                Script1: new MigrationScript(id1, upSql, downSql, desc, checksum),
                                Script2: new MigrationScript(id2, upSql, downSql, desc, checksum))))))));

        return Prop.ForAll(Arb.From(gen), pair =>
            pair.Script1 != pair.Script2);
    }

    #endregion

    #region Value Preservation

    /// <summary>
    /// The Id property preserves the value provided to the constructor.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ScriptId_IsPreserved_AsProvided()
    {
        return Prop.ForAll(Arb.From(BuildScriptPartsGen()), values =>
        {
            var script = new MigrationScript(
                values.Id, values.Up, values.Down, values.Desc, values.Cs);

            return script.Id == values.Id;
        });
    }

    /// <summary>
    /// All fields are preserved exactly as provided to the constructor.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AllFields_ArePreserved_AsProvided()
    {
        return Prop.ForAll(Arb.From(BuildScriptPartsGen()), values =>
        {
            var script = new MigrationScript(
                values.Id, values.Up, values.Down, values.Desc, values.Cs);

            return script.Id == values.Id
                && script.UpSql == values.Up
                && script.DownSql == values.Down
                && script.Description == values.Desc
                && script.Checksum == values.Cs;
        });
    }

    #endregion

    #region Valid Construction

    /// <summary>
    /// Any non-whitespace string combination creates a valid MigrationScript without throwing.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ScriptWithNonWhitespaceValues_CanBeCreated()
    {
        return Prop.ForAll(Arb.From(BuildScriptPartsGen()), values =>
        {
            try
            {
                var script = new MigrationScript(
                    values.Id, values.Up, values.Down, values.Desc, values.Cs);
                return script is not null;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
            {
                return false;
            }
#pragma warning restore CA1031
        });
    }

    /// <summary>
    /// A MigrationScript is always equal to itself (reflexive equality).
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Script_Equals_ItselfAlways()
    {
        return Prop.ForAll(Arb.From(BuildScriptGen()), script =>
        {
#pragma warning disable CS1718 // Comparison made to same variable
            return script == script;
#pragma warning restore CS1718
        });
    }

    #endregion

    #region Generators

    private static Gen<string> GenScriptId()
    {
        return Gen.Elements(
            "migration_001", "20260216_add_index", "v2_alter_orders",
            "add_users_table", "fix_schema_drift");
    }

    private static Gen<string> GenSql()
    {
        return Gen.Elements(
            "CREATE INDEX idx_orders ON orders(status);",
            "ALTER TABLE users ADD COLUMN email TEXT;",
            "DROP INDEX idx_old;",
            "CREATE TABLE events (id INT PRIMARY KEY);",
            "ALTER TABLE orders DROP COLUMN legacy;");
    }

    private static Gen<string> GenDescription()
    {
        return Gen.Elements(
            "Add index on orders.status",
            "Add email column to users",
            "Remove old index",
            "Create events table",
            "Remove legacy column");
    }

    private static Gen<string> GenChecksum()
    {
        return Gen.Elements(
            "sha256:a1b2c3d4e5", "sha256:ff00ee11dd22", "sha256:1234567890ab",
            "md5:abcdef012345", "sha256:deadbeef0000");
    }

    private static Gen<MigrationScript> BuildScriptGen()
    {
        return GenScriptId().SelectMany(id =>
            GenSql().SelectMany(up =>
                GenSql().SelectMany(down =>
                    GenDescription().SelectMany(desc =>
                        GenChecksum().Select(cs =>
                            new MigrationScript(id, up, down, desc, cs))))));
    }

    private static Gen<(string Id, string Up, string Down, string Desc, string Cs)> BuildScriptPartsGen()
    {
        return GenScriptId().SelectMany(id =>
            GenSql().SelectMany(up =>
                GenSql().SelectMany(down =>
                    GenDescription().SelectMany(desc =>
                        GenChecksum().Select(cs => (id, up, down, desc, cs))))));
    }

    #endregion
}
