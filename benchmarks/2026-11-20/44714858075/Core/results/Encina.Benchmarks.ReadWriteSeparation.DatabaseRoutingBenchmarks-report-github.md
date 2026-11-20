```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.40GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev     | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|-----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 2,455.7 ns | 22,750.9 ns | 1,247.1 ns | 1,838.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   168.7 ns |  2,058.4 ns |   112.8 ns |   195.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 1,722.7 ns | 18,382.6 ns | 1,007.6 ns | 1,462.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   503.7 ns | 11,583.8 ns |   634.9 ns |   220.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   563.5 ns | 11,525.2 ns |   631.7 ns |   239.5 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   556.0 ns | 14,396.1 ns |   789.1 ns |   116.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   213.7 ns |  4,074.5 ns |   223.3 ns |   110.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 1,852.5 ns | 20,992.8 ns | 1,150.7 ns | 1,581.5 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 1,909.7 ns | 18,521.2 ns | 1,015.2 ns | 1,803.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         |   976.7 ns | 13,363.6 ns |   732.5 ns |   696.0 ns |      96 B |
