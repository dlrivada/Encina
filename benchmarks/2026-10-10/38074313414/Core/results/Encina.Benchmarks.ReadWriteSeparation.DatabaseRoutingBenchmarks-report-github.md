```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median      | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,745.7 ns | 18,796.8 ns | 1,030.32 ns | 3,555.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   121.0 ns |  1,015.8 ns |    55.68 ns |   111.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,025.5 ns | 16,581.4 ns |   908.88 ns | 2,658.50 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   140.0 ns |  1,015.8 ns |    55.68 ns |   130.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   125.3 ns |  1,631.8 ns |    89.44 ns |    95.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   125.0 ns |  1,377.4 ns |    75.50 ns |   135.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   110.3 ns |  1,859.6 ns |   101.93 ns |   130.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 1,582.7 ns | 16,150.8 ns |   885.28 ns | 1,403.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,990.8 ns | 11,253.6 ns |   616.85 ns | 2,953.50 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,093.7 ns |  9,381.9 ns |   514.25 ns | 1,027.00 ns |      96 B |
