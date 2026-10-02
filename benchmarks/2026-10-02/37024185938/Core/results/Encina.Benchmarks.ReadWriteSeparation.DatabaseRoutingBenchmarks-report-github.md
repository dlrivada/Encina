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
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,110.3 ns | 2,281.1 ns | 125.03 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   220.0 ns |   912.2 ns |  50.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,503.3 ns | 1,604.0 ns |  87.92 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   187.2 ns | 1,485.9 ns |  81.45 ns |         - |
| &#39;Read HasIntent&#39;                       |   206.3 ns |   270.8 ns |  14.84 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   187.7 ns |   105.3 ns |   5.77 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   209.7 ns |   729.8 ns |  40.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,759.3 ns | 4,159.8 ns | 228.01 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,660.7 ns | 1,705.6 ns |  93.49 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,085.0 ns | 1,377.4 ns |  75.50 ns |      96 B |
