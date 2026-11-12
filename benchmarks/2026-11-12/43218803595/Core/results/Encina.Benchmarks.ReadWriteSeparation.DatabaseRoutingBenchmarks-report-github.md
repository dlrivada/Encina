```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.06GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 6,094.7 ns | 25,326.9 ns | 1,388.25 ns | 5,334.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   210.0 ns |  1,538.1 ns |    84.31 ns |   176.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,409.3 ns | 28,954.8 ns | 1,587.11 ns | 2,717.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   194.3 ns |  1,178.4 ns |    64.59 ns |   208.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   155.0 ns |  1,600.5 ns |    87.73 ns |   119.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   906.7 ns | 23,991.9 ns | 1,315.08 ns |   168.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   161.7 ns |  1,716.3 ns |    94.08 ns |   132.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,530.3 ns | 22,648.8 ns | 1,241.46 ns | 3,051.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,411.2 ns | 22,309.5 ns | 1,222.86 ns | 3,000.5 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,962.0 ns | 14,511.9 ns |   795.45 ns | 1,634.0 ns |      96 B |
