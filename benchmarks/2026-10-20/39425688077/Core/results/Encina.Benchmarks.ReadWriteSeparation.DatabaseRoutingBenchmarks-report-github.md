```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,974.2 ns | 3,010.7 ns | 165.03 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   255.2 ns |   955.0 ns |  52.35 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,659.7 ns | 2,617.9 ns | 143.49 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   186.3 ns | 1,009.2 ns |  55.32 ns |         - |
| &#39;Read HasIntent&#39;                       |   301.8 ns | 2,453.8 ns | 134.50 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   306.0 ns |   948.0 ns |  51.96 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   307.7 ns | 1,319.8 ns |  72.34 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,623.7 ns | 2,324.4 ns | 127.41 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,756.0 ns | 2,056.0 ns | 112.69 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,207.7 ns | 4,494.4 ns | 246.35 ns |      96 B |
