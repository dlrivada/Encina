```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,423.2 ns | 2,627.4 ns | 144.02 ns | 3,479.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   127.2 ns | 1,037.4 ns |  56.86 ns |   110.5 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,716.0 ns | 4,962.4 ns | 272.01 ns | 2,749.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   120.7 ns |   472.3 ns |  25.89 ns |   111.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   144.3 ns |   210.7 ns |  11.55 ns |   151.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   147.2 ns |   640.7 ns |  35.12 ns |   150.5 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   244.8 ns | 2,695.5 ns | 147.75 ns |   175.5 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,747.2 ns | 3,018.2 ns | 165.44 ns | 2,793.5 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,915.0 ns | 3,378.8 ns | 185.20 ns | 2,925.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,729.3 ns | 1,560.9 ns |  85.56 ns | 1,733.0 ns |      96 B |
