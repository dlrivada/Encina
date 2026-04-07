```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,087.3 ns | 1,387.0 ns |  76.03 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   220.0 ns |   965.4 ns |  52.92 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,638.2 ns | 2,792.7 ns | 153.08 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   280.7 ns | 1,430.7 ns |  78.42 ns |         - |
| &#39;Read HasIntent&#39;                       |   226.8 ns |   926.7 ns |  50.80 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   217.7 ns |   586.5 ns |  32.15 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   290.5 ns |   729.7 ns |  40.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,553.7 ns | 3,622.8 ns | 198.58 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,706.0 ns | 2,631.1 ns | 144.22 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,323.7 ns | 3,852.9 ns | 211.19 ns |      96 B |
