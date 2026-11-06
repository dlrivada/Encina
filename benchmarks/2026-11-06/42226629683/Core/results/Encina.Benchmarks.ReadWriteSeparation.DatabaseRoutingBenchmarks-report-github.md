```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,138.0 ns |    482.7 ns |  26.46 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   190.0 ns |    632.0 ns |  34.64 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,700.7 ns |  4,412.6 ns | 241.87 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   220.5 ns |    836.0 ns |  45.83 ns |         - |
| &#39;Read HasIntent&#39;                       |   187.7 ns |    690.7 ns |  37.86 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   150.0 ns |    364.9 ns |  20.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   297.7 ns |  1,037.4 ns |  56.86 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,281.7 ns | 17,855.8 ns | 978.74 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,707.3 ns |  2,754.8 ns | 151.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,206.7 ns |    769.8 ns |  42.19 ns |      96 B |
