```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev       | Median     | Allocated |
|--------------------------------------- |------------:|-------------:|-------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    |  3,994.7 ns |   2,217.5 ns |    121.55 ns | 4,008.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    205.5 ns |     965.4 ns |     52.92 ns |   185.5 ns |         - |
| DatabaseRoutingScope.ForRead()         |  3,732.2 ns |   7,368.5 ns |    403.89 ns | 3,792.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |    206.3 ns |     468.8 ns |     25.70 ns |   210.0 ns |         - |
| &#39;Read HasIntent&#39;                       |    215.5 ns |   1,109.7 ns |     60.83 ns |   185.5 ns |         - |
| &#39;Read IsReadIntent&#39;                    |    227.3 ns |     559.4 ns |     30.66 ns |   220.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |    186.8 ns |     532.0 ns |     29.16 ns |   170.5 ns |         - |
| DatabaseRoutingScope.ForWrite()        |  3,609.8 ns |   2,374.0 ns |    130.13 ns | 3,616.5 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 11,093.3 ns | 223,773.3 ns | 12,265.77 ns | 4,127.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         |  2,257.0 ns |     904.3 ns |     49.57 ns | 2,233.0 ns |      96 B |
