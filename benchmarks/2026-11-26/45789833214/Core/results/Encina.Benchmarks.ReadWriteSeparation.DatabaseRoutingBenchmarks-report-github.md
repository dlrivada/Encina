```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.28GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error        | StdDev     | Median      | Allocated |
|--------------------------------------- |-----------:|-------------:|-----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 2,112.8 ns |  23,304.8 ns | 1,277.4 ns | 1,742.50 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   876.3 ns |  11,086.7 ns |   607.7 ns | 1,207.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 6,573.3 ns | 157,066.3 ns | 8,609.3 ns | 2,654.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; | 1,364.0 ns |   1,837.7 ns |   100.7 ns | 1,327.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   610.7 ns |  14,872.6 ns |   815.2 ns |   140.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   492.7 ns |  14,140.3 ns |   775.1 ns |    75.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   530.0 ns |  14,551.4 ns |   797.6 ns |    70.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,115.0 ns |  26,642.4 ns | 1,460.4 ns | 1,438.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 1,562.3 ns |  17,399.1 ns |   953.7 ns | 1,142.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         |   788.3 ns |  12,123.0 ns |   664.5 ns |   461.00 ns |      96 B |
