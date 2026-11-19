```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev       | Median     | Allocated |
|--------------------------------------- |------------:|-------------:|-------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 11,424.3 ns | 219,819.7 ns | 12,049.06 ns | 4,558.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |  1,080.5 ns |  28,318.8 ns |  1,552.25 ns |   265.5 ns |         - |
| DatabaseRoutingScope.ForRead()         |  3,758.5 ns |   6,876.1 ns |    376.90 ns | 3,631.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |    228.5 ns |   1,290.9 ns |     70.76 ns |   235.5 ns |         - |
| &#39;Read HasIntent&#39;                       |    252.7 ns |     842.6 ns |     46.19 ns |   226.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |    199.2 ns |   1,846.2 ns |    101.19 ns |   185.5 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |    249.7 ns |   1,451.5 ns |     79.56 ns |   219.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        |  4,407.8 ns |   1,769.8 ns |     97.01 ns | 4,367.5 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   |  4,535.7 ns |   6,421.0 ns |    351.96 ns | 4,369.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         |  2,306.3 ns |   1,234.7 ns |     67.68 ns | 2,330.0 ns |      96 B |
