```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,098.0 ns |  3,703.1 ns |   202.98 ns | 4,058.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   226.0 ns |    855.1 ns |    46.87 ns |   209.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,592.3 ns | 36,053.0 ns | 1,976.18 ns | 3,517.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   147.3 ns |    416.2 ns |    22.81 ns |   160.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   273.3 ns |  1,369.3 ns |    75.06 ns |   270.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   214.0 ns |  1,558.4 ns |    85.42 ns |   181.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   221.0 ns |    316.0 ns |    17.32 ns |   211.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,529.0 ns |  3,275.1 ns |   179.52 ns | 3,435.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,640.0 ns |  2,810.8 ns |   154.07 ns | 3,567.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,329.5 ns |  1,493.3 ns |    81.85 ns | 2,349.5 ns |      96 B |
