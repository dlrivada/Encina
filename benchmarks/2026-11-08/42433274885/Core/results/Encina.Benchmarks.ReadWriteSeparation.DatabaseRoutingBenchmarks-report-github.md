```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                                 | Mean        | Error     | StdDev    | Median      | Allocated |
|--------------------------------------- |------------:|----------:|----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 1,708.31 ns | 562.61 ns | 770.11 ns | 1,665.50 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    22.32 ns |  16.14 ns |  23.14 ns |    15.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 1,366.38 ns | 629.72 ns | 861.97 ns | 1,713.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |    28.19 ns |  21.06 ns |  29.52 ns |    21.00 ns |         - |
| &#39;Read HasIntent&#39;                       |    28.00 ns |  39.61 ns |  55.53 ns |    10.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |    23.00 ns |  14.12 ns |  20.25 ns |    20.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |    16.56 ns |  11.34 ns |  15.89 ns |    15.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        |   802.00 ns | 139.98 ns | 191.61 ns |   816.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 1,361.08 ns | 613.28 ns | 839.46 ns | 1,928.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         |   965.30 ns | 487.29 ns | 683.11 ns |   466.00 ns |      96 B |
