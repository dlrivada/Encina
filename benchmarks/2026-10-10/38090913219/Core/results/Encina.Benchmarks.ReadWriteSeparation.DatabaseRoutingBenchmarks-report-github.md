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
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,124.7 ns |   379.8 ns |  20.82 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   220.2 ns | 1,024.0 ns |  56.13 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,483.7 ns | 1,214.7 ns |  66.58 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   152.7 ns |   288.7 ns |  15.82 ns |         - |
| &#39;Read HasIntent&#39;                       |   231.0 ns |   482.7 ns |  26.46 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   306.3 ns | 1,479.9 ns |  81.12 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   328.0 ns | 1,245.0 ns |  68.24 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,603.3 ns | 1,351.8 ns |  74.10 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,582.7 ns |   459.1 ns |  25.17 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,552.0 ns | 6,496.1 ns | 356.07 ns |      96 B |
