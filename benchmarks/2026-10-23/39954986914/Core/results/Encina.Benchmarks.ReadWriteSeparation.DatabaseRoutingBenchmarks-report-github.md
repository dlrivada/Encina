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
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,224.0 ns |  4,344.4 ns |   238.13 ns | 4,107.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   233.3 ns |    862.2 ns |    47.26 ns |   250.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,783.7 ns |  3,749.2 ns |   205.51 ns | 3,887.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   171.0 ns |    836.0 ns |    45.83 ns |   161.0 ns |         - |
| &#39;Read HasIntent&#39;                       | 1,055.7 ns | 28,287.0 ns | 1,550.50 ns |   170.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   217.7 ns |  1,158.6 ns |    63.51 ns |   181.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   221.0 ns |    836.0 ns |    45.83 ns |   211.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,698.2 ns |  2,449.9 ns |   134.29 ns | 3,641.5 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,510.0 ns |    938.6 ns |    51.45 ns | 3,496.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,465.0 ns |  5,752.4 ns |   315.31 ns | 2,405.0 ns |      96 B |
