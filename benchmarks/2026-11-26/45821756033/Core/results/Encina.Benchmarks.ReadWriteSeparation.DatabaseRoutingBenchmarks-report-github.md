```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,129.7 ns | 2,530.12 ns | 138.68 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   208.2 ns |   910.42 ns |  49.90 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,248.3 ns | 9,565.18 ns | 524.30 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   197.3 ns | 1,334.45 ns |  73.15 ns |         - |
| &#39;Read HasIntent&#39;                       |   311.0 ns |     0.00 ns |   0.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   273.3 ns |   586.45 ns |  32.15 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   230.7 ns |   479.34 ns |  26.27 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,630.0 ns | 2,960.72 ns | 162.29 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,958.8 ns | 8,856.03 ns | 485.43 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,220.0 ns | 2,201.15 ns | 120.65 ns |      96 B |
