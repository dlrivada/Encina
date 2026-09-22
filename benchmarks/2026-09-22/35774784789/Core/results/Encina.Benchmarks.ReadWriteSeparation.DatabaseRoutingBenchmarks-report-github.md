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
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,942.0 ns | 20,386.3 ns | 1,117.44 ns | 4,598.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   149.7 ns |    729.8 ns |    40.00 ns |   149.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,710.7 ns |  4,648.0 ns |   254.78 ns | 3,657.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   168.8 ns |  1,685.3 ns |    92.38 ns |   115.5 ns |         - |
| &#39;Read HasIntent&#39;                       |   346.7 ns |  1,037.4 ns |    56.86 ns |   330.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   240.2 ns |  1,323.2 ns |    72.53 ns |   220.5 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   221.0 ns |  2,219.4 ns |   121.66 ns |   161.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 5,135.8 ns | 38,929.6 ns | 2,133.87 ns | 4,046.5 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,565.7 ns |  3,035.3 ns |   166.38 ns | 3,485.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,882.3 ns |    639.9 ns |    35.08 ns | 2,885.0 ns |      96 B |
