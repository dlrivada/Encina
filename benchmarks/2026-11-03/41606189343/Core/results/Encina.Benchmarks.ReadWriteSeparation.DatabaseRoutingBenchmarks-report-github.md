```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev      | Median      | Allocated |
|--------------------------------------- |------------:|-------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 6,185.00 ns | 22,410.33 ns | 1,228.39 ns | 5,847.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    86.67 ns |  1,499.49 ns |    82.19 ns |    96.50 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,578.50 ns | 29,181.57 ns | 1,599.54 ns | 2,858.50 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   887.00 ns | 14,198.49 ns |   778.27 ns |   647.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   265.33 ns |  2,135.48 ns |   117.05 ns |   208.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   149.83 ns |  2,191.86 ns |   120.14 ns |   135.50 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   223.83 ns |  2,983.58 ns |   163.54 ns |   136.50 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 5,487.83 ns | 26,423.39 ns | 1,448.36 ns | 4,774.50 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 5,626.33 ns | 28,819.98 ns | 1,579.72 ns | 4,954.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 3,603.00 ns | 10,238.43 ns |   561.20 ns | 3,311.00 ns |      96 B |
