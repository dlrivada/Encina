using System.Runtime.CompilerServices;
using System.Text.Json;
using LanguageExt;

namespace RopUnions.Spike;

/// <summary>Non-benchmark facts: struct sizes, allocations of one construction, System.Text.Json behaviour.</summary>
public static class Probe
{
    private static readonly EncinaError Err = EncinaError.New("not found");

    public static void Run()
    {
        Console.WriteLine("== Sizes (bytes) ==");
        Console.WriteLine($"EncinaError                 {Unsafe.SizeOf<EncinaError>()}");
        Console.WriteLine($"Either<EncinaError,int>     {Unsafe.SizeOf<Either<EncinaError, int>>()}");
        Console.WriteLine($"Either<EncinaError,string>  {Unsafe.SizeOf<Either<EncinaError, string>>()}");
        Console.WriteLine($"StdResult<int>              {Unsafe.SizeOf<StdResult<int>>()}");
        Console.WriteLine($"StdResult<string>           {Unsafe.SizeOf<StdResult<string>>()}");
        Console.WriteLine($"Result<int> (custom)        {Unsafe.SizeOf<Result<int>>()}");
        Console.WriteLine($"Result<string> (custom)     {Unsafe.SizeOf<Result<string>>()}");
        Console.WriteLine($"HResult<int> (reference)    {Unsafe.SizeOf<HResult<int>>()}");

        Console.WriteLine("== Allocated bytes for one construction ==");
        Measure("Either<,int>  success", () => { Either<EncinaError, int> r = 42; return r.IsRight; });
        Measure("Either<,int>  failure", () => { Either<EncinaError, int> r = Err; return r.IsLeft; });
        Measure("StdResult<int> success", () => { StdResult<int> r = 42; return r.Value is int; });
        Measure("StdResult<int> failure", () => { StdResult<int> r = Err; return r.Value is EncinaError; });
        Measure("StdResult<str> success", () => { StdResult<string> r = "ok"; return r.Value is string; });
        Measure("Result<int>   success", () => { Result<int> r = 42; return r.IsSuccess; });
        Measure("Result<int>   failure", () => { Result<int> r = Err; return r.IsSuccess; });
        Measure("HResult<int>  success", () => { HResult<int> r = new HResult<int>.Ok(42); return r is HResult<int>.Ok; });
        Measure("Result<int>   switch on failure", () => { Result<int> r = Err; return r switch { int => true, EncinaError => false }; });
        Measure("StdResult<int> switch on failure", () => { StdResult<int> r = Err; return r switch { int => true, EncinaError => false }; });
        Measure("default(Result<int>) switch", () => { Result<int> r = default; try { return r switch { int => true, EncinaError => false }; } catch (Exception) { return false; } });
        try { StdResult<int> d = default; Console.WriteLine("default(StdResult<int>) switch -> " + (d switch { int => "int", EncinaError => "error" })); }
        catch (Exception ex) { Console.WriteLine("default(StdResult<int>) switch THROWS " + ex.GetType().Name); }
        try { Result<int> d = default; Console.WriteLine("default(Result<int>) switch -> " + (d switch { int => "int", EncinaError => "error" })); }
        catch (Exception ex) { Console.WriteLine("default(Result<int>) switch THROWS " + ex.GetType().Name); }

        Console.WriteLine("== Pattern matching on the custom union (compiler uses TryGetValue) ==");
        Result<int> custom = 7;
        var text = custom switch
        {
            int v => $"value {v}",
            EncinaError e => $"error {e.Message}",
        };
        Console.WriteLine(text);

        Console.WriteLine("== System.Text.Json ==");
        Json("Either<EncinaError,int> Right", (Either<EncinaError, int>)42);
        Json("Either<EncinaError,int> Left", (Either<EncinaError, int>)Err);
        Json("StdResult<int> success", (StdResult<int>)42);
        Json("StdResult<int> failure", (StdResult<int>)Err);
        Json("Result<int> success", (Result<int>)42);
        Json("Result<int> failure", (Result<int>)Err);
        Json("HResult<int> Ok", (HResult<int>)new HResult<int>.Ok(42));
        RoundTrip("StdResult<int>", (StdResult<int>)42);
        RoundTrip("Result<int>", (Result<int>)42);
        RoundTrip("StdResult<int> failure", (StdResult<int>)Err);
        RoundTrip("Result<int> failure", (Result<int>)Err);
        RoundTrip("StdResult<string> success", (StdResult<string>)"ok");
        RoundTrip("Result<string> failure", (Result<string>)Err);
    }

    private static void Measure(string name, Func<bool> f)
    {
        f(); f();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++) { f(); }
        var after = GC.GetAllocatedBytesForCurrentThread();
        Console.WriteLine($"{name,-26} {(after - before) / 1000.0:F1}");
    }

    private static void Json<T>(string name, T value)
    {
        try { Console.WriteLine($"{name,-32} {JsonSerializer.Serialize(value)}"); }
        catch (Exception ex) { Console.WriteLine($"{name,-32} THROWS {ex.GetType().Name}: {ex.Message}"); }
    }

    private static void RoundTrip<T>(string name, T value)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            var back = JsonSerializer.Deserialize<T>(json);
            Console.WriteLine($"{name,-32} round trip {json} -> {(back is IUnion u ? u.Value : back)}");
        }
        catch (Exception ex) { Console.WriteLine($"{name,-32} round trip THROWS {ex.GetType().Name}: {ex.Message}"); }
    }
}
