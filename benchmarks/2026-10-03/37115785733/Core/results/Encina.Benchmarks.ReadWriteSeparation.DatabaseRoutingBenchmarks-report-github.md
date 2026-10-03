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
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,245.0 ns | 3,326.0 ns | 182.31 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   260.5 ns |   948.0 ns |  51.96 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,698.2 ns | 2,009.6 ns | 110.15 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   241.5 ns | 1,183.3 ns |  64.86 ns |         - |
| &#39;Read HasIntent&#39;                       |   237.7 ns |   278.7 ns |  15.28 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   283.3 ns |   640.7 ns |  35.12 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   483.3 ns | 4,002.5 ns | 219.39 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,687.3 ns | 3,205.4 ns | 175.70 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,614.5 ns | 2,493.9 ns | 136.70 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,167.0 ns | 2,837.3 ns | 155.52 ns |      96 B |
