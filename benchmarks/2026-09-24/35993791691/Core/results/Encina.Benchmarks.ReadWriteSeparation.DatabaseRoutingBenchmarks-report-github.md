```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,902.0 ns | 8,676.4 ns | 475.58 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   191.0 ns | 1,109.7 ns |  60.83 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,737.3 ns | 1,849.8 ns | 101.39 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   246.3 ns |   100.5 ns |   5.51 ns |         - |
| &#39;Read HasIntent&#39;                       |   271.0 ns |   812.0 ns |  44.51 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   245.7 ns |   862.2 ns |  47.26 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   277.7 ns | 1,099.7 ns |  60.28 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,573.3 ns | 5,502.5 ns | 301.61 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,580.7 ns | 3,705.8 ns | 203.13 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,380.3 ns | 4,075.4 ns | 223.38 ns |      96 B |
