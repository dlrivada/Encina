```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,120.3 ns | 2,281.1 ns | 125.03 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   230.0 ns |   364.9 ns |  20.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,697.7 ns | 3,539.2 ns | 193.99 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   191.8 ns | 1,009.2 ns |  55.32 ns |         - |
| &#39;Read HasIntent&#39;                       |   250.8 ns |   921.3 ns |  50.50 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   271.5 ns | 1,337.8 ns |  73.33 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   255.5 ns | 1,109.7 ns |  60.83 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,269.5 ns | 4,994.2 ns | 273.75 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,770.0 ns | 4,825.7 ns | 264.51 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,283.7 ns | 1,085.5 ns |  59.50 ns |      96 B |
