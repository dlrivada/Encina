```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 5,471.8 ns |  7,815.5 ns | 428.39 ns | 5,224.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   113.3 ns |  1,004.8 ns |  55.08 ns |   140.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,443.5 ns | 10,719.5 ns | 587.57 ns | 4,603.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   298.7 ns |  3,356.1 ns | 183.96 ns |   235.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   268.8 ns |  3,065.5 ns | 168.03 ns |   305.5 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   281.7 ns |  1,753.0 ns |  96.09 ns |   265.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   198.8 ns |    918.2 ns |  50.33 ns |   205.5 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,232.0 ns |  8,448.8 ns | 463.11 ns | 4,088.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 4,312.0 ns |  4,287.7 ns | 235.03 ns | 4,259.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 3,216.2 ns |  7,197.4 ns | 394.51 ns | 3,155.5 ns |      96 B |
