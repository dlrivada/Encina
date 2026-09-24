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
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,264.5 ns | 13,777.2 ns |   755.17 ns | 3,927.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   954.8 ns | 25,116.7 ns | 1,376.73 ns |   170.5 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,977.0 ns |  3,378.8 ns |   185.20 ns | 3,907.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   299.2 ns |  4,239.5 ns |   232.38 ns |   185.5 ns |         - |
| &#39;Read HasIntent&#39;                       |   232.2 ns |  1,037.4 ns |    56.86 ns |   215.5 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   300.8 ns |  1,567.3 ns |    85.91 ns |   310.5 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   161.0 ns |    316.0 ns |    17.32 ns |   171.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,625.7 ns |  5,678.5 ns |   311.26 ns | 3,532.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,836.3 ns |  4,929.5 ns |   270.20 ns | 3,706.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,237.0 ns |  3,694.8 ns |   202.53 ns | 2,204.0 ns |      96 B |
