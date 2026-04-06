```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|----------:|
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   204.0 ns |   567.3 ns |  31.10 ns |         - |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   203.3 ns | 1,114.7 ns |  61.10 ns |         - |
| &#39;Read HasIntent&#39;                       |   280.5 ns | 1,740.3 ns |  95.39 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   199.3 ns |   665.4 ns |  36.47 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   237.7 ns |   105.3 ns |   5.77 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,419.5 ns | 2,540.2 ns | 139.24 ns |     392 B |
| DatabaseRoutingScope.ForWrite()        | 3,605.7 ns | 3,789.7 ns | 207.73 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,607.0 ns | 2,056.0 ns | 112.69 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,384.7 ns | 1,429.6 ns |  78.36 ns |      96 B |
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,262.0 ns | 2,472.5 ns | 135.52 ns |     840 B |
