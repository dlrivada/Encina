```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,485.7 ns | 10,272.9 ns |   563.09 ns | 4,239.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   199.7 ns |    738.9 ns |    40.50 ns |   200.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,371.0 ns |  4,906.7 ns |   268.96 ns | 4,468.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; | 1,339.3 ns | 25,685.8 ns | 1,407.93 ns |   751.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   267.7 ns |    459.1 ns |    25.17 ns |   271.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   254.3 ns |    822.7 ns |    45.09 ns |   251.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   296.3 ns |  2,769.2 ns |   151.79 ns |   230.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,692.3 ns |  3,299.1 ns |   180.83 ns | 3,642.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,867.3 ns |  4,974.6 ns |   272.67 ns | 3,907.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,228.0 ns |  1,469.4 ns |    80.54 ns | 2,274.0 ns |      96 B |
