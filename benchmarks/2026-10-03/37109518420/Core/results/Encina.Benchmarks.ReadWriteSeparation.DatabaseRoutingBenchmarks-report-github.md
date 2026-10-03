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
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,104.3 ns |  1,225.2 ns |    67.16 ns | 4,138.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   207.7 ns |    936.2 ns |    51.32 ns |   221.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,421.0 ns |  4,130.0 ns |   226.38 ns | 4,398.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   216.7 ns |  1,950.7 ns |   106.93 ns |   160.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   283.3 ns |  1,069.0 ns |    58.59 ns |   260.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   985.7 ns | 25,270.2 ns | 1,385.14 ns |   201.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   209.7 ns |  1,451.5 ns |    79.56 ns |   179.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,713.3 ns |  2,030.7 ns |   111.31 ns | 3,756.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,843.3 ns |  1,014.2 ns |    55.59 ns | 3,847.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,400.7 ns |  1,845.6 ns |   101.16 ns | 2,394.0 ns |      96 B |
