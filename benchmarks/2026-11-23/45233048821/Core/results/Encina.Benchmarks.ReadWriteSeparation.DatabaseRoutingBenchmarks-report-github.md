```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,435.3 ns | 3,218.9 ns | 176.44 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   237.3 ns |   115.9 ns |   6.35 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,583.7 ns | 4,123.4 ns | 226.02 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   204.0 ns |   567.3 ns |  31.10 ns |         - |
| &#39;Read HasIntent&#39;                       |   207.3 ns |   532.0 ns |  29.16 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   273.8 ns | 1,214.7 ns |  66.58 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   180.0 ns |   364.9 ns |  20.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,632.7 ns | 5,564.6 ns | 305.01 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,578.2 ns | 2,310.1 ns | 126.62 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,371.7 ns | 2,556.3 ns | 140.12 ns |      96 B |
