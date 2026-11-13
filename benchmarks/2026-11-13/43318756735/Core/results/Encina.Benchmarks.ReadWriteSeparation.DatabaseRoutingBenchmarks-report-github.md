```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,357.3 ns |  5,975.2 ns | 327.52 ns | 4,237.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   177.3 ns |    382.8 ns |  20.98 ns |   170.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,639.8 ns |    640.7 ns |  35.12 ns | 3,636.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   381.2 ns |  3,837.7 ns | 210.36 ns |   291.5 ns |         - |
| &#39;Read HasIntent&#39;                       |   226.7 ns |    105.3 ns |   5.77 ns |   230.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   214.0 ns |    766.9 ns |  42.04 ns |   201.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   298.8 ns |    640.7 ns |  35.12 ns |   295.5 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,104.2 ns | 12,002.0 ns | 657.87 ns | 3,796.5 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 4,253.0 ns |  9,965.0 ns | 546.22 ns | 4,083.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,939.0 ns |  2,565.6 ns | 140.63 ns | 2,946.0 ns |      96 B |
