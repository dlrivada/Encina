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
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,537.3 ns | 1,463.3 ns |  80.21 ns | 4,544.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   337.2 ns | 2,130.2 ns | 116.76 ns |   360.5 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,827.7 ns | 2,220.2 ns | 121.70 ns | 3,808.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   242.2 ns |   278.7 ns |  15.28 ns |   245.5 ns |         - |
| &#39;Read HasIntent&#39;                       |   246.7 ns | 1,344.8 ns |  73.71 ns |   220.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   390.5 ns | 5,085.4 ns | 278.75 ns |   260.5 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   231.8 ns | 1,009.2 ns |  55.32 ns |   204.5 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,630.2 ns | 3,932.8 ns | 215.57 ns | 3,576.5 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 4,877.5 ns | 7,224.8 ns | 396.02 ns | 4,873.5 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,423.2 ns | 9,308.2 ns | 510.22 ns | 2,169.5 ns |      96 B |
