```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,205.0 ns | 1,925.3 ns | 105.53 ns | 4,208.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   213.0 ns |   813.2 ns |  44.58 ns |   210.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,564.0 ns | 2,679.2 ns | 146.86 ns | 3,587.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   250.5 ns |   982.6 ns |  53.86 ns |   230.5 ns |         - |
| &#39;Read HasIntent&#39;                       |   243.0 ns |   742.6 ns |  40.71 ns |   220.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   344.0 ns | 2,305.3 ns | 126.36 ns |   361.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   270.5 ns | 2,534.5 ns | 138.92 ns |   200.5 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,710.3 ns | 2,516.9 ns | 137.96 ns | 3,657.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,880.0 ns | 3,863.9 ns | 211.79 ns | 3,907.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,224.7 ns | 1,094.7 ns |  60.00 ns | 2,224.0 ns |      96 B |
