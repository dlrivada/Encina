```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 5,823.0 ns | 12,599.8 ns | 690.64 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   180.7 ns |    628.1 ns |  34.43 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,204.8 ns |  5,524.1 ns | 302.80 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   165.5 ns |    406.3 ns |  22.27 ns |         - |
| &#39;Read HasIntent&#39;                       |   291.3 ns |    495.4 ns |  27.15 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   218.5 ns |    496.6 ns |  27.22 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   248.2 ns |  2,378.0 ns | 130.35 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,785.3 ns |  9,823.8 ns | 538.48 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 4,261.2 ns | 16,716.2 ns | 916.27 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,299.5 ns |  6,540.4 ns | 358.50 ns |      96 B |
