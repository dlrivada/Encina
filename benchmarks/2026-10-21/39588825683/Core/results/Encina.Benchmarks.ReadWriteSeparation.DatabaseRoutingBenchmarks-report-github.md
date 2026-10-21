```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.88GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,127.7 ns | 2,382.2 ns | 130.58 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   193.3 ns | 1,053.3 ns |  57.74 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,511.0 ns | 6,949.6 ns | 380.93 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   235.5 ns | 1,424.9 ns |  78.10 ns |         - |
| &#39;Read HasIntent&#39;                       |   255.2 ns |   799.5 ns |  43.82 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   321.3 ns | 1,491.1 ns |  81.73 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   281.0 ns | 1,448.1 ns |  79.37 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,135.8 ns | 4,740.7 ns | 259.85 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,749.7 ns | 2,605.7 ns | 142.83 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,237.0 ns | 3,128.5 ns | 171.48 ns |      96 B |
