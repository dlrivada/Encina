```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,812.2 ns |  7,177.8 ns | 393.44 ns | 3,925.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   227.0 ns |    567.3 ns |  31.10 ns |   220.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,713.2 ns |  8,403.2 ns | 460.61 ns | 3,509.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   206.8 ns |  2,416.9 ns | 132.48 ns |   180.5 ns |         - |
| &#39;Read HasIntent&#39;                       |   162.3 ns |  1,641.0 ns |  89.95 ns |   116.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   264.3 ns |  3,749.2 ns | 205.51 ns |   161.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   224.0 ns |  1,851.5 ns | 101.49 ns |   204.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,698.8 ns | 10,257.0 ns | 562.22 ns | 3,485.5 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,762.2 ns | 17,018.2 ns | 932.83 ns | 3,464.5 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,956.7 ns |  2,665.7 ns | 146.12 ns | 2,003.0 ns |      96 B |
