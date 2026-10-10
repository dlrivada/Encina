```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev      | Median        | Allocated |
|--------------------------------------- |------------:|-------------:|------------:|--------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 2,908.00 ns | 17,956.11 ns |   984.24 ns | 3,425.0000 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    61.00 ns |  1,630.44 ns |    89.37 ns |    15.0000 ns |         - |
| DatabaseRoutingScope.ForRead()         | 1,933.33 ns | 24,980.35 ns | 1,369.26 ns | 1,462.0000 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |    63.67 ns |  1,632.82 ns |    89.50 ns |    25.0000 ns |         - |
| &#39;Read HasIntent&#39;                       |   103.67 ns |  1,208.82 ns |    66.26 ns |    70.0000 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   128.33 ns |  1,068.98 ns |    58.59 ns |   105.0000 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |    28.17 ns |    890.04 ns |    48.79 ns |     0.0000 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 1,774.67 ns | 19,617.71 ns | 1,075.31 ns | 1,468.0000 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 1,661.00 ns | 13,744.29 ns |   753.37 ns | 1,277.0000 ns |     392 B |
| DatabaseRoutingContext.Clear()         |   938.33 ns | 11,662.60 ns |   639.27 ns |   741.0000 ns |      96 B |
