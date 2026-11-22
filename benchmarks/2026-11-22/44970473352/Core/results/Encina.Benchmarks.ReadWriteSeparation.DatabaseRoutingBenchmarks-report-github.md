```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                                 | Mean        | Error     | StdDev    | Median      | Allocated |
|--------------------------------------- |------------:|----------:|----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,053.89 ns | 175.08 ns | 251.09 ns | 2,996.50 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   100.15 ns |  14.92 ns |  22.33 ns |   100.25 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,590.46 ns | 103.78 ns | 142.06 ns | 2,598.50 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   118.80 ns |  16.08 ns |  24.07 ns |   125.50 ns |         - |
| &#39;Read HasIntent&#39;                       |    70.95 ns |  31.89 ns |  45.73 ns |    95.25 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   158.04 ns |  55.08 ns |  78.99 ns |   125.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |    95.39 ns |  25.91 ns |  37.16 ns |   100.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,430.10 ns | 176.83 ns | 259.19 ns | 2,454.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,547.79 ns | 109.30 ns | 156.75 ns | 2,558.50 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,503.46 ns | 130.35 ns | 186.95 ns | 1,562.50 ns |      96 B |
