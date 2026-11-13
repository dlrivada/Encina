```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev     | Median      | Allocated |
|--------------------------------------- |-----------:|------------:|-----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,477.8 ns | 33,675.8 ns | 1,845.9 ns | 3,636.50 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   153.5 ns |  2,740.4 ns |   150.2 ns |   131.50 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,357.8 ns |  4,667.4 ns |   255.8 ns | 3,499.50 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   170.8 ns |  3,135.2 ns |   171.8 ns |   119.50 ns |         - |
| &#39;Read HasIntent&#39;                       |   123.7 ns |  2,990.8 ns |   163.9 ns |    46.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   165.0 ns |  3,369.3 ns |   184.7 ns |   130.50 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   130.7 ns |  2,497.1 ns |   136.9 ns |   119.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,398.2 ns | 16,526.2 ns |   905.9 ns | 2,209.50 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 4,272.5 ns | 44,007.6 ns | 2,412.2 ns | 2,919.50 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 5,004.2 ns | 42,412.6 ns | 2,324.8 ns | 6,261.50 ns |      96 B |
