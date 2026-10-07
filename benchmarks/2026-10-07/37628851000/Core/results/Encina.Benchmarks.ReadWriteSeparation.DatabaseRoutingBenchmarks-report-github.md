```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,619.7 ns | 10,873.4 ns | 596.01 ns | 4,623.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   163.8 ns |  2,001.3 ns | 109.70 ns |   100.5 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,560.7 ns |  2,777.0 ns | 152.22 ns | 3,587.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   222.2 ns |  1,485.9 ns |  81.45 ns |   185.5 ns |         - |
| &#39;Read HasIntent&#39;                       |   235.5 ns |    836.0 ns |  45.83 ns |   225.5 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   278.8 ns |  1,214.7 ns |  66.58 ns |   245.5 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   230.0 ns |    965.4 ns |  52.92 ns |   210.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,666.0 ns |  4,557.3 ns | 249.80 ns | 3,746.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,604.0 ns |  4,661.5 ns | 255.51 ns | 3,607.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,207.5 ns |  3,139.7 ns | 172.10 ns | 2,234.5 ns |      96 B |
