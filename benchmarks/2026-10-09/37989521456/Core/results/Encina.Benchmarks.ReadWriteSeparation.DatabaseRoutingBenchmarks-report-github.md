```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Median      | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,410.0 ns | 3,319.2 ns | 181.93 ns | 3,510.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   170.7 ns |   921.3 ns |  50.50 ns |   171.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,712.5 ns |   837.4 ns |  45.90 ns | 2,738.50 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   100.0 ns |   729.7 ns |  40.00 ns |   100.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   130.3 ns | 1,451.5 ns |  79.56 ns |   161.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   116.7 ns | 1,004.8 ns |  55.08 ns |    90.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   118.2 ns |   756.7 ns |  41.48 ns |   105.50 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,691.0 ns | 6,568.7 ns | 360.05 ns | 2,574.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,533.7 ns | 3,910.3 ns | 214.34 ns | 2,604.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,759.7 ns | 1,114.7 ns |  61.10 ns | 1,773.00 ns |      96 B |
