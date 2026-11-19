```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error       | StdDev    | Median      | Allocated |
|--------------------------------------- |------------:|------------:|----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,282.00 ns |   426.68 ns |  23.39 ns | 3,295.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    87.33 ns |   468.81 ns |  25.70 ns |    91.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,698.83 ns | 6,351.86 ns | 348.17 ns | 2,748.50 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   172.33 ns | 1,473.15 ns |  80.75 ns |   185.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   103.17 ns | 1,010.90 ns |  55.41 ns |   129.50 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   110.67 ns |   191.63 ns |  10.50 ns |   111.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   128.67 ns |   869.28 ns |  47.65 ns |   145.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,841.00 ns | 6,268.17 ns | 343.58 ns | 2,894.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,610.67 ns | 4,730.49 ns | 259.29 ns | 2,724.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,669.00 ns | 1,224.78 ns |  67.13 ns | 1,652.00 ns |      96 B |
