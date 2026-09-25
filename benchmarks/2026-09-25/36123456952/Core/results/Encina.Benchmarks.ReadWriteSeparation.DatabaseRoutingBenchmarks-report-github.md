```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.54GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,399.0 ns | 38,846.5 ns | 2,129.31 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   101.7 ns |  1,588.8 ns |    87.09 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,421.0 ns | 16,144.4 ns |   884.93 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   168.3 ns |  2,445.6 ns |   134.05 ns |         - |
| &#39;Read HasIntent&#39;                       | 1,551.8 ns | 23,445.1 ns | 1,285.11 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   210.8 ns |  2,287.2 ns |   125.37 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   167.3 ns |    893.1 ns |    48.95 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,509.3 ns | 34,329.2 ns | 1,881.70 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,046.2 ns | 17,268.1 ns |   946.53 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 3,190.7 ns | 33,632.7 ns | 1,843.52 ns |      96 B |
