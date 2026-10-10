```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,325.0 ns | 9,118.0 ns | 499.79 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   273.3 ns |   759.5 ns |  41.63 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,141.7 ns | 5,826.6 ns | 319.37 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   230.7 ns |   650.2 ns |  35.64 ns |         - |
| &#39;Read HasIntent&#39;                       |   223.3 ns |   459.1 ns |  25.17 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   252.2 ns | 1,004.8 ns |  55.08 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   265.5 ns |   632.0 ns |  34.64 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,723.2 ns |   899.9 ns |  49.33 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,721.8 ns | 3,163.1 ns | 173.38 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,196.0 ns | 1,454.6 ns |  79.73 ns |      96 B |
