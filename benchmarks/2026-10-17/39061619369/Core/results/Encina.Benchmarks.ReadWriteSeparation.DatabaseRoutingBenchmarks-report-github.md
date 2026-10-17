```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,115.3 ns |   759.5 ns |  41.63 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   197.7 ns |   690.7 ns |  37.86 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,631.5 ns | 3,593.6 ns | 196.98 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   219.7 ns | 1,283.6 ns |  70.36 ns |         - |
| &#39;Read HasIntent&#39;                       |   191.0 ns |   316.0 ns |  17.32 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   186.3 ns |   596.9 ns |  32.72 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   242.2 ns |   737.3 ns |  40.41 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,744.3 ns | 3,265.2 ns | 178.98 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,807.3 ns | 5,744.9 ns | 314.90 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,247.2 ns |   382.8 ns |  20.98 ns |      96 B |
