```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.23GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 5,609.7 ns | 19,056.1 ns | 1,044.53 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   205.2 ns |  1,072.6 ns |    58.79 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,900.0 ns | 10,829.7 ns |   593.61 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   211.2 ns |  1,227.4 ns |    67.28 ns |         - |
| &#39;Read HasIntent&#39;                       |   156.7 ns |    412.1 ns |    22.59 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   245.7 ns |    284.0 ns |    15.57 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   286.8 ns |  1,728.6 ns |    94.75 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,444.0 ns |  9,043.8 ns |   495.72 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,899.5 ns |  7,568.0 ns |   414.83 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,297.2 ns |  9,104.0 ns |   499.02 ns |      96 B |
