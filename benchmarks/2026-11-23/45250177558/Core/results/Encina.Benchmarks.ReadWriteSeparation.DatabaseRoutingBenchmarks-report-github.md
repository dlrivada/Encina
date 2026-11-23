```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,329.5 ns | 2,150.9 ns | 117.90 ns | 3,299.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   107.0 ns |   280.9 ns |  15.39 ns |   111.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,799.7 ns | 2,744.7 ns | 150.44 ns | 2,723.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   168.5 ns | 1,403.3 ns |  76.92 ns |   185.5 ns |         - |
| &#39;Read HasIntent&#39;                       |   493.0 ns | 7,114.5 ns | 389.97 ns |   336.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   223.8 ns | 3,901.5 ns | 213.85 ns |   110.5 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   223.7 ns | 2,391.3 ns | 131.07 ns |   180.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,573.7 ns | 5,147.5 ns | 282.15 ns | 2,644.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,134.7 ns | 8,037.0 ns | 440.53 ns | 2,964.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,676.0 ns | 5,031.7 ns | 275.80 ns | 1,532.0 ns |      96 B |
