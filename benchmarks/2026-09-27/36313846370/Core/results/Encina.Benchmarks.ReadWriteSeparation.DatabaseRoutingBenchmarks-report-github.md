```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error        | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|-------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,022.7 ns |   1,458.4 ns |    79.94 ns | 4,053.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   309.7 ns |   3,119.1 ns |   170.97 ns |   216.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,298.7 ns |   9,819.8 ns |   538.26 ns | 4,509.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   303.8 ns |     459.1 ns |    25.17 ns |   300.5 ns |         - |
| &#39;Read HasIntent&#39;                       |   291.3 ns |     650.2 ns |    35.64 ns |   281.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   437.0 ns |   1,479.5 ns |    81.10 ns |   400.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   271.3 ns |   1,295.3 ns |    71.00 ns |   272.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,796.3 ns |   4,976.6 ns |   272.78 ns | 3,686.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 9,108.8 ns | 166,732.0 ns | 9,139.15 ns | 3,902.5 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,661.0 ns |   9,304.1 ns |   509.99 ns | 2,554.0 ns |      96 B |
