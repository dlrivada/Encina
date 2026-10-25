```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,572.0 ns | 9,234.1 ns | 506.15 ns | 4,358.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   213.3 ns |   557.4 ns |  30.55 ns |   220.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,240.7 ns | 7,555.4 ns | 414.14 ns | 4,027.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   310.7 ns | 4,610.7 ns | 252.73 ns |   191.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   285.5 ns |   795.2 ns |  43.59 ns |   305.5 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   281.0 ns |   547.3 ns |  30.00 ns |   281.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   260.2 ns | 1,501.1 ns |  82.28 ns |   240.5 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,681.3 ns | 5,609.3 ns | 307.46 ns | 4,828.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,594.0 ns | 2,849.6 ns | 156.20 ns | 3,577.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,158.5 ns | 1,277.1 ns |  70.00 ns | 2,188.5 ns |      96 B |
