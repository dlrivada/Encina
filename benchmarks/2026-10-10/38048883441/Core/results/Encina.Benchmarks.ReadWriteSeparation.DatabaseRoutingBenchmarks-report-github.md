```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.47GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median      | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 1,800.7 ns | 18,953.3 ns | 1,038.89 ns | 1,527.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   117.0 ns |  1,280.6 ns |    70.19 ns |   111.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 1,481.7 ns | 17,397.5 ns |   953.61 ns | 1,271.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   120.3 ns |  1,287.5 ns |    70.57 ns |    90.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   190.3 ns |  1,114.3 ns |    61.08 ns |   221.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   130.3 ns |  1,313.1 ns |    71.97 ns |   111.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   136.7 ns |  1,037.4 ns |    56.86 ns |   120.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 1,565.7 ns | 16,590.7 ns |   909.39 ns | 1,482.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 1,515.7 ns | 15,483.3 ns |   848.69 ns | 1,162.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         |   918.3 ns |  7,181.2 ns |   393.62 ns |   782.00 ns |      96 B |
