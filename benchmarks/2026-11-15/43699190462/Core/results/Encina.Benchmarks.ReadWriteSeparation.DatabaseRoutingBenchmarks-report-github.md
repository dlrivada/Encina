```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.31GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev      | Median      | Allocated |
|--------------------------------------- |------------:|-------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 2,009.67 ns | 22,104.42 ns | 1,211.62 ns | 1,793.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   109.00 ns |  1,596.40 ns |    87.50 ns |    85.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 1,710.50 ns | 15,901.78 ns |   871.63 ns | 1,557.50 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |    66.67 ns |  1,790.61 ns |    98.15 ns |    10.00 ns |         - |
| &#39;Read HasIntent&#39;                       |    57.00 ns |  1,330.29 ns |    72.92 ns |    20.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   110.00 ns |  1,558.74 ns |    85.44 ns |   100.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |    92.00 ns |  1,047.55 ns |    57.42 ns |    75.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 1,594.00 ns | 17,069.23 ns |   935.62 ns | 1,407.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 1,580.67 ns | 18,955.68 ns | 1,039.02 ns | 1,187.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         |   896.33 ns |  9,407.29 ns |   515.65 ns |   666.00 ns |      96 B |
