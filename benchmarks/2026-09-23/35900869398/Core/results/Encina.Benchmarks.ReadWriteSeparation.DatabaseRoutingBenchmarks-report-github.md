```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,362.0 ns |  6,836.1 ns |   374.71 ns | 4,209.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   266.7 ns |  2,946.5 ns |   161.51 ns |   199.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,913.7 ns |  4,648.9 ns |   254.82 ns | 3,787.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   176.7 ns |    918.2 ns |    50.33 ns |   170.0 ns |         - |
| &#39;Read HasIntent&#39;                       | 1,151.8 ns | 23,527.5 ns | 1,289.62 ns |   530.5 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   323.0 ns |  2,060.6 ns |   112.95 ns |   296.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   340.5 ns |  1,558.7 ns |    85.44 ns |   330.5 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,634.0 ns |  3,150.8 ns |   172.70 ns | 3,577.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 4,412.0 ns | 11,927.0 ns |   653.76 ns | 4,679.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,275.0 ns |  1,277.1 ns |    70.00 ns | 2,305.0 ns |      96 B |
