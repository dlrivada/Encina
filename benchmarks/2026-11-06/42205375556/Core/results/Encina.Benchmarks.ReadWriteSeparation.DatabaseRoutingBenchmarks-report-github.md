```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error       | StdDev    | Median      | Allocated |
|--------------------------------------- |------------:|------------:|----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,244.17 ns | 4,668.41 ns | 255.89 ns | 3,304.50 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    87.00 ns |   286.72 ns |  15.72 ns |    90.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,889.67 ns | 2,078.53 ns | 113.93 ns | 2,840.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |    97.00 ns |   917.10 ns |  50.27 ns |    91.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   189.67 ns |   547.41 ns |  30.01 ns |   189.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   147.00 ns |   286.72 ns |  15.72 ns |   150.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   240.33 ns | 1,590.49 ns |  87.18 ns |   190.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,781.00 ns | 3,907.61 ns | 214.19 ns | 2,694.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,068.00 ns | 6,978.24 ns | 382.50 ns | 2,905.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,836.67 ns | 3,097.16 ns | 169.77 ns | 1,793.00 ns |      96 B |
