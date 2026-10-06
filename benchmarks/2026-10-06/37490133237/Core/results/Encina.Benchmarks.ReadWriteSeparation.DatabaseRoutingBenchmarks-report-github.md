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
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,114.0 ns |    747.1 ns |    40.95 ns | 4,107.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   233.3 ns |    557.4 ns |    30.55 ns |   240.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,443.3 ns |  2,018.4 ns |   110.64 ns | 3,437.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; | 1,131.7 ns | 27,546.3 ns | 1,509.90 ns |   280.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   237.0 ns |    641.7 ns |    35.17 ns |   241.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   167.3 ns |    288.7 ns |    15.82 ns |   171.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   224.3 ns |    278.7 ns |    15.28 ns |   221.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,710.2 ns |  2,171.7 ns |   119.04 ns | 3,646.5 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,825.5 ns |  5,039.3 ns |   276.22 ns | 3,802.5 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,624.7 ns |  1,287.5 ns |    70.57 ns | 2,655.0 ns |      96 B |
