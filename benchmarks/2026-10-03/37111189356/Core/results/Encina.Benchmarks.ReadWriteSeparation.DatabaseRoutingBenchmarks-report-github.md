```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error       | StdDev    | Allocated |
|--------------------------------------- |------------:|------------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,110.67 ns | 4,737.52 ns | 259.68 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   116.50 ns |   280.86 ns |  15.39 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,668.00 ns | 2,291.53 ns | 125.61 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   260.67 ns | 3,490.25 ns | 191.31 ns |         - |
| &#39;Read HasIntent&#39;                       |    91.83 ns |   382.83 ns |  20.98 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   127.33 ns |   278.68 ns |  15.28 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   137.33 ns |   565.36 ns |  30.99 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,786.33 ns | 4,228.98 ns | 231.80 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,644.33 ns | 1,594.67 ns |  87.41 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,672.00 ns | 3,822.48 ns | 209.52 ns |      96 B |
